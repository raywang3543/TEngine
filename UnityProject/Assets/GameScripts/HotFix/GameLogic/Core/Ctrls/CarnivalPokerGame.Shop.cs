using System;
using System.Collections.Generic;

namespace GameLogic.Core
{
    public sealed partial class CarnivalPokerGame
    {
        public bool BuyPerformer(string performerId)
        {
            if (Phase != CarnivalRunPhase.Shop || IsBoosterOpen)
                return false;

            CarnivalShopOffer offer = FindOffer(performerId);
            CarnivalPerformer performer = offer?.Performer;
            if (performer == null)
                return false;

            if (_performers.Count >= MaxPerformerSlots)
            {
                StatusMessage = $"小丑牌槽位已满（当前最多 {MaxPerformerSlots} 张）。";
                return false;
            }

            int cost = GetOfferCost(performerId);
            if (!CanAfford(cost))
            {
                StatusMessage = "金币不足。";
                return false;
            }

            Money -= cost;
            CarnivalPerformer ownedPerformer = AddOwnedPerformer(performer);
            _shopOffers.Remove(offer);
            StatusMessage = $"已购买「{ownedPerformer.Name}」。";
            return true;
        }

        public bool BuyConsumable(string consumableId)
        {
            if (Phase != CarnivalRunPhase.Shop || IsBoosterOpen)
                return false;

            CarnivalShopOffer offer = FindOffer(consumableId);
            CarnivalConsumable consumable = offer?.Consumable;
            if (consumable == null)
                return false;

            if (!HasConsumableSlot())
            {
                StatusMessage = $"消耗牌栏已满（最多 {MaxConsumables} 张）。";
                return false;
            }

            int cost = GetOfferCost(consumableId);
            if (!CanAfford(cost))
            {
                StatusMessage = "金币不足。";
                return false;
            }

            Money -= cost;
            AddOwnedConsumable(consumable);
            _shopOffers.Remove(offer);
            StatusMessage = $"获得「{consumable.Name}」。可在盲注中使用。";
            return true;
        }

        public bool SellConsumable(string consumableId)
        {
            CarnivalConsumableState consumable = _consumables.Find(item =>
                item.RuntimeId == consumableId || item.Id == consumableId);
            if (consumable == null)
                return false;

            _consumables.Remove(consumable);
            Money += consumable.SellValue;
            RecordCardSoldForUnlocks(false);
            ApplyJokers(
                CarnivalJokerTrigger.CardSold,
                new CarnivalJokerContext
                {
                    Trigger = CarnivalJokerTrigger.CardSold,
                    Consumable = consumable,
                    SoldCardKind = CarnivalSoldCardKind.Consumable,
                });
            StatusMessage = $"出售「{consumable.Name}」，获得 ${consumable.SellValue}。";
            return true;
        }

        public void ContinueFromShop()
        {
            if (Phase != CarnivalRunPhase.Shop || IsBoosterOpen)
                return;

            ApplyEndOfShopJokers();
            _couponShopActive = false;
            _currentBoosterPack = null;
            Round++;
            StartRound();
        }

        public bool RerollShop()
        {
            if (Phase != CarnivalRunPhase.Shop || IsBoosterOpen)
                return false;

            int cost = RerollCost;
            if (!CanAfford(cost))
            {
                StatusMessage = "金币不足，无法重掷商店。";
                return false;
            }

            Money -= cost;
            if (_freeRerolls > 0)
                _freeRerolls--;
            _shopRerollsThisRun++;

            CarnivalPerformer flash = FindOwnedJoker("flash");
            if (flash != null)
                GetJokerState(flash).Value += 2f;

            GenerateShop(false);
            StatusMessage = cost == 0 ? "免费重掷了商店。" : $"花费 ${cost} 重掷了商店。";
            return true;
        }

        public bool SellPerformer(int performerIndex)
        {
            if (performerIndex < 0 || performerIndex >= _performers.Count)
                return false;

            CarnivalPerformer performer = _performers[performerIndex];
            CarnivalJokerState state = GetJokerState(performer);
            if (state.Eternal)
            {
                StatusMessage = "永恒小丑牌无法出售。";
                return false;
            }

            int sellValue = state.SellValue;
            bool duplicateAfterSale = performer.Id == "invisible" && state.Counter >= 2;
            bool createsDoubleTag = performer.Id == "diet_cola";
            RemoveOwnedPerformer(performer);
            Money += sellValue;
            RecordCardSoldForUnlocks(true);

            if (createsDoubleTag)
                _doubleTagCount++;

            ApplyJokers(
                CarnivalJokerTrigger.CardSold,
                new CarnivalJokerContext
                {
                    Trigger = CarnivalJokerTrigger.CardSold,
                    SoldJoker = performer,
                    SoldCardKind = CarnivalSoldCardKind.Joker,
                });

            if (performer.Id == "luchador" &&
                CurrentBlind != null &&
                CurrentBlind.Tier == CarnivalBlindTier.Boss)
            {
                _bossBlindDisabled = true;
            }

            if (duplicateAfterSale && _performers.Count > 0 && _performers.Count < MaxPerformerSlots)
            {
                CarnivalPerformer source = _performers[_random.Next(_performers.Count)];
                AddOwnedPerformer(source);
            }

            StatusMessage = createsDoubleTag
                ? $"出售「{performer.Name}」，获得 ${sellValue} 和 1 个双倍标签。"
                : $"出售「{performer.Name}」，获得 ${sellValue}。";
            return true;
        }

        public bool MovePerformer(int performerIndex, int direction)
        {
            int targetIndex = performerIndex + Math.Sign(direction);
            if (performerIndex < 0 || performerIndex >= _performers.Count ||
                targetIndex < 0 || targetIndex >= _performers.Count)
            {
                return false;
            }

            CarnivalPerformer performer = _performers[performerIndex];
            _performers.RemoveAt(performerIndex);
            _performers.Insert(targetIndex, performer);
            StatusMessage = $"已调整「{performer.Name}」的位置。";
            return true;
        }

        public int GetPerformerSellValue(int performerIndex)
        {
            return performerIndex >= 0 && performerIndex < _performers.Count
                ? GetJokerState(_performers[performerIndex]).SellValue
                : 0;
        }

        public int GetOfferCost(string offerId)
        {
            CarnivalShopOffer offer = FindOffer(offerId);
            if (offer == null)
                return 0;
            if (_couponShopActive)
                return 0;
            if (offer.Kind == CarnivalShopOfferKind.Consumable &&
                offer.Consumable.Family == CarnivalConsumableFamily.Planet &&
                HasJoker("astronomer"))
            {
                return 0;
            }

            return offer.Cost;
        }

        private bool CanAfford(int cost)
        {
            int minimumMoney = HasJoker("credit_card") ? -20 : 0;
            return Money - cost >= minimumMoney;
        }

        private void GenerateShop(bool resetFreeRerolls = true)
        {
            _shopOffers.Clear();
            var candidates = new List<CarnivalPerformer>();
            bool allowDuplicates = HasJoker("ring_master");
            foreach (CarnivalPerformer performer in _contentModel.Performers)
            {
                if (!performer.UnlockedByDefault && !_unlockModel.IsJokerUnlocked(performer.Id))
                    continue;
                if (performer.Rarity == "传说")
                    continue;
                if (performer.Id == "cavendish" && !_grosMichelExtinct)
                    continue;
                if (allowDuplicates || !HasJoker(performer.Id))
                    candidates.Add(performer);
            }

            int offerCount = Math.Min(2, candidates.Count);
            for (int i = 0; i < offerCount; i++)
            {
                CarnivalPerformer performer = DrawShopPerformer(candidates);
                if (performer == null)
                    break;
                _shopOffers.Add(new CarnivalShopOffer(performer));
                if (!allowDuplicates)
                    candidates.Remove(performer);
            }

            bool showman = HasJoker("ring_master");
            var consumableCandidates = new List<CarnivalConsumable>();
            foreach (CarnivalConsumable consumable in _contentModel.Consumables)
            {
                if (showman || !HasConsumable(consumable.Id))
                    consumableCandidates.Add(consumable);
            }
            Shuffle(consumableCandidates);
            int consumableOfferCount = Math.Min(2, consumableCandidates.Count);
            for (int i = 0; i < consumableOfferCount; i++)
            {
                CarnivalConsumable consumable = showman
                    ? consumableCandidates[_random.Next(consumableCandidates.Count)]
                    : consumableCandidates[i];
                _shopOffers.Add(new CarnivalShopOffer(consumable));
            }

            if (resetFreeRerolls)
            {
                _couponShopActive = _couponShopPending;
                _couponShopPending = false;
                _freeRerolls = HasJoker("chaos") || _d6TagPending ? 1 : 0;
                _d6TagPending = false;
                GenerateBoosterPack();
            }
        }

        private CarnivalShopOffer FindOffer(string offerId)
        {
            foreach (CarnivalShopOffer offer in _shopOffers)
            {
                if (offer.Id == offerId)
                    return offer;
            }

            return null;
        }

        private CarnivalPerformer DrawShopPerformer(List<CarnivalPerformer> candidates)
        {
            string rarity = RollShopRarity();
            var matching = new List<CarnivalPerformer>();
            foreach (CarnivalPerformer performer in candidates)
            {
                if (performer.Rarity == rarity)
                    matching.Add(performer);
            }

            if (matching.Count == 0)
            {
                foreach (CarnivalPerformer performer in candidates)
                {
                    if (performer.Rarity != "传说")
                        matching.Add(performer);
                }
            }

            return matching.Count == 0 ? null : matching[_random.Next(matching.Count)];
        }

        private string RollShopRarity()
        {
            double roll = _random.NextDouble();
            if (roll < 0.70)
                return "普通";
            return roll < 0.95 ? "罕见" : "稀有";
        }

    }
}
