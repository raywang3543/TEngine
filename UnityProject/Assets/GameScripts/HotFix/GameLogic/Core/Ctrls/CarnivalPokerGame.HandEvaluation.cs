using System;
using System.Collections.Generic;

namespace GameLogic.Core
{
    public sealed partial class CarnivalPokerGame
    {
        private CarnivalHandEvaluation EvaluateHand(List<CarnivalCard> playedCards)
        {
            var eligibleCards = new List<CarnivalCard>();
            var stoneCardIds = new List<int>();
            foreach (CarnivalCard card in playedCards)
            {
                if (card.Enhancement == CarnivalCardEnhancement.Stone)
                    stoneCardIds.Add(card.Id);
                else
                    eligibleCards.Add(card);
            }

            Dictionary<int, List<CarnivalCard>> groups = BuildRankGroups(eligibleCards);
            int minimumSequenceSize = HasJoker("four_fingers") ? 4 : 5;
            List<int> scoringIds;

            if (TryFindFlushFive(eligibleCards, groups, out scoringIds))
                return WithStoneCards(CarnivalHandKind.FlushFive, scoringIds, stoneCardIds);
            if (TryFindFlushHouse(eligibleCards, groups, out scoringIds))
                return WithStoneCards(CarnivalHandKind.FlushHouse, scoringIds, stoneCardIds);
            if (TryFindStraightFlush(
                    eligibleCards,
                    minimumSequenceSize,
                    HasJoker("shortcut"),
                    out scoringIds))
            {
                return WithStoneCards(CarnivalHandKind.StraightFlush, scoringIds, stoneCardIds);
            }

            if (TryFindRankGroup(groups, 5, out scoringIds))
                return WithStoneCards(CarnivalHandKind.FiveOfAKind, scoringIds, stoneCardIds);
            if (TryFindRankGroup(groups, 4, out scoringIds))
                return WithStoneCards(CarnivalHandKind.FourOfAKind, scoringIds, stoneCardIds);
            if (TryFindFullHouse(groups, out scoringIds))
                return WithStoneCards(CarnivalHandKind.FullHouse, scoringIds, stoneCardIds);
            if (TryFindFlush(eligibleCards, minimumSequenceSize, out scoringIds))
                return WithStoneCards(CarnivalHandKind.Flush, scoringIds, stoneCardIds);
            if (TryFindStraight(
                    eligibleCards,
                    minimumSequenceSize,
                    HasJoker("shortcut"),
                    out scoringIds))
            {
                return WithStoneCards(CarnivalHandKind.Straight, scoringIds, stoneCardIds);
            }

            if (TryFindRankGroup(groups, 3, out scoringIds))
                return WithStoneCards(CarnivalHandKind.ThreeOfAKind, scoringIds, stoneCardIds);
            if (TryFindTwoPair(groups, out scoringIds))
                return WithStoneCards(CarnivalHandKind.TwoPair, scoringIds, stoneCardIds);
            if (TryFindRankGroup(groups, 2, out scoringIds))
                return WithStoneCards(CarnivalHandKind.Pair, scoringIds, stoneCardIds);

            scoringIds = new List<int>();
            CarnivalCard? highest = null;
            foreach (CarnivalCard card in eligibleCards)
            {
                if (!highest.HasValue || card.Rank > highest.Value.Rank)
                    highest = card;
            }

            if (highest.HasValue)
                scoringIds.Add(highest.Value.Id);
            return WithStoneCards(CarnivalHandKind.HighCard, scoringIds, stoneCardIds);
        }

        private static CarnivalHandEvaluation WithStoneCards(
            CarnivalHandKind kind,
            List<int> scoringIds,
            List<int> stoneCardIds)
        {
            foreach (int cardId in stoneCardIds)
            {
                if (!scoringIds.Contains(cardId))
                    scoringIds.Add(cardId);
            }

            return new CarnivalHandEvaluation(kind, scoringIds.AsReadOnly());
        }

        private bool TryFindFlushFive(
            List<CarnivalCard> cards,
            Dictionary<int, List<CarnivalCard>> groups,
            out List<int> scoringIds)
        {
            if (TryFindRankGroup(groups, 5, out scoringIds) &&
                CardsShareSuit(cards))
            {
                return true;
            }

            scoringIds = null;
            return false;
        }

        private bool TryFindFlushHouse(
            List<CarnivalCard> cards,
            Dictionary<int, List<CarnivalCard>> groups,
            out List<int> scoringIds)
        {
            if (TryFindFullHouse(groups, out scoringIds) && CardsShareSuit(cards))
                return true;

            scoringIds = null;
            return false;
        }

        private bool CardsShareSuit(List<CarnivalCard> cards)
        {
            if (cards.Count != 5)
                return false;

            foreach (CarnivalSuit suit in Enum.GetValues(typeof(CarnivalSuit)))
            {
                bool matches = true;
                foreach (CarnivalCard card in cards)
                {
                    if (!IsSuit(card, suit))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                    return true;
            }

            return false;
        }

        private bool TryFindStraightFlush(
            List<CarnivalCard> cards,
            int minimumSize,
            bool allowRankGap,
            out List<int> scoringIds)
        {
            foreach (CarnivalSuit suit in Enum.GetValues(typeof(CarnivalSuit)))
            {
                var suitedCards = new List<CarnivalCard>();
                foreach (CarnivalCard card in cards)
                {
                    if (IsSuit(card, suit))
                        suitedCards.Add(card);
                }

                if (TryFindStraight(suitedCards, minimumSize, allowRankGap, out scoringIds))
                    return true;
            }

            scoringIds = null;
            return false;
        }

        private bool TryFindFlush(
            List<CarnivalCard> cards,
            int minimumSize,
            out List<int> scoringIds)
        {
            foreach (CarnivalSuit suit in Enum.GetValues(typeof(CarnivalSuit)))
            {
                var matches = new List<CarnivalCard>();
                foreach (CarnivalCard card in cards)
                {
                    if (IsSuit(card, suit))
                        matches.Add(card);
                }

                if (matches.Count < minimumSize)
                    continue;

                matches.Sort((left, right) => right.Rank.CompareTo(left.Rank));
                int scoreCount = matches.Count >= 5 ? 5 : minimumSize;
                scoringIds = new List<int>(scoreCount);
                for (int index = 0; index < scoreCount; index++)
                    scoringIds.Add(matches[index].Id);
                return true;
            }

            scoringIds = null;
            return false;
        }

        private static bool TryFindStraight(
            List<CarnivalCard> cards,
            int minimumSize,
            bool allowRankGap,
            out List<int> scoringIds)
        {
            int preferredSize = minimumSize == 4 ? 5 : minimumSize;
            if (preferredSize > minimumSize &&
                TryFindStraightOfSize(cards, preferredSize, allowRankGap, out scoringIds))
            {
                return true;
            }

            return TryFindStraightOfSize(cards, minimumSize, allowRankGap, out scoringIds);
        }

        private static bool TryFindStraightOfSize(
            List<CarnivalCard> cards,
            int requiredSize,
            bool allowRankGap,
            out List<int> scoringIds)
        {
            var byRank = new Dictionary<int, CarnivalCard>();
            foreach (CarnivalCard card in cards)
            {
                if (!byRank.ContainsKey(card.Rank))
                    byRank.Add(card.Rank, card);
                if (card.Rank == 14 && !byRank.ContainsKey(1))
                    byRank.Add(1, card);
            }

            var ranks = new List<int>(byRank.Keys);
            ranks.Sort();
            int maximumStep = allowRankGap ? 2 : 1;
            for (int start = ranks.Count - 1; start >= 0; start--)
            {
                var selectedRanks = new List<int> { ranks[start] };
                int previous = ranks[start];
                for (int index = start - 1; index >= 0; index--)
                {
                    int difference = previous - ranks[index];
                    if (difference <= 0)
                        continue;
                    if (difference > maximumStep)
                        break;

                    selectedRanks.Add(ranks[index]);
                    previous = ranks[index];
                    if (selectedRanks.Count == requiredSize)
                    {
                        scoringIds = new List<int>(requiredSize);
                        foreach (int rank in selectedRanks)
                        {
                            int cardId = byRank[rank].Id;
                            if (!scoringIds.Contains(cardId))
                                scoringIds.Add(cardId);
                        }

                        if (scoringIds.Count == requiredSize)
                            return true;
                        break;
                    }
                }
            }

            scoringIds = null;
            return false;
        }

        private static bool TryFindRankGroup(
            Dictionary<int, List<CarnivalCard>> groups,
            int requiredCount,
            out List<int> scoringIds)
        {
            int bestRank = int.MinValue;
            List<CarnivalCard> bestGroup = null;
            foreach (KeyValuePair<int, List<CarnivalCard>> pair in groups)
            {
                if (pair.Value.Count >= requiredCount && pair.Key > bestRank)
                {
                    bestRank = pair.Key;
                    bestGroup = pair.Value;
                }
            }

            if (bestGroup == null)
            {
                scoringIds = null;
                return false;
            }

            scoringIds = new List<int>(requiredCount);
            for (int index = 0; index < requiredCount; index++)
                scoringIds.Add(bestGroup[index].Id);
            return true;
        }

        private static bool TryFindFullHouse(
            Dictionary<int, List<CarnivalCard>> groups,
            out List<int> scoringIds)
        {
            List<CarnivalCard> three = null;
            List<CarnivalCard> pair = null;
            int threeRank = int.MinValue;
            int pairRank = int.MinValue;
            foreach (KeyValuePair<int, List<CarnivalCard>> entry in groups)
            {
                if (entry.Value.Count >= 3 && entry.Key > threeRank)
                {
                    threeRank = entry.Key;
                    three = entry.Value;
                }
            }

            foreach (KeyValuePair<int, List<CarnivalCard>> entry in groups)
            {
                if (entry.Key != threeRank && entry.Value.Count >= 2 && entry.Key > pairRank)
                {
                    pairRank = entry.Key;
                    pair = entry.Value;
                }
            }

            if (three == null || pair == null)
            {
                scoringIds = null;
                return false;
            }

            scoringIds = new List<int>(5);
            for (int index = 0; index < 3; index++)
                scoringIds.Add(three[index].Id);
            for (int index = 0; index < 2; index++)
                scoringIds.Add(pair[index].Id);
            return true;
        }

        private static bool TryFindTwoPair(
            Dictionary<int, List<CarnivalCard>> groups,
            out List<int> scoringIds)
        {
            var pairRanks = new List<int>();
            foreach (KeyValuePair<int, List<CarnivalCard>> entry in groups)
            {
                if (entry.Value.Count >= 2)
                    pairRanks.Add(entry.Key);
            }

            if (pairRanks.Count < 2)
            {
                scoringIds = null;
                return false;
            }

            pairRanks.Sort();
            scoringIds = new List<int>(4);
            for (int rankIndex = pairRanks.Count - 1; rankIndex >= pairRanks.Count - 2; rankIndex--)
            {
                List<CarnivalCard> pair = groups[pairRanks[rankIndex]];
                scoringIds.Add(pair[0].Id);
                scoringIds.Add(pair[1].Id);
            }

            return true;
        }
    }
}
