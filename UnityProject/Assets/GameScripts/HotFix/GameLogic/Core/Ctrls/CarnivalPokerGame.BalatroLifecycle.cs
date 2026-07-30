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
                int size = HandSize + _handSizeModifier;
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
            ApplyJokers(
                CarnivalJokerTrigger.BlindSelected,
                new CarnivalJokerContext
                {
                    Trigger = CarnivalJokerTrigger.BlindSelected,
                });
        }

        private void ApplyAfterInitialDrawJokers()
        {
            ApplyJokers(
                CarnivalJokerTrigger.InitialHandDrawn,
                new CarnivalJokerContext
                {
                    Trigger = CarnivalJokerTrigger.InitialHandDrawn,
                });
        }

        private void ApplyAfterHandPlayedJokers(
            List<CarnivalCard> playedCards,
            CarnivalScoreResult result)
        {
            ApplyJokers(
                CarnivalJokerTrigger.AfterHandScored,
                new CarnivalJokerContext
                {
                    Trigger = CarnivalJokerTrigger.AfterHandScored,
                    PlayedCards = playedCards,
                    ScoreResult = result,
                    HandKind = result.Kind,
                });
        }

        private void ApplyDiscardJokers(List<CarnivalCard> discardedCards)
        {
            bool firstDiscard = !_firstDiscardUsedThisRound;
            _firstDiscardUsedThisRound = true;
            _discardsUsedThisRound++;
            _cardsDiscardedThisRun += discardedCards.Count;

            CarnivalHandKind discardedKind = ResolveDiscardedHandKind(discardedCards);
            ApplyJokers(
                CarnivalJokerTrigger.CardDiscarded,
                new CarnivalJokerContext
                {
                    Trigger = CarnivalJokerTrigger.CardDiscarded,
                    PlayedCards = discardedCards,
                    HandKind = discardedKind,
                    IsFirstDiscard = firstDiscard,
                });

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

            int heldGoldTriggers = 1 + CountJokerAbilityOccurrences("mime");
            foreach (CarnivalCard card in _hand)
            {
                if (card.Enhancement == CarnivalCardEnhancement.Gold)
                {
                    CarnivalCardEnhancementContent enhancement =
                        _contentModel.FindEnhancement(card.Enhancement);
                    Money += enhancement.HeldMoney * heldGoldTriggers;
                }
            }

            ApplyJokers(
                CarnivalJokerTrigger.RoundEnded,
                new CarnivalJokerContext
                {
                    Trigger = CarnivalJokerTrigger.RoundEnded,
                });

            foreach (CarnivalPerformer performer in _performers)
            {
                if (GetJokerState(performer).Rental)
                    Money -= 3;
            }

            int interestCap = 5 + 5 * CountJokerAbilityOccurrences("to_the_moon");
            Money += Math.Min(interestCap, Math.Max(0, Money / 5));
        }

        private void ApplyEndOfShopJokers()
        {
            ApplyJokers(
                CarnivalJokerTrigger.ShopEnded,
                new CarnivalJokerContext
                {
                    Trigger = CarnivalJokerTrigger.ShopEnded,
                });
        }

        private void NotifyPlayingCardAdded()
        {
            ApplyJokers(
                CarnivalJokerTrigger.PlayingCardAdded,
                new CarnivalJokerContext
                {
                    Trigger = CarnivalJokerTrigger.PlayingCardAdded,
                });
        }

        private bool TryCreateConsumable(
            CarnivalConsumableFamily family,
            CarnivalHandKind? handKind = null)
        {
            if (!HasConsumableSlot())
                return false;

            var candidates = new List<CarnivalConsumable>();
            foreach (CarnivalConsumable consumable in _contentModel.Consumables)
            {
                if (consumable.Family == family &&
                    (!handKind.HasValue || consumable.HandKind == handKind))
                    candidates.Add(consumable);
            }

            if (candidates.Count == 0)
                return false;
            AddOwnedConsumable(candidates[_random.Next(candidates.Count)]);
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
            return EvaluateHand(cards).Kind;
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
