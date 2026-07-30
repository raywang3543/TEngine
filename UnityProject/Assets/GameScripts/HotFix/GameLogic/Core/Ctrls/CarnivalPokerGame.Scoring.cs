using System;
using System.Collections.Generic;

namespace GameLogic.Core
{
    public sealed partial class CarnivalPokerGame
    {
        private CarnivalScoreResult Evaluate(List<CarnivalCard> cards)
        {
            foreach (CarnivalPerformer performer in _performers)
                GetJokerState(performer).Active = false;

            Dictionary<int, List<CarnivalCard>> rankGroups = BuildRankGroups(cards);
            int sequenceSize = HasJoker("four_fingers") ? 4 : 5;
            bool isFlush = HasFlush(cards, sequenceSize);
            bool isStraight = HasStraight(cards, sequenceSize, HasJoker("shortcut"));
            CarnivalHandKind kind = ResolveHandKind(cards.Count, rankGroups, isFlush, isStraight);
            CarnivalScoreResult result = CreateBaseResult(kind);
            _currentEvaluatedHand = kind;
            _roundHandPlayCounts.TryGetValue(kind, out int previousRoundPlays);
            _currentHandWasPlayedThisRound = previousRoundPlays > 0;
            IncrementCount(_roundHandPlayCounts, kind);
            IncrementCount(_handPlayCounts, kind);
            _handsPlayedThisRound++;

            if (!_bossBlindDisabled && CurrentBlind.BossRule == CarnivalBossRule.HalveBaseScore)
            {
                result.Chips = Math.Max(1, result.Chips / 2);
                result.Multiplier = Math.Max(1f, result.Multiplier / 2f);
                result.Breakdown.Add("Boss 盲注：基础筹码与倍率减半");
            }

            AddScoringCards(result, cards, rankGroups);
            if (HasJoker("splash"))
            {
                result.ScoringCardIds.Clear();
                AddCardIds(result, cards);
            }

            bool pareidolia = HasJoker("pareidolia");
            foreach (CarnivalCard card in cards)
            {
                if (!result.ScoringCardIds.Contains(card.Id))
                    continue;

                if (!_bossBlindDisabled &&
                    CurrentBlind.BossRule == CarnivalBossRule.DebuffFaceCards &&
                    IsFaceCard(card, pareidolia))
                {
                    result.Breakdown.Add($"{card.RankText}{card.SuitText} 被 Boss 盲注削弱");
                    continue;
                }

                int triggerCount = GetScoringCardTriggerCount(card, result, pareidolia);
                for (int trigger = 0; trigger < triggerCount; trigger++)
                    ApplyScoringCardTrigger(card, cards, result, pareidolia);
            }

            ApplyHeldCardEffects(cards, result, pareidolia);
            result.Breakdown.Add($"基础 {result.Chips} 筹码 × {result.Multiplier:0.#} 倍率");

            foreach (CarnivalPerformer performer in _performers)
                ApplyPerformer(performer, cards, result);

            result.Score = Math.Max(1, (int)Math.Round(result.Chips * result.Multiplier));
            result.Breakdown.Add($"最终得分 {result.Score:N0}");
            return result;
        }

        private void ApplyPerformer(
            CarnivalPerformer performer,
            List<CarnivalCard> playedCards,
            CarnivalScoreResult result)
        {
            ApplyDataDrivenPerformer(performer, playedCards, result);
        }

        private void ApplyDataDrivenPerformer(
            CarnivalPerformer performer,
            List<CarnivalCard> playedCards,
            CarnivalScoreResult result)
        {
            int matchingCount;
            switch (performer.Effect)
            {
                case CarnivalPerformerEffect.BalatroOriginal:
                    ApplyBalatroIndependentJoker(performer, playedCards, result);
                    return;
                case CarnivalPerformerEffect.FlatChips:
                    result.Chips += (int)performer.EffectValue;
                    break;
                case CarnivalPerformerEffect.FlatMultiplier:
                    result.Multiplier += performer.EffectValue;
                    break;
                case CarnivalPerformerEffect.MultiplyMultiplier:
                    result.Multiplier *= performer.EffectValue;
                    break;
                case CarnivalPerformerEffect.HandChips:
                    if (performer.HandKind == result.Kind)
                        result.Chips += (int)performer.EffectValue;
                    else
                        return;
                    break;
                case CarnivalPerformerEffect.HandMultiplier:
                    if (performer.HandKind == result.Kind)
                        result.Multiplier += performer.EffectValue;
                    else
                        return;
                    break;
                case CarnivalPerformerEffect.SuitChipsPerCard:
                    matchingCount = CountScoringCards(
                        playedCards,
                        result,
                        card => card.Suit == performer.Suit);
                    if (matchingCount == 0)
                        return;
                    result.Chips += (int)(matchingCount * performer.EffectValue);
                    break;
                case CarnivalPerformerEffect.SuitMultiplierPerCard:
                    matchingCount = CountScoringCards(
                        playedCards,
                        result,
                        card => card.Suit == performer.Suit);
                    if (matchingCount == 0)
                        return;
                    result.Multiplier += matchingCount * performer.EffectValue;
                    break;
                case CarnivalPerformerEffect.OddRankChipsPerCard:
                    matchingCount = CountScoringCards(
                        playedCards,
                        result,
                        card => card.Rank == 14 || card.Rank % 2 == 1);
                    if (matchingCount == 0)
                        return;
                    result.Chips += (int)(matchingCount * performer.EffectValue);
                    break;
                case CarnivalPerformerEffect.FaceMultiplierPerCard:
                    matchingCount = CountScoringCards(playedCards, result, card => card.IsFace);
                    if (matchingCount == 0)
                        return;
                    result.Multiplier += matchingCount * performer.EffectValue;
                    break;
                case CarnivalPerformerEffect.MoneyPerScoringCard:
                    matchingCount = result.ScoringCardIds.Count;
                    if (matchingCount == 0)
                        return;
                    Money += (int)(matchingCount * performer.EffectValue);
                    break;
                case CarnivalPerformerEffect.MaxPlayedCardsChips:
                    if (playedCards.Count > performer.ConditionValue)
                        return;
                    result.Chips += (int)performer.EffectValue;
                    break;
                case CarnivalPerformerEffect.PairFamilyMultiplyMultiplier:
                    if (!ContainsPair(result.Kind))
                        return;
                    result.Multiplier *= performer.EffectValue;
                    break;
                case CarnivalPerformerEffect.StraightPermanentChips:
                    if (result.Kind == CarnivalHandKind.Straight ||
                        result.Kind == CarnivalHandKind.StraightFlush)
                    {
                        _runnerBonus += (int)performer.EffectValue;
                    }

                    if (_runnerBonus == 0)
                        return;
                    result.Chips += _runnerBonus;
                    break;
                case CarnivalPerformerEffect.SuitMoneyPerCard:
                    matchingCount = CountScoringCards(
                        playedCards,
                        result,
                        card => card.Suit == performer.Suit);
                    if (matchingCount == 0)
                        return;
                    Money += (int)(matchingCount * performer.EffectValue);
                    break;
                case CarnivalPerformerEffect.LastHandMultiplyMultiplier:
                    if (HandsRemaining != 0)
                        return;
                    result.Multiplier *= performer.EffectValue;
                    break;
                case CarnivalPerformerEffect.PerformerCountMultiplier:
                    result.Multiplier += _performers.Count * performer.EffectValue;
                    break;
                default:
                    return;
            }

            result.Breakdown.Add($"{performer.Name}：{performer.Description}");
        }

        private static bool ContainsPair(CarnivalHandKind kind)
        {
            return kind == CarnivalHandKind.Pair ||
                   kind == CarnivalHandKind.TwoPair ||
                   kind == CarnivalHandKind.ThreeOfAKind ||
                   kind == CarnivalHandKind.FullHouse ||
                   kind == CarnivalHandKind.FourOfAKind;
        }

        private static Dictionary<int, List<CarnivalCard>> BuildRankGroups(List<CarnivalCard> cards)
        {
            var groups = new Dictionary<int, List<CarnivalCard>>();
            foreach (CarnivalCard card in cards)
            {
                if (!groups.TryGetValue(card.Rank, out List<CarnivalCard> group))
                {
                    group = new List<CarnivalCard>();
                    groups.Add(card.Rank, group);
                }

                group.Add(card);
            }

            return groups;
        }

        private static CarnivalHandKind ResolveHandKind(
            int cardCount,
            Dictionary<int, List<CarnivalCard>> rankGroups,
            bool isFlush,
            bool isStraight)
        {
            bool hasFour = false;
            bool hasThree = false;
            bool hasFive = false;
            int pairCount = 0;
            foreach (List<CarnivalCard> group in rankGroups.Values)
            {
                if (group.Count == 5)
                    hasFive = true;
                else if (group.Count == 4)
                    hasFour = true;
                else if (group.Count == 3)
                    hasThree = true;
                else if (group.Count == 2)
                    pairCount++;
            }

            if (isFlush && hasFive)
                return CarnivalHandKind.FlushFive;
            if (isFlush && hasThree && pairCount == 1)
                return CarnivalHandKind.FlushHouse;
            if (isFlush && isStraight)
                return CarnivalHandKind.StraightFlush;
            if (hasFive)
                return CarnivalHandKind.FiveOfAKind;
            if (hasFour)
                return CarnivalHandKind.FourOfAKind;
            if (cardCount == 5 && hasThree && pairCount == 1)
                return CarnivalHandKind.FullHouse;
            if (isFlush)
                return CarnivalHandKind.Flush;
            if (isStraight)
                return CarnivalHandKind.Straight;
            if (hasThree)
                return CarnivalHandKind.ThreeOfAKind;
            if (pairCount >= 2)
                return CarnivalHandKind.TwoPair;
            if (pairCount == 1)
                return CarnivalHandKind.Pair;
            return CarnivalHandKind.HighCard;
        }

        private CarnivalScoreResult CreateBaseResult(CarnivalHandKind kind)
        {
            CarnivalHandLevel handLevel = _handLevels[kind];
            var result = new CarnivalScoreResult
            {
                Kind = kind,
                Chips = handLevel.Chips,
                Multiplier = handLevel.Multiplier,
            };
            switch (kind)
            {
                case CarnivalHandKind.Pair:
                    result.HandName = "对子";
                    break;
                case CarnivalHandKind.TwoPair:
                    result.HandName = "两对";
                    break;
                case CarnivalHandKind.ThreeOfAKind:
                    result.HandName = "三条";
                    break;
                case CarnivalHandKind.Straight:
                    result.HandName = "顺子";
                    break;
                case CarnivalHandKind.Flush:
                    result.HandName = "同花";
                    break;
                case CarnivalHandKind.FullHouse:
                    result.HandName = "葫芦";
                    break;
                case CarnivalHandKind.FourOfAKind:
                    result.HandName = "四条";
                    break;
                case CarnivalHandKind.StraightFlush:
                    result.HandName = "同花顺";
                    break;
                case CarnivalHandKind.FiveOfAKind:
                    result.HandName = "五条";
                    break;
                case CarnivalHandKind.FlushHouse:
                    result.HandName = "同花葫芦";
                    break;
                case CarnivalHandKind.FlushFive:
                    result.HandName = "同花五条";
                    break;
                default:
                    result.HandName = "高牌";
                    break;
            }

            result.Breakdown.Add($"{result.HandName} Lv.{handLevel.Level}");
            return result;
        }

        private static void AddScoringCards(
            CarnivalScoreResult result,
            List<CarnivalCard> cards,
            Dictionary<int, List<CarnivalCard>> rankGroups)
        {
            if (result.Kind == CarnivalHandKind.Straight ||
                result.Kind == CarnivalHandKind.Flush ||
                result.Kind == CarnivalHandKind.FullHouse ||
                result.Kind == CarnivalHandKind.StraightFlush ||
                result.Kind == CarnivalHandKind.FlushHouse ||
                result.Kind == CarnivalHandKind.FlushFive)
            {
                AddCardIds(result, cards);
                return;
            }

            int requiredCount = 1;
            if (result.Kind == CarnivalHandKind.Pair || result.Kind == CarnivalHandKind.TwoPair)
                requiredCount = 2;
            else if (result.Kind == CarnivalHandKind.ThreeOfAKind)
                requiredCount = 3;
            else if (result.Kind == CarnivalHandKind.FourOfAKind)
                requiredCount = 4;
            else if (result.Kind == CarnivalHandKind.FiveOfAKind)
                requiredCount = 5;

            if (result.Kind == CarnivalHandKind.HighCard)
            {
                CarnivalCard highest = cards[0];
                foreach (CarnivalCard card in cards)
                {
                    if (card.Rank > highest.Rank)
                        highest = card;
                }

                result.ScoringCardIds.Add(highest.Id);
                return;
            }

            foreach (List<CarnivalCard> group in rankGroups.Values)
            {
                if (group.Count == requiredCount)
                    AddCardIds(result, group);
            }
        }

        private static void AddCardIds(CarnivalScoreResult result, List<CarnivalCard> cards)
        {
            foreach (CarnivalCard card in cards)
                result.ScoringCardIds.Add(card.Id);
        }

        private static bool AllSameSuit(List<CarnivalCard> cards)
        {
            CarnivalSuit? suit = null;
            foreach (CarnivalCard card in cards)
            {
                if (card.Enhancement != CarnivalCardEnhancement.Wild)
                {
                    suit = card.Suit;
                    break;
                }
            }

            if (!suit.HasValue)
                return true;

            foreach (CarnivalCard card in cards)
            {
                if (card.Enhancement != CarnivalCardEnhancement.Wild && card.Suit != suit.Value)
                    return false;
            }

            return true;
        }

        private static bool IsStraight(List<CarnivalCard> cards)
        {
            var ranks = new List<int>(5);
            foreach (CarnivalCard card in cards)
            {
                if (!ranks.Contains(card.Rank))
                    ranks.Add(card.Rank);
            }

            if (ranks.Count != 5)
                return false;

            ranks.Sort();
            bool normalStraight = true;
            for (int i = 1; i < ranks.Count; i++)
            {
                if (ranks[i] != ranks[0] + i)
                {
                    normalStraight = false;
                    break;
                }
            }

            if (normalStraight)
                return true;

            return ranks[0] == 2 &&
                   ranks[1] == 3 &&
                   ranks[2] == 4 &&
                   ranks[3] == 5 &&
                   ranks[4] == 14;
        }

        private static int CountScoringCards(
            List<CarnivalCard> cards,
            CarnivalScoreResult result,
            Predicate<CarnivalCard> predicate)
        {
            int count = 0;
            foreach (CarnivalCard card in cards)
            {
                if (result.ScoringCardIds.Contains(card.Id) && predicate(card))
                    count++;
            }

            return count;
        }

        private void ApplyCardEnhancement(CarnivalCard card, CarnivalScoreResult result)
        {
            if (card.Enhancement == CarnivalCardEnhancement.None)
                return;

            CarnivalCardEnhancementContent content = _contentModel.FindEnhancement(card.Enhancement);
            result.Chips += content.Chips;
            result.Multiplier += content.AdditiveMultiplier;
            result.Multiplier *= content.MultiplierFactor;
            result.Breakdown.Add($"{card.RankText}{card.SuitText} {content.Name}：{content.Description}");
        }

        private void ResolveBreakingCards(List<CarnivalCard> playedCards, CarnivalScoreResult result)
        {
            foreach (CarnivalCard card in playedCards)
            {
                if (card.Enhancement == CarnivalCardEnhancement.None)
                    continue;

                CarnivalCardEnhancementContent content = _contentModel.FindEnhancement(card.Enhancement);
                if (content.BreakChance <= 0f || _random.NextDouble() >= content.BreakChance)
                    continue;

                _selectedCardIds.Remove(card.Id);
                _hand.RemoveAll(item => item.Id == card.Id);
                result.Breakdown.Add($"{card.RankText}{card.SuitText} {content.Name}碎裂");
                CarnivalPerformer glassJoker = FindOwnedJoker("glass");
                if (glassJoker != null)
                    GetJokerState(glassJoker).Value += 0.75f;
            }
        }
    }
}
