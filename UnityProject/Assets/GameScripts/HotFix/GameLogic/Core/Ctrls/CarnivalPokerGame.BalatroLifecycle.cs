using System;
using System.Collections.Generic;

namespace GameLogic.Core
{
    public sealed partial class CarnivalPokerGame
    {
        private int CurrentHandSize
        {
            get
            {
                int size = HandSize;
                foreach (CarnivalPerformer performer in _performers)
                {
                    CarnivalJokerState state = GetJokerState(performer);
                    switch (performer.Id)
                    {
                        case "turtle_bean":
                            size += state.Counter;
                            break;
                        case "juggler":
                            size++;
                            break;
                        case "troubadour":
                            size += 2;
                            break;
                        case "merry_andy":
                            size--;
                            break;
                        case "stuntman":
                            size -= 2;
                            break;
                    }
                }

                return Math.Max(1, size);
            }
        }

        private void ApplyBlindSelectionJokers()
        {
            _bossBlindDisabled = HasJoker("chicot");

            var snapshot = new List<CarnivalPerformer>(_performers);
            foreach (CarnivalPerformer performer in snapshot)
            {
                if (!_performers.Contains(performer))
                    continue;

                CarnivalJokerState state = GetJokerState(performer);
                switch (performer.Id)
                {
                    case "ceremonial":
                        int index = GetPerformerIndex(performer);
                        if (index >= 0 && index + 1 < _performers.Count)
                        {
                            CarnivalPerformer victim = _performers[index + 1];
                            if (!GetJokerState(victim).Eternal)
                            {
                                state.Value += GetJokerState(victim).SellValue * 2;
                                RemoveOwnedPerformer(victim);
                            }
                        }
                        break;
                    case "marble":
                        AddCardToDeck(new CarnivalCard(
                            _nextCardId++,
                            (CarnivalSuit)_random.Next(0, 4),
                            _random.Next(2, 15),
                            CarnivalCardEnhancement.Stone));
                        break;
                    case "burglar":
                        HandsRemaining += 3;
                        DiscardsRemaining = 0;
                        break;
                    case "madness":
                        if (CurrentBlind.Tier != CarnivalBlindTier.Boss)
                        {
                            state.Value += 0.5f;
                            DestroyRandomOtherJoker(performer);
                        }
                        break;
                    case "riff_raff":
                        TryCreateCommonJoker();
                        TryCreateCommonJoker();
                        break;
                    case "cartomancer":
                        TryCreateConsumable(CarnivalConsumableFamily.Tarot);
                        break;
                    case "drunkard":
                        DiscardsRemaining++;
                        break;
                    case "merry_andy":
                        DiscardsRemaining += 3;
                        break;
                    case "troubadour":
                        HandsRemaining = Math.Max(1, HandsRemaining - 1);
                        break;
                }
            }
        }

        private void ApplyAfterInitialDrawJokers()
        {
            foreach (CarnivalPerformer performer in _performers)
            {
                if (performer.Id != "certificate")
                    continue;

                var card = new CarnivalCard(
                    _nextCardId++,
                    (CarnivalSuit)_random.Next(0, 4),
                    _random.Next(2, 15),
                    CarnivalCardEnhancement.None,
                    (CarnivalCardSeal)_random.Next(1, 5));
                _hand.Add(card);
                NotifyPlayingCardAdded();
            }
        }

        private void ApplyAfterHandPlayedJokers(
            List<CarnivalCard> playedCards,
            CarnivalScoreResult result)
        {
            bool hasScoringFace = false;
            foreach (CarnivalCard card in playedCards)
            {
                if (result.ScoringCardIds.Contains(card.Id) && IsFaceCard(card, HasJoker("pareidolia")))
                    hasScoringFace = true;
            }

            var snapshot = new List<CarnivalPerformer>(_performers);
            foreach (CarnivalPerformer performer in snapshot)
            {
                if (!_performers.Contains(performer))
                    continue;

                CarnivalJokerState state = GetJokerState(performer);
                switch (performer.Id)
                {
                    case "loyalty_card":
                        state.Counter = state.Counter == 0 ? 5 : state.Counter - 1;
                        break;
                    case "ride_the_bus":
                        state.Value = hasScoringFace ? 0f : state.Value + 1f;
                        break;
                    case "space":
                        if (RollChance(4))
                            _handLevels[result.Kind].Upgrade();
                        break;
                    case "runner":
                        if (ContainsStraight(result.Kind))
                            state.Value += 15f;
                        break;
                    case "ice_cream":
                        state.Value = Math.Max(0f, state.Value - 5f);
                        break;
                    case "dna":
                        if (_handsPlayedThisRound == 1 && playedCards.Count == 1)
                        {
                            CarnivalCard source = playedCards[0];
                            CarnivalCard copy = new CarnivalCard(
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
                        break;
                    case "sixth_sense":
                        if (_handsPlayedThisRound == 1 && playedCards.Count == 1 && playedCards[0].Rank == 6)
                        {
                            DestroyCardEverywhere(playedCards[0].Id);
                            TryCreateConsumable(CarnivalConsumableFamily.Spectral);
                        }
                        break;
                    case "green_joker":
                        state.Value += 1f;
                        break;
                    case "superposition":
                        if (ContainsStraight(result.Kind) && ContainsRank(playedCards, 14))
                            TryCreateConsumable(CarnivalConsumableFamily.Tarot);
                        break;
                    case "square":
                        if (playedCards.Count == 4)
                            state.Value += 4f;
                        break;
                    case "seance":
                        if (result.Kind == CarnivalHandKind.StraightFlush)
                            TryCreateConsumable(CarnivalConsumableFamily.Spectral);
                        break;
                    case "vagabond":
                        if (Money <= 4)
                            TryCreateConsumable(CarnivalConsumableFamily.Tarot);
                        break;
                    case "obelisk":
                        if (IsMostPlayedHand(result.Kind))
                            state.Value = 1f;
                        else
                            state.Value += 0.2f;
                        break;
                    case "trousers":
                        if (ContainsTwoPair(result.Kind))
                            state.Value += 2f;
                        break;
                    case "selzer":
                        state.Counter = Math.Max(0, state.Counter - 1);
                        break;
                }
            }
        }

        private void ApplyDiscardJokers(List<CarnivalCard> discardedCards)
        {
            bool firstDiscard = !_firstDiscardUsedThisRound;
            _firstDiscardUsedThisRound = true;
            _discardsUsedThisRound++;
            _cardsDiscardedThisRun += discardedCards.Count;

            CarnivalHandKind discardedKind = ResolveDiscardedHandKind(discardedCards);
            var snapshot = new List<CarnivalPerformer>(_performers);
            foreach (CarnivalPerformer performer in snapshot)
            {
                CarnivalJokerState state = GetJokerState(performer);
                switch (performer.Id)
                {
                    case "faceless":
                        if (CountMatches(discardedCards, card => IsFaceCard(card, HasJoker("pareidolia"))) >= 3)
                            Money += 5;
                        break;
                    case "green_joker":
                        state.Value = Math.Max(0f, state.Value - 1f);
                        break;
                    case "mail":
                        Money += 5 * CountMatches(discardedCards, card => card.Rank == state.Rank);
                        break;
                    case "trading":
                        if (firstDiscard && discardedCards.Count == 1)
                        {
                            DestroyCardEverywhere(discardedCards[0].Id);
                            Money += 3;
                        }
                        break;
                    case "ramen":
                        state.Value = Math.Max(1f, state.Value - 0.01f * discardedCards.Count);
                        break;
                    case "castle":
                        state.Value += 3f * CountMatches(discardedCards, card => IsSuit(card, state.Suit));
                        break;
                    case "hit_the_road":
                        state.Value += 0.5f * CountMatches(discardedCards, card => card.Rank == 11);
                        break;
                    case "burnt":
                        if (firstDiscard && discardedCards.Count > 0)
                            _handLevels[discardedKind].Upgrade();
                        break;
                    case "yorick":
                        state.Counter += discardedCards.Count;
                        while (state.Counter >= 23)
                        {
                            state.Counter -= 23;
                            state.Value += 1f;
                        }
                        break;
                }
            }

            foreach (CarnivalCard card in discardedCards)
            {
                if (card.Seal == CarnivalCardSeal.Purple)
                    TryCreateConsumable(CarnivalConsumableFamily.Tarot);
            }
        }

        private void ApplyEndOfRoundJokers()
        {
            if (CurrentBlind.Tier == CarnivalBlindTier.Boss && _investmentTagCount > 0)
            {
                Money += 25 * _investmentTagCount;
                _investmentTagCount = 0;
            }

            int heldGoldTriggers = HasJoker("mime") ? 2 : 1;
            foreach (CarnivalCard card in _hand)
            {
                if (card.Enhancement == CarnivalCardEnhancement.Gold)
                    Money += 3 * heldGoldTriggers;
            }

            var toDestroy = new List<CarnivalPerformer>();
            foreach (CarnivalPerformer performer in _performers)
            {
                CarnivalJokerState state = GetJokerState(performer);
                switch (performer.Id)
                {
                    case "delayed_grat":
                        if (_discardsUsedThisRound == 0)
                            Money += 2 * DiscardsRemaining;
                        break;
                    case "egg":
                        state.SellValue += 3;
                        break;
                    case "gros_michel":
                        if (RollChance(6))
                        {
                            _grosMichelExtinct = true;
                            toDestroy.Add(performer);
                        }
                        break;
                    case "cavendish":
                        if (RollChance(1000))
                            toDestroy.Add(performer);
                        break;
                    case "cloud_9":
                        Money += CountCards(card => card.Rank == 9);
                        break;
                    case "rocket":
                        Money += Math.Max(1, state.Counter + 1);
                        if (CurrentBlind.Tier == CarnivalBlindTier.Boss)
                            state.Counter += 2;
                        break;
                    case "gift":
                        foreach (CarnivalPerformer target in _performers)
                            GetJokerState(target).SellValue++;
                        break;
                    case "turtle_bean":
                        state.Counter = Math.Max(0, state.Counter - 1);
                        break;
                    case "golden":
                        Money += 4;
                        break;
                    case "popcorn":
                        state.Value = Math.Max(0f, state.Value - 4f);
                        if (state.Value <= 0f)
                            toDestroy.Add(performer);
                        break;
                    case "invisible":
                        state.Counter++;
                        break;
                    case "satellite":
                        Money += _usedPlanetKinds.Count;
                        break;
                    case "campfire":
                        if (CurrentBlind.Tier == CarnivalBlindTier.Boss)
                            state.Value = 1f;
                        break;
                }

                if (state.Rental)
                    Money -= 3;
            }

            foreach (CarnivalPerformer performer in toDestroy)
                RemoveOwnedPerformer(performer);

            int interestCap = HasJoker("to_the_moon") ? 10 : 5;
            Money += Math.Min(interestCap, Math.Max(0, Money / 5));
        }

        private void ApplyEndOfShopJokers()
        {
            CarnivalPerformer perkeo = FindOwnedJoker("perkeo");
            if (perkeo == null || _consumables.Count == 0)
                return;

            CarnivalConsumable source = _consumables[_random.Next(_consumables.Count)];
            _consumables.Add(source);
        }

        private void NotifyPlayingCardAdded()
        {
            CarnivalPerformer hologram = FindOwnedJoker("hologram");
            if (hologram != null)
                GetJokerState(hologram).Value += 0.25f;
        }

        private bool TryCreateConsumable(CarnivalConsumableFamily family)
        {
            if (_consumables.Count >= MaxConsumables)
                return false;

            var candidates = new List<CarnivalConsumable>();
            foreach (CarnivalConsumable consumable in _contentModel.Consumables)
            {
                if (consumable.Family == family)
                    candidates.Add(consumable);
            }

            if (candidates.Count == 0)
                return false;
            _consumables.Add(candidates[_random.Next(candidates.Count)]);
            return true;
        }

        private void TryCreateCommonJoker()
        {
            if (_performers.Count >= MaxPerformerSlots)
                return;

            var candidates = new List<CarnivalPerformer>();
            foreach (CarnivalPerformer performer in _contentModel.Performers)
            {
                if (performer.Rarity == "普通" && !HasJoker(performer.Id))
                    candidates.Add(performer);
            }

            if (candidates.Count > 0)
                AddOwnedPerformer(candidates[_random.Next(candidates.Count)]);
        }

        private void DestroyRandomOtherJoker(CarnivalPerformer excluded)
        {
            var candidates = new List<CarnivalPerformer>();
            foreach (CarnivalPerformer performer in _performers)
            {
                if (!ReferenceEquals(performer, excluded) && !GetJokerState(performer).Eternal)
                    candidates.Add(performer);
            }

            if (candidates.Count > 0)
                RemoveOwnedPerformer(candidates[_random.Next(candidates.Count)]);
        }

        private void AddCardToDeck(CarnivalCard card)
        {
            _deck.Add(card);
            NotifyPlayingCardAdded();
        }

        private void ReplaceCardEverywhere(CarnivalCard replacement)
        {
            ReplaceCardIn(_deck, replacement);
            ReplaceCardIn(_hand, replacement);
            ReplaceCardIn(_discardPile, replacement);
        }

        private static void ReplaceCardIn(List<CarnivalCard> cards, CarnivalCard replacement)
        {
            for (int index = 0; index < cards.Count; index++)
            {
                if (cards[index].Id == replacement.Id)
                    cards[index] = replacement;
            }
        }

        private void DestroyCardEverywhere(int cardId)
        {
            _deck.RemoveAll(card => card.Id == cardId);
            _hand.RemoveAll(card => card.Id == cardId);
            _discardPile.RemoveAll(card => card.Id == cardId);
            _selectedCardIds.Remove(cardId);
        }

        private bool IsMostPlayedHand(CarnivalHandKind kind)
        {
            _handPlayCounts.TryGetValue(kind, out int current);
            foreach (int count in _handPlayCounts.Values)
            {
                if (count > current)
                    return false;
            }

            return true;
        }

        private CarnivalHandKind ResolveDiscardedHandKind(List<CarnivalCard> cards)
        {
            if (cards.Count == 0)
                return CarnivalHandKind.HighCard;
            Dictionary<int, List<CarnivalCard>> groups = BuildRankGroups(cards);
            int sequenceSize = HasJoker("four_fingers") ? 4 : 5;
            return ResolveHandKind(
                cards.Count,
                groups,
                HasFlush(cards, sequenceSize),
                HasStraight(cards, sequenceSize, HasJoker("shortcut")));
        }

        private static bool ContainsRank(List<CarnivalCard> cards, int rank)
        {
            foreach (CarnivalCard card in cards)
            {
                if (card.Rank == rank)
                    return true;
            }

            return false;
        }

        private static int CountMatches(
            List<CarnivalCard> cards,
            Predicate<CarnivalCard> predicate)
        {
            int count = 0;
            foreach (CarnivalCard card in cards)
            {
                if (predicate(card))
                    count++;
            }

            return count;
        }
    }
}
