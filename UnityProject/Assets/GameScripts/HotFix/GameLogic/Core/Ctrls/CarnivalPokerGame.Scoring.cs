using System;
using System.Collections.Generic;

namespace GameLogic.Core
{
    public sealed partial class CarnivalPokerGame
    {
        private CarnivalScoreResult Evaluate(List<CarnivalCard> cards)
        {
            Dictionary<int, List<CarnivalCard>> rankGroups = BuildRankGroups(cards);
            bool isFlush = cards.Count == 5 && AllSameSuit(cards);
            bool isStraight = cards.Count == 5 && IsStraight(cards);
            CarnivalHandKind kind = ResolveHandKind(cards.Count, rankGroups, isFlush, isStraight);
            CarnivalScoreResult result = CreateBaseResult(kind);

            if (CurrentBlind.BossRule == CarnivalBossRule.HalveBaseScore)
            {
                result.Chips = Math.Max(1, result.Chips / 2);
                result.Multiplier = Math.Max(1f, result.Multiplier / 2f);
                result.Breakdown.Add("Boss 盲注：基础筹码与倍率减半");
            }

            AddScoringCards(result, cards, rankGroups);
            foreach (CarnivalCard card in cards)
            {
                if (!result.ScoringCardIds.Contains(card.Id))
                    continue;

                if (CurrentBlind.BossRule == CarnivalBossRule.DebuffFaceCards && card.IsFace)
                {
                    result.Breakdown.Add($"{card.RankText}{card.SuitText} 被 Boss 盲注削弱");
                    continue;
                }

                result.Chips += card.ChipValue;
                ApplyCardEnhancement(card, result);
            }

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
            if (performer.Effect != CarnivalPerformerEffect.Custom)
            {
                ApplyDataDrivenPerformer(performer, playedCards, result);
                return;
            }

            int matchingCount;
            switch (performer.Id)
            {
                case "red-ribbons":
                    matchingCount = CountScoringCards(playedCards, result, card => card.Suit == CarnivalSuit.Hearts);
                    if (matchingCount > 0)
                    {
                        result.Multiplier += matchingCount * 3;
                        result.Breakdown.Add($"红绸舞者 +{matchingCount * 3} 倍率");
                    }
                    break;
                case "pocket-confetti":
                    if (playedCards.Count <= 3)
                    {
                        result.Chips += 24;
                        result.Breakdown.Add("口袋彩屑 +24 筹码");
                    }
                    break;
                case "club-lantern":
                    matchingCount = CountScoringCards(playedCards, result, card => card.Suit == CarnivalSuit.Clubs);
                    if (matchingCount > 0)
                    {
                        result.Multiplier += matchingCount * 5;
                        result.Breakdown.Add($"梅花提灯 +{matchingCount * 5} 倍率");
                    }
                    break;
                case "mirror-duet":
                    if (ContainsPair(result.Kind))
                    {
                        result.Multiplier *= 2f;
                        result.Breakdown.Add("镜面二重奏 ×2 倍率");
                    }
                    break;
                case "street-runner":
                    if (result.Kind == CarnivalHandKind.Straight ||
                        result.Kind == CarnivalHandKind.StraightFlush)
                    {
                        _runnerBonus += 12;
                    }

                    if (_runnerBonus > 0)
                    {
                        result.Chips += _runnerBonus;
                        result.Breakdown.Add($"高跷跑者 +{_runnerBonus} 筹码");
                    }
                    break;
                case "diamond-register":
                    matchingCount = CountScoringCards(
                        playedCards,
                        result,
                        card => card.Suit == CarnivalSuit.Diamonds);
                    if (matchingCount > 0)
                    {
                        Money += matchingCount;
                        result.Breakdown.Add($"钻石收银机 +${matchingCount}");
                    }
                    break;
                case "late-finale":
                    if (HandsRemaining == 0)
                    {
                        result.Multiplier *= 2.5f;
                        result.Breakdown.Add("压轴面具 ×2.5 倍率");
                    }
                    break;
                case "full-tent":
                    int bonus = _performers.Count * 4;
                    result.Multiplier += bonus;
                    result.Breakdown.Add($"满座帐篷 +{bonus} 倍率");
                    break;
                case "odd-acrobat":
                    matchingCount = CountScoringCards(
                        playedCards,
                        result,
                        card => card.Rank == 14 || card.Rank % 2 == 1);
                    if (matchingCount > 0)
                    {
                        result.Chips += matchingCount * 18;
                        result.Breakdown.Add($"奇数杂技团 +{matchingCount * 18} 筹码");
                    }
                    break;
            }
        }

        private void ApplyDataDrivenPerformer(
            CarnivalPerformer performer,
            List<CarnivalCard> playedCards,
            CarnivalScoreResult result)
        {
            int matchingCount;
            switch (performer.Effect)
            {
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

        private static void ApplyCardEnhancement(CarnivalCard card, CarnivalScoreResult result)
        {
            switch (card.Enhancement)
            {
                case CarnivalCardEnhancement.Bonus:
                    result.Chips += 30;
                    result.Breakdown.Add($"{card.RankText}{card.SuitText} 奖励牌 +30 筹码");
                    break;
                case CarnivalCardEnhancement.Mult:
                    result.Multiplier += 4f;
                    result.Breakdown.Add($"{card.RankText}{card.SuitText} 倍率牌 +4 倍率");
                    break;
                case CarnivalCardEnhancement.Glass:
                    result.Multiplier *= 2f;
                    result.Breakdown.Add($"{card.RankText}{card.SuitText} 玻璃牌 ×2 倍率");
                    break;
                case CarnivalCardEnhancement.Lucky:
                    result.Multiplier += 4f;
                    result.Breakdown.Add($"{card.RankText}{card.SuitText} 幸运牌 +4 倍率");
                    break;
                case CarnivalCardEnhancement.Steel:
                    result.Multiplier *= 1.5f;
                    result.Breakdown.Add($"{card.RankText}{card.SuitText} 钢铁牌 ×1.5 倍率");
                    break;
                case CarnivalCardEnhancement.Gold:
                    result.Chips += 20;
                    result.Breakdown.Add($"{card.RankText}{card.SuitText} 黄金牌 +20 筹码");
                    break;
                case CarnivalCardEnhancement.Stone:
                    result.Chips += 50;
                    result.Breakdown.Add($"{card.RankText}{card.SuitText} 石牌 +50 筹码");
                    break;
            }
        }

        private void ResolveGlassCards(List<CarnivalCard> playedCards, CarnivalScoreResult result)
        {
            foreach (CarnivalCard card in playedCards)
            {
                if (card.Enhancement != CarnivalCardEnhancement.Glass || _random.NextDouble() >= 0.25)
                    continue;

                _selectedCardIds.Remove(card.Id);
                _hand.RemoveAll(item => item.Id == card.Id);
                result.Breakdown.Add($"{card.RankText}{card.SuitText} 玻璃牌碎裂");
            }
        }
    }
}
