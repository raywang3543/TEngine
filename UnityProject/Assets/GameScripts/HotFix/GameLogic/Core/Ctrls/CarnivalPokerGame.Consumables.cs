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

            CarnivalConsumable consumable = _consumables.Find(item => item.Id == consumableId);
            if (consumable == null)
                return false;

            if (!ApplyConsumable(consumable))
                return false;

            _consumables.Remove(consumable);
            return true;
        }

        private bool ApplyConsumable(CarnivalConsumable consumable)
        {
            switch (consumable.Id)
            {
                case "tarot-forge":
                    return EnhanceSelected(consumable, 2, CarnivalCardEnhancement.Bonus);
                case "tarot-mask":
                    return EnhanceSelected(consumable, 3, CarnivalCardEnhancement.Wild);
                case "tarot-rise":
                    return RaiseSelectedRanks(consumable, 2);
                case "spectral-glass":
                    return EnhanceSelected(consumable, 1, CarnivalCardEnhancement.Glass);
                case "spectral-echo":
                    UpgradeRandomHands();
                    Money = Math.Max(0, Money - 3);
                    StatusMessage = "群星回声升级了 2 种牌型，但夺走了 $3。";
                    return true;
                case "spectral-void":
                    int targetRank = _hand[_random.Next(_hand.Count)].Rank;
                    for (int i = 0; i < _hand.Count; i++)
                        _hand[i] = _hand[i].WithRank(targetRank);
                    HandsRemaining = Math.Max(1, HandsRemaining - 1);
                    StatusMessage = $"虚空契约将整手牌化为 {RankText(targetRank)}，并吞噬 1 次出牌。";
                    return true;
                default:
                    if (consumable.HandKind.HasValue)
                    {
                        CarnivalHandKind kind = consumable.HandKind.Value;
                        _handLevels[kind].Upgrade();
                        StatusMessage = $"{consumable.Name}：对应牌型升至 Lv.{_handLevels[kind].Level}。";
                        return true;
                    }

                    if (consumable.Id.StartsWith("tarot-", StringComparison.Ordinal))
                        return ApplyExpandedTarot(consumable);
                    if (consumable.Id.StartsWith("spectral-", StringComparison.Ordinal))
                        return ApplyExpandedSpectral(consumable);
                    return false;
            }
        }

        private bool ApplyExpandedTarot(CarnivalConsumable consumable)
        {
            switch (consumable.Id)
            {
                case "tarot-04":
                    return EnhanceSelected(consumable, 2, CarnivalCardEnhancement.Mult);
                case "tarot-05":
                    return EnhanceSelected(consumable, 1, CarnivalCardEnhancement.Gold);
                case "tarot-06":
                    return EnhanceSelected(consumable, 1, CarnivalCardEnhancement.Steel);
                case "tarot-07":
                    return EnhanceSelected(consumable, 1, CarnivalCardEnhancement.Lucky);
                case "tarot-08":
                    return EnhanceSelected(consumable, 2, CarnivalCardEnhancement.None);
                case "tarot-09":
                    Money += 8;
                    StatusMessage = $"{consumable.Name}带来了 $8。";
                    return true;
                case "tarot-10":
                    UpgradeRandomHands();
                    StatusMessage = $"{consumable.Name}随机提升了 2 种牌型。";
                    return true;
                case "tarot-11":
                    return ChangeSelectedSuit(consumable, CarnivalSuit.Hearts);
                case "tarot-12":
                    return ChangeSelectedSuit(consumable, CarnivalSuit.Spades);
                case "tarot-13":
                    return ChangeSelectedSuit(consumable, CarnivalSuit.Diamonds);
                case "tarot-14":
                    return ChangeSelectedSuit(consumable, CarnivalSuit.Clubs);
                case "tarot-15":
                    return CopySelectedCard(consumable, true);
                case "tarot-16":
                    return DestroySelected(consumable, 2);
                case "tarot-17":
                    return RandomizeSelectedRanks(consumable, 5);
                case "tarot-18":
                    return ShiftSelectedRanks(consumable, 2, -1);
                case "tarot-19":
                    Money += 5;
                    StatusMessage = $"{consumable.Name}换得 $5。";
                    return true;
                case "tarot-20":
                    return CreateRandomConsumable(consumable);
                case "tarot-21":
                    DiscardsRemaining++;
                    StatusMessage = $"{consumable.Name}恢复了 1 次弃牌。";
                    return true;
                case "tarot-22":
                    HandsRemaining++;
                    StatusMessage = $"{consumable.Name}恢复了 1 次出牌。";
                    return true;
                default:
                    return false;
            }
        }

        private bool ApplyExpandedSpectral(CarnivalConsumable consumable)
        {
            switch (consumable.Id)
            {
                case "spectral-04":
                    if (!EnhanceSelected(consumable, 2, CarnivalCardEnhancement.Mult))
                        return false;
                    UpgradeOneRandomHand();
                    return true;
                case "spectral-05":
                    if (!EnhanceSelected(consumable, 2, CarnivalCardEnhancement.Glass))
                        return false;
                    Money = Math.Max(0, Money - 2);
                    return true;
                case "spectral-06":
                    return EnhanceSelected(consumable, 1, CarnivalCardEnhancement.Gold);
                case "spectral-07":
                    return EnhanceSelected(consumable, 1, CarnivalCardEnhancement.Steel);
                case "spectral-08":
                    return EnhanceSelected(consumable, 1, CarnivalCardEnhancement.Lucky);
                case "spectral-09":
                    return EnhanceSelected(consumable, 1, CarnivalCardEnhancement.Stone);
                case "spectral-10":
                    ChangeWholeHandSuit(CarnivalSuit.Hearts);
                    Money = Math.Max(0, Money - 3);
                    StatusMessage = $"{consumable.Name}将整手牌化为红心，并夺走了 $3。";
                    return true;
                case "spectral-11":
                    return CopySelectedCard(consumable, false);
                case "spectral-12":
                    if (!DestroySelected(consumable, 3))
                        return false;
                    UpgradeOneRandomHand();
                    return true;
                case "spectral-13":
                    Money += 10;
                    DiscardsRemaining = Math.Max(0, DiscardsRemaining - 1);
                    StatusMessage = $"{consumable.Name}给予 $10，但吞噬了 1 次弃牌。";
                    return true;
                case "spectral-14":
                    RandomizeWholeHandRanks();
                    StatusMessage = $"{consumable.Name}重排了整手牌的点数。";
                    return true;
                case "spectral-15":
                    EnhanceFaceCards();
                    StatusMessage = $"{consumable.Name}将所有人头牌强化为倍率牌。";
                    return true;
                case "spectral-16":
                    UpgradeRandomHands(3);
                    Money = 0;
                    StatusMessage = $"{consumable.Name}提升 3 种牌型，并夺走全部金币。";
                    return true;
                case "spectral-17":
                    return AddRandomLegendaryPerformer(consumable);
                case "spectral-18":
                    foreach (CarnivalHandLevel level in _handLevels.Values)
                        level.Upgrade();
                    StatusMessage = $"{consumable.Name}令所有牌型提升 1 级。";
                    return true;
                default:
                    return false;
            }
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

        private bool RaiseSelectedRanks(CarnivalConsumable consumable, int maximum)
        {
            List<CarnivalCard> cards = GetSelectedCards();
            if (cards.Count == 0 || cards.Count > maximum)
            {
                StatusMessage = $"{consumable.Name}需要选择 1–{maximum} 张牌。";
                return false;
            }

            foreach (CarnivalCard card in cards)
                ReplaceCard(card.WithRank(card.Rank == 14 ? 2 : card.Rank + 1));

            _selectedCardIds.Clear();
            StatusMessage = $"{consumable.Name}提升了 {cards.Count} 张牌的点数。";
            return true;
        }

        private void UpgradeRandomHands()
        {
            UpgradeRandomHands(2);
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

        private bool ChangeSelectedSuit(CarnivalConsumable consumable, CarnivalSuit suit)
        {
            List<CarnivalCard> cards = GetSelectedCards();
            if (cards.Count == 0 || cards.Count > 3)
            {
                StatusMessage = $"{consumable.Name}需要选择 1–3 张牌。";
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
            if (cards.Count != 2)
            {
                StatusMessage = $"{consumable.Name}需要选择恰好 2 张牌。";
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
                _hand.RemoveAll(item => item.Id == card.Id);
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
            if (_consumables.Count > MaxConsumables)
            {
                StatusMessage = "消耗牌栏已满，无法生成新牌。";
                return false;
            }

            var candidates = new List<CarnivalConsumable>(_contentModel.Consumables);
            candidates.RemoveAll(item => item.Id == source.Id);
            _consumables.Add(candidates[_random.Next(candidates.Count)]);
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

        private void EnhanceFaceCards()
        {
            for (int i = 0; i < _hand.Count; i++)
            {
                if (_hand[i].IsFace)
                    _hand[i] = _hand[i].WithEnhancement(CarnivalCardEnhancement.Mult);
            }
        }

        private bool AddRandomLegendaryPerformer(CarnivalConsumable consumable)
        {
            if (_performers.Count >= MaxPerformers)
            {
                StatusMessage = "表演者席位已满，无法召来新成员。";
                return false;
            }

            var candidates = new List<CarnivalPerformer>();
            foreach (CarnivalPerformer performer in _contentModel.Performers)
            {
                if (performer.Rarity == "传说" && !_performers.Contains(performer))
                    candidates.Add(performer);
            }

            if (candidates.Count == 0)
                return false;

            CarnivalPerformer selected = candidates[_random.Next(candidates.Count)];
            _performers.Add(selected);
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
