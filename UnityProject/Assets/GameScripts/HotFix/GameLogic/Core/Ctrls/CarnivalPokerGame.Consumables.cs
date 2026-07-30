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
            if (!ApplyConsumable(consumable, state.OccupiesSlot))
                return false;

            if (consumable.Family == CarnivalConsumableFamily.Tarot)
                _tarotCardsUsedThisRun++;
            else if (consumable.Family == CarnivalConsumableFamily.Planet && consumable.HandKind.HasValue)
                _usedPlanetKinds.Add(consumable.HandKind.Value);

            if (consumable.Id != "tarot-fool" &&
                (consumable.Family == CarnivalConsumableFamily.Tarot ||
                 consumable.Family == CarnivalConsumableFamily.Planet))
            {
                _lastUsedConsumable = consumable;
            }

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

        private bool ApplyConsumable(CarnivalConsumable consumable, bool releasesOccupiedSlot)
        {
            switch (consumable.Action)
            {
                case CarnivalConsumableAction.EnhanceSelected:
                    return EnhanceSelected(consumable, consumable.MaxSelected, consumable.Enhancement);
                case CarnivalConsumableAction.ShiftSelectedRanks:
                    return ShiftSelectedRanks(consumable, consumable.MaxSelected, consumable.Amount);
                case CarnivalConsumableAction.UpgradeHand:
                    return UpgradeConfiguredHand(consumable);
                case CarnivalConsumableAction.CreateLastConsumable:
                    return CreateLastConsumable(consumable, releasesOccupiedSlot);
                case CarnivalConsumableAction.CreateRandomConsumables:
                    return CreateRandomConsumables(consumable, releasesOccupiedSlot);
                case CarnivalConsumableAction.DoubleMoneyCapped:
                    int moneyGain = Math.Min(consumable.Amount, Math.Max(0, Money));
                    Money += moneyGain;
                    StatusMessage = $"{consumable.Name}获得了 ${moneyGain}。";
                    return true;
                case CarnivalConsumableAction.AddRandomJokerEditionChance:
                    return AddRandomJokerEditionChance(consumable);
                case CarnivalConsumableAction.AddJokerSellValueMoney:
                    int sellValueGain = Math.Min(consumable.Amount, SumJokerSellValues());
                    Money += sellValueGain;
                    StatusMessage = $"{consumable.Name}获得了 ${sellValueGain}。";
                    return true;
                case CarnivalConsumableAction.AddRandomJoker:
                    return AddRandomJoker(consumable, consumable.Rarity, false);
                case CarnivalConsumableAction.DestroyRandomAndCreateEnhancedCards:
                    return DestroyRandomAndCreateEnhancedCards(consumable);
                case CarnivalConsumableAction.AddSealSelected:
                    return AddSealSelected(consumable);
                case CarnivalConsumableAction.AddRandomEditionSelected:
                    return AddRandomEditionSelected(consumable);
                case CarnivalConsumableAction.AddRandomJokerAndClearMoney:
                    if (!AddRandomJoker(consumable, consumable.Rarity, false))
                        return false;
                    Money = 0;
                    return true;
                case CarnivalConsumableAction.ChangeWholeHandRandomSuit:
                    ChangeWholeHandSuit((CarnivalSuit)_random.Next(0, 4));
                    StatusMessage = $"{consumable.Name}统一了整手牌的花色。";
                    return true;
                case CarnivalConsumableAction.UnifyHandRankAndReduceHandSize:
                    return UnifyHandRankAndReduceHandSize(consumable);
                case CarnivalConsumableAction.AddRandomJokerEditionAndReduceHandSize:
                    return AddRandomJokerEditionAndReduceHandSize(consumable);
                case CarnivalConsumableAction.DestroyRandomCardsAndAddMoney:
                    return DestroyRandomCardsAndAddMoney(consumable);
                case CarnivalConsumableAction.CopyRandomJokerAndDestroyOthers:
                    return CopyRandomJokerAndDestroyOthers(consumable);
                case CarnivalConsumableAction.AddJokerEditionAndDestroyOthers:
                    return AddJokerEditionAndDestroyOthers(consumable);
                case CarnivalConsumableAction.CopySelectedCardToHand:
                    return CopySelectedCardToHand(consumable);
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

        private bool CreateLastConsumable(CarnivalConsumable consumable, bool releasesOccupiedSlot)
        {
            if (_lastUsedConsumable == null ||
                GetAvailableConsumableSlots(releasesOccupiedSlot) <= 0)
            {
                StatusMessage = $"{consumable.Name}没有可复制的塔罗牌或星球牌。";
                return false;
            }

            AddOwnedConsumable(_lastUsedConsumable);
            StatusMessage = $"{consumable.Name}生成了「{_lastUsedConsumable.Name}」。";
            return true;
        }

        private bool CreateRandomConsumables(CarnivalConsumable consumable, bool releasesOccupiedSlot)
        {
            if (!consumable.CreatedFamily.HasValue)
                return false;

            int createCount = Math.Min(
                consumable.Amount,
                GetAvailableConsumableSlots(releasesOccupiedSlot));
            if (createCount <= 0)
            {
                StatusMessage = "消耗牌栏已满，无法生成新牌。";
                return false;
            }

            var candidates = new List<CarnivalConsumable>();
            bool allowDuplicates = HasJoker("ring_master");
            foreach (CarnivalConsumable candidate in _contentModel.Consumables)
            {
                if (candidate.Family == consumable.CreatedFamily.Value &&
                    (allowDuplicates || !HasConsumable(candidate.Id)))
                {
                    candidates.Add(candidate);
                }
            }

            if (candidates.Count == 0)
                return false;

            int created = 0;
            while (created < createCount && candidates.Count > 0)
            {
                int index = _random.Next(candidates.Count);
                AddOwnedConsumable(candidates[index]);
                created++;
                if (!allowDuplicates)
                    candidates.RemoveAt(index);
            }

            StatusMessage = $"{consumable.Name}生成了 {created} 张{FamilyText(consumable.CreatedFamily.Value)}。";
            return created > 0;
        }

        private int GetAvailableConsumableSlots(bool releasesOccupiedSlot)
        {
            return Math.Max(
                0,
                MaxConsumables -
                CountOccupiedConsumableSlots() +
                (releasesOccupiedSlot ? 1 : 0));
        }

        private int SumJokerSellValues()
        {
            int total = 0;
            foreach (CarnivalPerformer performer in _performers)
                total += GetJokerState(performer).SellValue;
            return total;
        }

        private bool AddRandomJokerEditionChance(CarnivalConsumable consumable)
        {
            var candidates = new List<CarnivalPerformer>();
            foreach (CarnivalPerformer performer in _performers)
            {
                if (GetJokerState(performer).Edition == CarnivalCardEdition.Base)
                    candidates.Add(performer);
            }

            if (candidates.Count == 0)
                return false;

            if (!RollChance(Math.Max(1, consumable.Amount)))
            {
                StatusMessage = $"{consumable.Name}没有产生效果。";
                return true;
            }

            CarnivalPerformer target = candidates[_random.Next(candidates.Count)];
            GetJokerState(target).Edition = RandomPositiveEdition();
            StatusMessage = $"{consumable.Name}为「{target.Name}」添加了版本。";
            return true;
        }

        private bool AddRandomJoker(
            CarnivalConsumable consumable,
            string rarity,
            bool clearMoney)
        {
            if (_performers.Count >= MaxPerformerSlots)
            {
                StatusMessage = "小丑牌槽位已满。";
                return false;
            }

            var candidates = new List<CarnivalPerformer>();
            bool allowDuplicates = HasJoker("ring_master");
            foreach (CarnivalPerformer performer in _contentModel.Performers)
            {
                if (rarity != "传说" &&
                    !performer.UnlockedByDefault &&
                    !_unlockModel.IsJokerUnlocked(performer.Id))
                    continue;
                if (!string.IsNullOrEmpty(rarity))
                {
                    if (performer.Rarity != rarity)
                        continue;
                }
                else if (performer.Rarity == "传说")
                {
                    continue;
                }

                if (allowDuplicates || !HasJoker(performer.Id))
                    candidates.Add(performer);
            }

            if (candidates.Count == 0)
                return false;

            CarnivalPerformer selected = candidates[_random.Next(candidates.Count)];
            AddOwnedPerformer(selected);
            if (clearMoney)
                Money = 0;
            StatusMessage = $"{consumable.Name}生成了「{selected.Name}」。";
            return true;
        }

        private bool DestroyRandomAndCreateEnhancedCards(CarnivalConsumable consumable)
        {
            if (_hand.Count < consumable.SecondaryAmount || consumable.SecondaryAmount <= 0)
                return false;

            var destroyCandidates = new List<CarnivalCard>(_hand);
            Shuffle(destroyCandidates);
            for (int index = 0; index < consumable.SecondaryAmount; index++)
            {
                DestroyPlayingCard(
                    destroyCandidates[index].Id,
                    CarnivalDestroyReason.Consumable);
            }

            for (int index = 0; index < consumable.Amount; index++)
            {
                int rank;
                switch (consumable.Rarity)
                {
                    case "Face":
                        rank = _random.Next(11, 14);
                        break;
                    case "Ace":
                        rank = 14;
                        break;
                    default:
                        rank = _random.Next(2, 11);
                        break;
                }

                var enhancement = (CarnivalCardEnhancement)_random.Next(
                    1,
                    Enum.GetValues(typeof(CarnivalCardEnhancement)).Length);
                var card = new CarnivalCard(
                    _nextCardId++,
                    (CarnivalSuit)_random.Next(0, 4),
                    rank,
                    enhancement);
                _hand.Add(card);
                NotifyPlayingCardAdded();
            }

            StatusMessage = $"{consumable.Name}生成了 {consumable.Amount} 张已增强扑克牌。";
            return true;
        }

        private bool AddSealSelected(CarnivalConsumable consumable)
        {
            List<CarnivalCard> cards = GetSelectedCards();
            if (cards.Count != consumable.MaxSelected)
            {
                StatusMessage = $"{consumable.Name}需要选择恰好 {consumable.MaxSelected} 张牌。";
                return false;
            }

            foreach (CarnivalCard card in cards)
                ReplaceCard(card.WithSeal(consumable.Seal));
            _selectedCardIds.Clear();
            StatusMessage = $"{consumable.Name}添加了{consumable.Seal}蜡封。";
            return true;
        }

        private bool AddRandomEditionSelected(CarnivalConsumable consumable)
        {
            List<CarnivalCard> cards = GetSelectedCards();
            if (cards.Count != consumable.MaxSelected)
            {
                StatusMessage = $"{consumable.Name}需要选择恰好 {consumable.MaxSelected} 张牌。";
                return false;
            }

            foreach (CarnivalCard card in cards)
                ReplaceCard(card.WithEdition(RandomPositiveEdition()));
            _selectedCardIds.Clear();
            StatusMessage = $"{consumable.Name}为所选牌添加了版本。";
            return true;
        }

        private bool UnifyHandRankAndReduceHandSize(CarnivalConsumable consumable)
        {
            if (_hand.Count == 0)
                return false;

            int targetRank = _random.Next(2, 15);
            for (int index = 0; index < _hand.Count; index++)
                _hand[index] = _hand[index].WithRank(targetRank);
            _handSizeModifier += consumable.Amount;
            TrimHandToCurrentSize();
            StatusMessage = $"{consumable.Name}将整手牌化为 {RankText(targetRank)}。";
            return true;
        }

        private bool AddRandomJokerEditionAndReduceHandSize(CarnivalConsumable consumable)
        {
            if (_performers.Count == 0)
                return false;

            CarnivalPerformer target = _performers[_random.Next(_performers.Count)];
            GetJokerState(target).Edition = consumable.Edition;
            _handSizeModifier += consumable.Amount;
            TrimHandToCurrentSize();
            StatusMessage = $"{consumable.Name}为「{target.Name}」添加了负片版本。";
            return true;
        }

        private bool DestroyRandomCardsAndAddMoney(CarnivalConsumable consumable)
        {
            if (consumable.Amount <= 0 || _hand.Count < consumable.Amount)
                return false;

            var candidates = new List<CarnivalCard>(_hand);
            Shuffle(candidates);
            for (int index = 0; index < consumable.Amount; index++)
                DestroyPlayingCard(candidates[index].Id, CarnivalDestroyReason.Consumable);
            Money += consumable.SecondaryAmount;
            StatusMessage = $"{consumable.Name}摧毁了 {consumable.Amount} 张牌并获得 ${consumable.SecondaryAmount}。";
            return true;
        }

        private bool CopyRandomJokerAndDestroyOthers(CarnivalConsumable consumable)
        {
            if (_performers.Count == 0)
                return false;

            CarnivalPerformer source = _performers[_random.Next(_performers.Count)];
            CarnivalJokerState sourceState = GetJokerState(source);
            RemoveOtherNonEternalJokers(source);
            if (_performers.Count >= MaxPerformerSlots)
                return false;

            CarnivalPerformer copy = AddOwnedPerformer(source);
            CopyJokerState(sourceState, GetJokerState(copy), true);
            StatusMessage = $"{consumable.Name}复制了「{source.Name}」。";
            return true;
        }

        private bool AddJokerEditionAndDestroyOthers(CarnivalConsumable consumable)
        {
            if (_performers.Count == 0)
                return false;

            CarnivalPerformer target = _performers[_random.Next(_performers.Count)];
            GetJokerState(target).Edition = consumable.Edition;
            RemoveOtherNonEternalJokers(target);
            StatusMessage = $"{consumable.Name}为「{target.Name}」添加了多彩版本。";
            return true;
        }

        private bool CopySelectedCardToHand(CarnivalConsumable consumable)
        {
            List<CarnivalCard> cards = GetSelectedCards();
            if (cards.Count != consumable.MaxSelected)
            {
                StatusMessage = $"{consumable.Name}需要选择恰好 {consumable.MaxSelected} 张牌。";
                return false;
            }

            CarnivalCard source = cards[0];
            for (int index = 0; index < consumable.Amount; index++)
            {
                var copy = new CarnivalCard(
                    _nextCardId++,
                    source.Suit,
                    source.Rank,
                    source.Enhancement,
                    source.Seal,
                    source.Edition,
                    source.PermanentChips);
                _hand.Add(copy);
                NotifyPlayingCardAdded();
            }

            _selectedCardIds.Clear();
            StatusMessage = $"{consumable.Name}生成了 {consumable.Amount} 张副本。";
            return true;
        }

        private void RemoveOtherNonEternalJokers(CarnivalPerformer preserved)
        {
            var snapshot = new List<CarnivalPerformer>(_performers);
            foreach (CarnivalPerformer performer in snapshot)
            {
                if (!ReferenceEquals(performer, preserved) && !GetJokerState(performer).Eternal)
                    RemoveOwnedPerformer(performer);
            }
        }

        private static void CopyJokerState(
            CarnivalJokerState source,
            CarnivalJokerState destination,
            bool removeNegativeEdition)
        {
            destination.Value = source.Value;
            destination.Counter = source.Counter;
            destination.SecondaryCounter = source.SecondaryCounter;
            destination.SellValue = source.SellValue;
            destination.Rank = source.Rank;
            destination.Suit = source.Suit;
            destination.Active = source.Active;
            destination.Edition =
                removeNegativeEdition && source.Edition == CarnivalCardEdition.Negative
                    ? CarnivalCardEdition.Base
                    : source.Edition;
            destination.Eternal = source.Eternal;
            destination.PerishableRounds = source.PerishableRounds;
            destination.Rental = source.Rental;
        }

        private CarnivalCardEdition RandomPositiveEdition()
        {
            return (CarnivalCardEdition)_random.Next(
                (int)CarnivalCardEdition.Foil,
                (int)CarnivalCardEdition.Polychrome + 1);
        }

        private void TrimHandToCurrentSize()
        {
            while (_hand.Count > CurrentHandSize)
            {
                int index = _random.Next(_hand.Count);
                _deck.Add(_hand[index]);
                _hand.RemoveAt(index);
            }
        }

        private static string FamilyText(CarnivalConsumableFamily family)
        {
            switch (family)
            {
                case CarnivalConsumableFamily.Tarot:
                    return "塔罗牌";
                case CarnivalConsumableFamily.Planet:
                    return "星球牌";
                default:
                    return "幻灵牌";
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

            CarnivalCard target = cards[0];
            CarnivalCard source = cards[1];
            CarnivalCard replacement = copySuit
                ? new CarnivalCard(
                    target.Id,
                    source.Suit,
                    source.Rank,
                    source.Enhancement,
                    source.Seal,
                    source.Edition,
                    source.PermanentChips)
                : target.WithRank(source.Rank);
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
