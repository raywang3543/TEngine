using System;
using System.Collections.Generic;

namespace GameLogic.Core
{
    public sealed partial class CarnivalPokerGame
    {
        public bool BuyPerformer(string performerId)
        {
            if (Phase != CarnivalRunPhase.Shop)
                return false;

            CarnivalShopOffer offer = FindOffer(performerId);
            CarnivalPerformer performer = offer?.Performer;
            if (performer == null)
                return false;

            if (_performers.Count >= MaxPerformers)
            {
                StatusMessage = "表演者席位已满（最多 5 张）。";
                return false;
            }

            if (Money < performer.Cost)
            {
                StatusMessage = $"金币不足，还需要 ${performer.Cost - Money}。";
                return false;
            }

            Money -= performer.Cost;
            _performers.Add(performer);
            _shopOffers.Remove(offer);
            StatusMessage = $"已邀请「{performer.Name}」加入巡演。";
            return true;
        }

        public bool BuyConsumable(string consumableId)
        {
            if (Phase != CarnivalRunPhase.Shop)
                return false;

            CarnivalShopOffer offer = FindOffer(consumableId);
            CarnivalConsumable consumable = offer?.Consumable;
            if (consumable == null)
                return false;

            if (_consumables.Count >= MaxConsumables)
            {
                StatusMessage = $"消耗牌栏已满（最多 {MaxConsumables} 张）。";
                return false;
            }

            if (Money < consumable.Cost)
            {
                StatusMessage = $"金币不足，还需要 ${consumable.Cost - Money}。";
                return false;
            }

            Money -= consumable.Cost;
            _consumables.Add(consumable);
            _shopOffers.Remove(offer);
            StatusMessage = $"获得「{consumable.Name}」。可在盲注中使用。";
            return true;
        }

        public void ContinueFromShop()
        {
            if (Phase != CarnivalRunPhase.Shop)
                return;

            Round++;
            StartRound();
        }

        private void GenerateShop()
        {
            _shopOffers.Clear();
            var candidates = new List<CarnivalPerformer>();
            foreach (CarnivalPerformer performer in _contentModel.Performers)
            {
                if (!_performers.Contains(performer))
                    candidates.Add(performer);
            }

            int offerCount = Math.Min(2, candidates.Count);
            for (int i = 0; i < offerCount; i++)
            {
                CarnivalPerformer performer = DrawShopPerformer(candidates);
                if (performer == null)
                    break;
                _shopOffers.Add(new CarnivalShopOffer(performer));
                candidates.Remove(performer);
            }

            var consumableCandidates = new List<CarnivalConsumable>(_contentModel.Consumables);
            Shuffle(consumableCandidates);
            for (int i = 0; i < 2; i++)
                _shopOffers.Add(new CarnivalShopOffer(consumableCandidates[i]));
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
