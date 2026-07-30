using System;
using System.Collections.Generic;

namespace GameLogic.Core
{
    public sealed partial class CarnivalPokerGame
    {
        public void SortHandByRank()
        {
            _hand.Sort((left, right) =>
            {
                int rankCompare = right.Rank.CompareTo(left.Rank);
                return rankCompare != 0 ? rankCompare : left.Suit.CompareTo(right.Suit);
            });
        }

        public void SortHandBySuit()
        {
            _hand.Sort((left, right) =>
            {
                int suitCompare = left.Suit.CompareTo(right.Suit);
                return suitCompare != 0 ? suitCompare : right.Rank.CompareTo(left.Rank);
            });
        }

        private void StartRound()
        {
            Phase = CarnivalRunPhase.Playing;
            RoundScore = 0;
            CurrentBlind = CreateBlind(Round);
            EvaluateAnteUnlocks();
            GenerateBlindTag();
            TargetScore = (int)Math.Round(AnteBaseTargets[Ante - 1] * CurrentBlind.ScoreScale);
            HandsRemaining = 4;
            DiscardsRemaining = 3;
            ResetBalatroRoundState();
            ApplyBlindSelectionJokers();
            if (IsBossRuleActive(CarnivalBossRule.LoseDiscard))
                DiscardsRemaining--;
            LastResult = null;
            StatusMessage = $"{CurrentBlind.Name}：{CurrentBlind.Description}";
            _selectedCardIds.Clear();
            PrepareDeckForRound();
            DrawToHandSize();
            ApplyAfterInitialDrawJokers();
            SortHandByRank();
        }

        private void BuildStandardDeck()
        {
            _deck.Clear();
            foreach (CarnivalSuit suit in Enum.GetValues(typeof(CarnivalSuit)))
            {
                for (int rank = 2; rank <= 14; rank++)
                    _deck.Add(new CarnivalCard(_nextCardId++, suit, rank));
            }
            _startingDeckSize = _deck.Count;
        }

        private void PrepareDeckForRound()
        {
            _deck.AddRange(_hand);
            _deck.AddRange(_discardPile);
            _hand.Clear();
            _discardPile.Clear();
            Shuffle(_deck);
        }

        private void DrawToHandSize()
        {
            while (_hand.Count < CurrentHandSize)
            {
                if (_deck.Count == 0)
                {
                    if (_discardPile.Count == 0)
                        break;

                    _deck.AddRange(_discardPile);
                    _discardPile.Clear();
                    Shuffle(_deck);
                }

                int lastIndex = _deck.Count - 1;
                _hand.Add(_deck[lastIndex]);
                _deck.RemoveAt(lastIndex);
            }
        }

        private List<CarnivalCard> GetSelectedCards()
        {
            var cards = new List<CarnivalCard>(_selectedCardIds.Count);
            foreach (CarnivalCard card in _hand)
            {
                if (_selectedCardIds.Contains(card.Id))
                    cards.Add(card);
            }

            return cards;
        }

        private void RemoveSelectedCards()
        {
            for (int i = _hand.Count - 1; i >= 0; i--)
            {
                if (!_selectedCardIds.Contains(_hand[i].Id))
                    continue;

                _discardPile.Add(_hand[i]);
                _hand.RemoveAt(i);
            }

            _selectedCardIds.Clear();
        }

        private void ReplaceCard(CarnivalCard replacement)
        {
            for (int i = 0; i < _hand.Count; i++)
            {
                if (_hand[i].Id == replacement.Id)
                {
                    _hand[i] = replacement;
                    return;
                }
            }
        }
    }
}
