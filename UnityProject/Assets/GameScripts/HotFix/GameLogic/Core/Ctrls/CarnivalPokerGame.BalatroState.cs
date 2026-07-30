using System;
using System.Collections.Generic;

namespace GameLogic.Core
{
    public sealed partial class CarnivalPokerGame
    {
        private int MaxPerformerSlots
        {
            get
            {
                int slots = MaxPerformers;
                foreach (CarnivalJokerState state in _jokerStates.Values)
                {
                    if (state.Edition == CarnivalCardEdition.Negative)
                        slots++;
                }

                return slots;
            }
        }

        private CarnivalPerformer AddOwnedPerformer(CarnivalPerformer content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            CarnivalPerformer performer = content.CreateRuntimeCopy();
            _performers.Add(performer);
            var state = new CarnivalJokerState(performer);
            _jokerStates.Add(performer, state);
            InitializeJokerRoundTarget(performer, state);
            return performer;
        }

        private void RemoveOwnedPerformer(CarnivalPerformer performer)
        {
            if (performer == null)
                return;

            _performers.Remove(performer);
            _jokerStates.Remove(performer);
        }

        private CarnivalJokerState GetJokerState(CarnivalPerformer performer)
        {
            if (_jokerStates.TryGetValue(performer, out CarnivalJokerState state))
                return state;

            state = new CarnivalJokerState(performer);
            _jokerStates.Add(performer, state);
            return state;
        }

        private bool HasJoker(string performerId)
        {
            foreach (CarnivalPerformer performer in _performers)
            {
                if (performer.Id == performerId)
                    return true;
            }

            return false;
        }

        private CarnivalPerformer FindOwnedJoker(string performerId)
        {
            foreach (CarnivalPerformer performer in _performers)
            {
                if (performer.Id == performerId)
                    return performer;
            }

            return null;
        }

        private void ResetBalatroRunState()
        {
            _jokerStates.Clear();
            _handPlayCounts.Clear();
            _roundHandPlayCounts.Clear();
            _usedPlanetKinds.Clear();
            _handsPlayedThisRound = 0;
            _discardsUsedThisRound = 0;
            _cardsDiscardedThisRun = 0;
            _blindsSkippedThisRun = 0;
            _shopRerollsThisRun = 0;
            _tarotCardsUsedThisRun = 0;
            _freeRerolls = 0;
            _doubleTagCount = 0;
            _tagsCollectedThisRun = 0;
            _investmentTagCount = 0;
            _firstDiscardUsedThisRound = false;
            _bossBlindDisabled = false;
            _grosMichelExtinct = false;
            _couponShopPending = false;
            _couponShopActive = false;
            _d6TagPending = false;
            _currentBoosterPack = null;
            _openedBoosterPack = null;
            _currentBlindTag = null;
            _boosterChoices.Clear();
        }

        private void ResetBalatroRoundState()
        {
            _roundHandPlayCounts.Clear();
            _handsPlayedThisRound = 0;
            _discardsUsedThisRound = 0;
            _firstDiscardUsedThisRound = false;
            _bossBlindDisabled = false;

            foreach (CarnivalPerformer performer in _performers)
            {
                CarnivalJokerState state = GetJokerState(performer);
                state.Active = false;
                if (performer.Id == "hit_the_road")
                    state.Value = 1f;
                InitializeJokerRoundTarget(performer, state);
            }
        }

        private void InitializeJokerRoundTarget(
            CarnivalPerformer performer,
            CarnivalJokerState state)
        {
            switch (performer.Id)
            {
                case "ancient":
                case "castle":
                    state.Suit = (CarnivalSuit)_random.Next(0, 4);
                    break;
                case "idol":
                    state.Suit = (CarnivalSuit)_random.Next(0, 4);
                    state.Rank = _random.Next(2, 15);
                    break;
                case "mail":
                    state.Rank = _random.Next(2, 15);
                    break;
                case "todo_list":
                    state.Rank = _random.Next(0, 12);
                    break;
            }
        }

        private static bool IsFaceCard(CarnivalCard card, bool pareidolia)
        {
            return pareidolia || card.IsFace;
        }

        private bool IsSuit(CarnivalCard card, CarnivalSuit suit)
        {
            if (card.Enhancement == CarnivalCardEnhancement.Wild)
                return true;

            if (!HasJoker("smeared"))
                return card.Suit == suit;

            bool targetRed = suit == CarnivalSuit.Hearts || suit == CarnivalSuit.Diamonds;
            return card.IsRed == targetRed;
        }

        private static int IncrementCount(
            Dictionary<CarnivalHandKind, int> counts,
            CarnivalHandKind kind)
        {
            counts.TryGetValue(kind, out int count);
            count++;
            counts[kind] = count;
            return count;
        }
    }
}
