using System;
using System.Collections.Generic;

namespace GameLogic.Core
{
    public sealed partial class CarnivalPokerGame
    {
        public bool UseConsumable(string consumableId)
        {
            if (Phase != CarnivalRunPhase.Playing)
                return false;

            CarnivalConsumableState state = _consumables.Find(item =>
                item.RuntimeId == consumableId || item.Id == consumableId);
            if (state == null)
                return false;

            CarnivalConsumable consumable = state.Content;
            if (!ApplyConsumable(consumable))
                return false;

            if (consumable.Family == CarnivalConsumableFamily.Tarot)
                _tarotCardsUsedThisRun++;
            else if (consumable.Family == CarnivalConsumableFamily.Planet && consumable.HandKind.HasValue)
                _usedPlanetKinds.Add(consumable.HandKind.Value);

            ApplyJokers(
                CarnivalJokerTrigger.ConsumableUsed,
                new CarnivalJokerContext
                {
                    Trigger = CarnivalJokerTrigger.ConsumableUsed,
                    Consumable = state,
                    HandKind = consumable.HandKind,
                });
            _consumables.Remove(state);
            return true;
        }

        private bool ApplyConsumable(CarnivalConsumable consumable)
        {
            switch (consumable.Action)
            {
                case CarnivalConsumableAction.EnhanceSelected:
                    return EnhanceSelected(consumable, consumable.MaxSelected, consumable.Enhancement);
                case CarnivalConsumableAction.ShiftSelectedRanks:
                    return ShiftSelectedRanks(consumable, consumable.MaxSelected, consumable.Amount);
                case CarnivalConsumableAction.UpgradeHand:
                    return UpgradeConfiguredHand(consumable);
                case CarnivalConsumableAction.UpgradeRandomHands:
                    UpgradeRandomHands(consumable.Amount);
                    Money = Math.Max(0, Money + consumable.SecondaryAmount);
                    StatusMessage = $"{consumable.Name}：{consumable.Description}";
                    return true;
                case CarnivalConsumableAction.UnifyHandRank:
                    int targetRank = _hand[_random.Next(_hand.Count)].Rank;
                    for (int i = 0; i < _hand.Count; i++)
                        _hand[i] = _hand[i].WithRank(targetRank);
                    HandsRemaining = Math.Max(1, HandsRemaining + consumable.Amount);
                    StatusMessage = $"{consumable.Name}将整手牌化为 {RankText(targetRank)}。";
                    return true;
                case CarnivalConsumableAction.ChangeSelectedSuit:
                    return ChangeSelectedSuit(consumable, consumable.MaxSelected, consumable.Suit.Value);
                case CarnivalConsumableAction.AddMoney:
                    Money = Math.Max(0, Money + consumable.Amount);
                    StatusMessage = $"{consumable.Name}：{consumable.Description}";
                    return true;
                case CarnivalConsumableAction.CopySelectedCard:
                    return CopySelectedCard(consumable, consumable.BoolValue);
                case CarnivalConsumableAction.DestroySelected:
                    return DestroySelected(consumable, consumable.MaxSelected);
                case CarnivalConsumableAction.RandomizeSelectedRanks:
                    return RandomizeSelectedRanks(consumable, consumable.MaxSelected);
                case CarnivalConsumableAction.CreateRandomConsumable:
                    return CreateRandomConsumable(consumable);
                case CarnivalConsumableAction.AddDiscards:
                    DiscardsRemaining = Math.Max(0, DiscardsRemaining + consumable.Amount);
                    StatusMessage = $"{consumable.Name}：{consumable.Description}";
                    return true;
                case CarnivalConsumableAction.AddHands:
                    HandsRemaining = Math.Max(1, HandsRemaining + consumable.Amount);
                    StatusMessage = $"{consumable.Name}：{consumable.Description}";
                    return true;
                case CarnivalConsumableAction.EnhanceAndUpgradeRandomHand:
                    if (!EnhanceSelected(consumable, consumable.MaxSelected, consumable.Enhancement))
                        return false;
                    UpgradeOneRandomHand();
                    return true;
                case CarnivalConsumableAction.EnhanceAndMoney:
                    if (!EnhanceSelected(consumable, consumable.MaxSelected, consumable.Enhancement))
                        return false;
                    Money = Math.Max(0, Money + consumable.Amount);
                    return true;
                case CarnivalConsumableAction.ChangeWholeHandSuitAndMoney:
                    ChangeWholeHandSuit(consumable.Suit.Value);
                    Money = Math.Max(0, Money + consumable.Amount);
                    StatusMessage = $"{consumable.Name}：{consumable.Description}";
                    return true;
                case CarnivalConsumableAction.DestroyAndUpgradeRandomHand:
                    if (!DestroySelected(consumable, consumable.MaxSelected))
                        return false;
                    UpgradeOneRandomHand();
                    return true;
                case CarnivalConsumableAction.AddMoneyAndDiscards:
                    Money = Math.Max(0, Money + consumable.Amount);
                    DiscardsRemaining = Math.Max(0, DiscardsRemaining + consumable.SecondaryAmount);
                    StatusMessage = $"{consumable.Name}：{consumable.Description}";
                    return true;
                case CarnivalConsumableAction.RandomizeWholeHandRanks:
                    RandomizeWholeHandRanks();
                    StatusMessage = $"{consumable.Name}：{consumable.Description}";
                    return true;
                case CarnivalConsumableAction.EnhanceFaceCards:
                    EnhanceFaceCards(consumable.Enhancement);
                    StatusMessage = $"{consumable.Name}：{consumable.Description}";
                    return true;
                case CarnivalConsumableAction.UpgradeRandomHandsAndClearMoney:
                    UpgradeRandomHands(consumable.Amount);
                    Money = 0;
                    StatusMessage = $"{consumable.Name}：{consumable.Description}";
                    return true;
                case CarnivalConsumableAction.AddRandomLegendaryPerformer:
                    return AddRandomLegendaryPerformer(consumable);
                case CarnivalConsumableAction.UpgradeAllHands:
                    foreach (CarnivalHandLevel level in _handLevels.Values)
                    {
                        for (int index = 0; index < consumable.Amount; index++)
                            level.Upgrade();
                    }
                    StatusMessage = $"{consumable.Name}：{consumable.Description}";
                    return true;
                default:
                    return false;
            }
        }

        private bool UpgradeConfiguredHand(CarnivalConsumable consumable)
        {
            if (!consumable.HandKind.HasValue)
                return false;

            CarnivalHandKind kind = consumable.HandKind.Value;
            for (int index = 0; index < consumable.Amount; index++)
                _handLevels[kind].Upgrade();
            StatusMessage = $"{consumable.Name}：对应牌型升至 Lv.{_handLevels[kind].Level}。";
            return true;
        }

        private bool EnhanceSelected(
            CarnivalConsumable consumable,
            int maximum,
            CarnivalCardEnhancement enhancement)
        {
            List<CarnivalCard> cards = GetSelectedCards();
            if (cards.Count == 0 || cards.Count > maximum)
            {
                StatusMessage = $"{consumable.Name}需要选择 1–{maximum} 张牌。";
                return false;
            }

            foreach (CarnivalCard card in cards)
                ReplaceCard(card.WithEnhancement(enhancement));

            _selectedCardIds.Clear();
            StatusMessage = $"{consumable.Name}强化了 {cards.Count} 张牌。";
            return true;
        }

        private void UpgradeRandomHands(int count)
        {
            var kinds = new List<CarnivalHandKind>(_handLevels.Keys);
            Shuffle(kinds);
            for (int i = 0; i < Math.Min(count, kinds.Count); i++)
                _handLevels[kinds[i]].Upgrade();
        }

        private void UpgradeOneRandomHand()
        {
            var kinds = new List<CarnivalHandKind>(_handLevels.Keys);
            _handLevels[kinds[_random.Next(kinds.Count)]].Upgrade();
        }

        private bool ChangeSelectedSuit(CarnivalConsumable consumable, int maximum, CarnivalSuit suit)
        {
            List<CarnivalCard> cards = GetSelectedCards();
            if (cards.Count == 0 || cards.Count > maximum)
            {
                StatusMessage = $"{consumable.Name}需要选择 1–{maximum} 张牌。";
                return false;
            }

            foreach (CarnivalCard card in cards)
                ReplaceCard(card.WithSuit(suit));
            _selectedCardIds.Clear();
            StatusMessage = $"{consumable.Name}改变了 {cards.Count} 张牌的花色。";
            return true;
        }

        private bool CopySelectedCard(CarnivalConsumable consumable, bool copySuit)
        {
            List<CarnivalCard> cards = GetSelectedCards();
            if (cards.Count != consumable.MaxSelected)
            {
                StatusMessage = $"{consumable.Name}需要选择恰好 {consumable.MaxSelected} 张牌。";
                return false;
            }

            CarnivalCard replacement = cards[1].WithRank(cards[0].Rank);
            if (copySuit)
                replacement = replacement.WithSuit(cards[0].Suit);
            ReplaceCard(replacement);
            _selectedCardIds.Clear();
            StatusMessage = $"{consumable.Name}完成了牌面复制。";
            return true;
        }

        private bool DestroySelected(CarnivalConsumable consumable, int maximum)
        {
            List<CarnivalCard> cards = GetSelectedCards();
            if (cards.Count == 0 || cards.Count > maximum)
            {
                StatusMessage = $"{consumable.Name}需要选择 1–{maximum} 张牌。";
                return false;
            }

            foreach (CarnivalCard card in cards)
                DestroyPlayingCard(card.Id, CarnivalDestroyReason.Consumable);
            _selectedCardIds.Clear();
            DrawToHandSize();
            StatusMessage = $"{consumable.Name}摧毁了 {cards.Count} 张牌。";
            return true;
        }

        private bool RandomizeSelectedRanks(CarnivalConsumable consumable, int maximum)
        {
            List<CarnivalCard> cards = GetSelectedCards();
            if (cards.Count == 0 || cards.Count > maximum)
            {
                StatusMessage = $"{consumable.Name}需要选择 1–{maximum} 张牌。";
                return false;
            }

            foreach (CarnivalCard card in cards)
                ReplaceCard(card.WithRank(_random.Next(2, 15)));
            _selectedCardIds.Clear();
            StatusMessage = $"{consumable.Name}重排了 {cards.Count} 张牌的点数。";
            return true;
        }

        private bool ShiftSelectedRanks(CarnivalConsumable consumable, int maximum, int amount)
        {
            List<CarnivalCard> cards = GetSelectedCards();
            if (cards.Count == 0 || cards.Count > maximum)
            {
                StatusMessage = $"{consumable.Name}需要选择 1–{maximum} 张牌。";
                return false;
            }

            foreach (CarnivalCard card in cards)
            {
                int rank = card.Rank + amount;
                if (rank < 2)
                    rank = 14;
                else if (rank > 14)
                    rank = 2;
                ReplaceCard(card.WithRank(rank));
            }

            _selectedCardIds.Clear();
            StatusMessage = $"{consumable.Name}改变了 {cards.Count} 张牌的点数。";
            return true;
        }

        private bool CreateRandomConsumable(CarnivalConsumable source)
        {
            if (!HasConsumableSlot(1))
            {
                StatusMessage = "消耗牌栏已满，无法生成新牌。";
                return false;
            }

            var candidates = new List<CarnivalConsumable>(_contentModel.Consumables);
            candidates.RemoveAll(item => item.Id == source.Id);
            AddOwnedConsumable(candidates[_random.Next(candidates.Count)]);
            StatusMessage = $"{source.Name}生成了 1 张随机消耗牌。";
            return true;
        }

        private void ChangeWholeHandSuit(CarnivalSuit suit)
        {
            for (int i = 0; i < _hand.Count; i++)
                _hand[i] = _hand[i].WithSuit(suit);
        }

        private void RandomizeWholeHandRanks()
        {
            for (int i = 0; i < _hand.Count; i++)
                _hand[i] = _hand[i].WithRank(_random.Next(2, 15));
        }

        private void EnhanceFaceCards(CarnivalCardEnhancement enhancement)
        {
            for (int i = 0; i < _hand.Count; i++)
            {
                if (_hand[i].IsFace)
                    _hand[i] = _hand[i].WithEnhancement(enhancement);
            }
        }

        private bool AddRandomLegendaryPerformer(CarnivalConsumable consumable)
        {
            if (_performers.Count >= MaxPerformerSlots)
            {
                StatusMessage = "表演者席位已满，无法召来新成员。";
                return false;
            }

            var candidates = new List<CarnivalPerformer>();
            foreach (CarnivalPerformer performer in _contentModel.Performers)
            {
                if (performer.Rarity == "传说" && !HasJoker(performer.Id))
                    candidates.Add(performer);
            }

            if (candidates.Count == 0)
                return false;

            CarnivalPerformer selected = candidates[_random.Next(candidates.Count)];
            AddOwnedPerformer(selected);
            StatusMessage = $"{consumable.Name}召来了「{selected.Name}」。";
            return true;
        }

        private static string RankText(int rank)
        {
            if (rank == 11)
                return "J";
            if (rank == 12)
                return "Q";
            if (rank == 13)
                return "K";
            if (rank == 14)
                return "A";
            return rank.ToString();
        }
    }
}
