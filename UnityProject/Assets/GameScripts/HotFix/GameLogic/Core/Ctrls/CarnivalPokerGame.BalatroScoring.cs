using System;
using System.Collections.Generic;

namespace GameLogic.Core
{
    public sealed partial class CarnivalPokerGame
    {
        private bool HasFlush(List<CarnivalCard> cards, int requiredCount)
        {
            foreach (CarnivalSuit suit in Enum.GetValues(typeof(CarnivalSuit)))
            {
                int count = 0;
                foreach (CarnivalCard card in cards)
                {
                    if (IsSuit(card, suit))
                        count++;
                }

                if (count >= requiredCount)
                    return true;
            }

            return false;
        }

        private static bool HasStraight(
            List<CarnivalCard> cards,
            int requiredCount,
            bool allowRankGap)
        {
            var ranks = new List<int>();
            foreach (CarnivalCard card in cards)
            {
                if (!ranks.Contains(card.Rank))
                    ranks.Add(card.Rank);
            }

            if (ranks.Contains(14))
                ranks.Add(1);
            ranks.Sort();

            int maximumStep = allowRankGap ? 2 : 1;
            for (int start = 0; start < ranks.Count; start++)
            {
                int length = 1;
                int previous = ranks[start];
                for (int index = start + 1; index < ranks.Count; index++)
                {
                    int difference = ranks[index] - previous;
                    if (difference <= 0)
                        continue;
                    if (difference > maximumStep)
                        break;

                    length++;
                    previous = ranks[index];
                    if (length >= requiredCount)
                        return true;
                }
            }

            return requiredCount <= 1 && ranks.Count > 0;
        }

        private int GetScoringCardTriggerCount(
            CarnivalCard card,
            CarnivalScoreResult result,
            bool pareidolia)
        {
            int triggers = 1;
            if (card.Seal == CarnivalCardSeal.Red)
                triggers++;
            if (HandsRemaining == 0)
                triggers += CountJokerAbilityOccurrences("dusk");
            if (card.Rank >= 2 && card.Rank <= 5)
                triggers += CountJokerAbilityOccurrences("hack");
            if (IsFaceCard(card, pareidolia))
                triggers += CountJokerAbilityOccurrences("sock_and_buskin");

            if (result.ScoringCardIds.Count > 0 &&
                card.Id == result.ScoringCardIds[0] &&
                CountJokerAbilityOccurrences("hanging_chad") > 0)
            {
                triggers += 2 * CountJokerAbilityOccurrences("hanging_chad");
            }

            CarnivalPerformer seltzer = FindOwnedJoker("selzer");
            if (seltzer != null && GetJokerState(seltzer).Counter > 0)
                triggers += CountJokerAbilityOccurrences("selzer");
            return triggers;
        }

        private void ApplyScoringCardTrigger(
            CarnivalCard card,
            List<CarnivalCard> playedCards,
            CarnivalScoreResult result,
            bool pareidolia)
        {
            if (card.Enhancement != CarnivalCardEnhancement.Stone)
                result.Chips += card.ChipValue;

            ApplyPlayingCardEdition(card, result);
            ApplyOriginalCardEnhancement(card, result);
            ApplyOnScoredJokers(card, playedCards, result, pareidolia);
        }

        private static void ApplyPlayingCardEdition(CarnivalCard card, CarnivalScoreResult result)
        {
            switch (card.Edition)
            {
                case CarnivalCardEdition.Foil:
                    result.Chips += 50;
                    break;
                case CarnivalCardEdition.Holographic:
                    result.Multiplier += 10f;
                    break;
                case CarnivalCardEdition.Polychrome:
                    result.Multiplier *= 1.5f;
                    break;
            }
        }

        private void ApplyOriginalCardEnhancement(CarnivalCard card, CarnivalScoreResult result)
        {
            switch (card.Enhancement)
            {
                case CarnivalCardEnhancement.Bonus:
                    result.Chips += 30;
                    break;
                case CarnivalCardEnhancement.Mult:
                    result.Multiplier += 4f;
                    break;
                case CarnivalCardEnhancement.Glass:
                    result.Multiplier *= 2f;
                    break;
                case CarnivalCardEnhancement.Lucky:
                    bool triggered = false;
                    if (RollChance(5))
                    {
                        result.Multiplier += 20f;
                        triggered = true;
                    }

                    if (RollChance(15))
                    {
                        Money += 20;
                        triggered = true;
                    }

                    if (triggered)
                    {
                        CarnivalPerformer luckyCat = FindOwnedJoker("lucky_cat");
                        if (luckyCat != null)
                            GetJokerState(luckyCat).Value += 0.25f;
                    }
                    break;
                case CarnivalCardEnhancement.Stone:
                    result.Chips += 50 + card.PermanentChips;
                    break;
            }
        }

        private void ApplyOnScoredJokers(
            CarnivalCard card,
            List<CarnivalCard> playedCards,
            CarnivalScoreResult result,
            bool pareidolia)
        {
            var snapshot = new List<CarnivalPerformer>(_performers);
            foreach (CarnivalPerformer performer in snapshot)
            {
                ApplyOnScoredJoker(
                    performer,
                    card,
                    playedCards,
                    result,
                    pareidolia,
                    0,
                    new HashSet<CarnivalPerformer>());
            }
        }

        private void ApplyOnScoredJoker(
            CarnivalPerformer performer,
            CarnivalCard card,
            List<CarnivalCard> playedCards,
            CarnivalScoreResult result,
            bool pareidolia,
            int copyDepth,
            HashSet<CarnivalPerformer> copyChain)
        {
            if (performer == null || copyDepth > _performers.Count || !copyChain.Add(performer))
                return;

            if (performer.Id == "blueprint" || performer.Id == "brainstorm")
            {
                CarnivalPerformer target = ResolveCopyTarget(performer);
                if (target != null && target.BlueprintCompatible)
                {
                    ApplyOnScoredJoker(
                        target,
                        card,
                        playedCards,
                        result,
                        pareidolia,
                        copyDepth + 1,
                        copyChain);
                }

                return;
            }

            switch (performer.Id)
            {
                    case "greedy_joker":
                        if (IsSuit(card, CarnivalSuit.Diamonds))
                            result.Multiplier += 3f;
                        break;
                    case "lusty_joker":
                        if (IsSuit(card, CarnivalSuit.Hearts))
                            result.Multiplier += 3f;
                        break;
                    case "wrathful_joker":
                        if (IsSuit(card, CarnivalSuit.Spades))
                            result.Multiplier += 3f;
                        break;
                    case "gluttenous_joker":
                        if (IsSuit(card, CarnivalSuit.Clubs))
                            result.Multiplier += 3f;
                        break;
                    case "8_ball":
                        if (card.Rank == 8 && RollChance(4))
                            TryCreateConsumable(CarnivalConsumableFamily.Tarot);
                        break;
                    case "fibonacci":
                        if (card.Rank == 14 || card.Rank == 2 || card.Rank == 3 ||
                            card.Rank == 5 || card.Rank == 8)
                            result.Multiplier += 8f;
                        break;
                    case "scary_face":
                        if (IsFaceCard(card, pareidolia))
                            result.Chips += 30;
                        break;
                    case "even_steven":
                        if (card.Rank <= 10 && card.Rank % 2 == 0)
                            result.Multiplier += 4f;
                        break;
                    case "odd_todd":
                        if (card.Rank == 14 || (card.Rank <= 10 && card.Rank % 2 == 1))
                            result.Chips += 31;
                        break;
                    case "scholar":
                        if (card.Rank == 14)
                        {
                            result.Chips += 20;
                            result.Multiplier += 4f;
                        }
                        break;
                    case "business":
                        if (IsFaceCard(card, pareidolia) && RollChance(2))
                            Money += 2;
                        break;
                    case "hiker":
                        ReplaceCardEverywhere(card.WithPermanentChips(card.PermanentChips + 5));
                        break;
                    case "midas_mask":
                        if (IsFaceCard(card, pareidolia))
                            ReplaceCardEverywhere(card.WithEnhancement(CarnivalCardEnhancement.Gold));
                        break;
                    case "photograph":
                        CarnivalJokerState photograph = GetJokerState(performer);
                        if (!photograph.Active && IsFaceCard(card, pareidolia))
                        {
                            result.Multiplier *= 2f;
                            photograph.Active = true;
                        }
                        break;
                    case "vampire":
                        if (card.Enhancement != CarnivalCardEnhancement.None)
                        {
                            GetJokerState(performer).Value += 0.1f;
                            ReplaceCardEverywhere(card.WithEnhancement(CarnivalCardEnhancement.None));
                        }
                        break;
                    case "ancient":
                        if (IsSuit(card, GetJokerState(performer).Suit))
                            result.Multiplier *= 1.5f;
                        break;
                    case "walkie_talkie":
                        if (card.Rank == 4 || card.Rank == 10)
                        {
                            result.Chips += 10;
                            result.Multiplier += 4f;
                        }
                        break;
                    case "smiley":
                        if (IsFaceCard(card, pareidolia))
                            result.Multiplier += 5f;
                        break;
                    case "ticket":
                        if (card.Enhancement == CarnivalCardEnhancement.Gold)
                            Money += 4;
                        break;
                    case "rough_gem":
                        if (IsSuit(card, CarnivalSuit.Diamonds))
                            Money++;
                        break;
                    case "bloodstone":
                        if (IsSuit(card, CarnivalSuit.Hearts) && RollChance(2))
                            result.Multiplier *= 1.5f;
                        break;
                    case "arrowhead":
                        if (IsSuit(card, CarnivalSuit.Spades))
                            result.Chips += 50;
                        break;
                    case "onyx_agate":
                        if (IsSuit(card, CarnivalSuit.Clubs))
                            result.Multiplier += 7f;
                        break;
                    case "wee":
                        if (card.Rank == 2)
                            GetJokerState(performer).Value += 8f;
                        break;
                    case "idol":
                        CarnivalJokerState idol = GetJokerState(performer);
                        if (card.Rank == idol.Rank && IsSuit(card, idol.Suit))
                            result.Multiplier *= 2f;
                        break;
                    case "triboulet":
                        if (card.Rank == 12 || card.Rank == 13)
                            result.Multiplier *= 2f;
                        break;
            }
        }

        private void ApplyHeldCardEffects(
            List<CarnivalCard> playedCards,
            CarnivalScoreResult result,
            bool pareidolia)
        {
            var playedIds = new HashSet<int>();
            foreach (CarnivalCard card in playedCards)
                playedIds.Add(card.Id);

            var held = new List<CarnivalCard>();
            foreach (CarnivalCard card in _hand)
            {
                if (!playedIds.Contains(card.Id))
                    held.Add(card);
            }

            int triggers = 1 + CountJokerAbilityOccurrences("mime");
            int baronCount = CountJokerAbilityOccurrences("baron");
            int shootTheMoonCount = CountJokerAbilityOccurrences("shoot_the_moon");
            int reservedParkingCount = CountJokerAbilityOccurrences("reserved_parking");
            for (int repeat = 0; repeat < triggers; repeat++)
            {
                foreach (CarnivalCard card in held)
                {
                    if (card.Enhancement == CarnivalCardEnhancement.Steel)
                        result.Multiplier *= 1.5f;
                    if (card.Rank == 13)
                    {
                        for (int index = 0; index < baronCount; index++)
                            result.Multiplier *= 1.5f;
                    }

                    if (card.Rank == 12)
                        result.Multiplier += 13f * shootTheMoonCount;
                    if (IsFaceCard(card, pareidolia))
                    {
                        for (int index = 0; index < reservedParkingCount; index++)
                        {
                            if (RollChance(2))
                                Money++;
                        }
                    }
                    if (card.Seal == CarnivalCardSeal.Blue && HandsRemaining == 0)
                        TryCreateConsumable(CarnivalConsumableFamily.Planet);
                }
            }

            int raisedFistCount = CountJokerAbilityOccurrences("raised_fist");
            if (held.Count > 0 && raisedFistCount > 0)
            {
                CarnivalCard lowest = held[0];
                foreach (CarnivalCard card in held)
                {
                    if (card.Rank < lowest.Rank)
                        lowest = card;
                }

                result.Multiplier += raisedFistCount *
                                     2f *
                                     (lowest.Rank == 14 ? 11 : Math.Min(lowest.Rank, 10));
            }
        }

        private void ApplyBalatroIndependentJoker(
            CarnivalPerformer performer,
            List<CarnivalCard> playedCards,
            CarnivalScoreResult result)
        {
            ApplyBalatroIndependentJoker(performer, playedCards, result, 0, true);
        }

        private void ApplyBalatroIndependentJoker(
            CarnivalPerformer performer,
            List<CarnivalCard> playedCards,
            CarnivalScoreResult result,
            int copyDepth,
            bool applyEdition)
        {
            if (performer == null || copyDepth > _performers.Count)
                return;

            CarnivalJokerState state = GetJokerState(performer);
            bool applied = true;
            switch (performer.Id)
            {
                case "blueprint":
                    ApplyCopiedJoker(performer, 1, playedCards, result, copyDepth);
                    if (applyEdition)
                        ApplyJokerEdition(state, result);
                    return;
                case "brainstorm":
                    ApplyCopiedJoker(performer, -GetPerformerIndex(performer), playedCards, result, copyDepth);
                    if (applyEdition)
                        ApplyJokerEdition(state, result);
                    return;
                case "joker":
                    result.Multiplier += 4f;
                    break;
                case "jolly":
                    applied = ContainsPair(result.Kind);
                    if (applied) result.Multiplier += 8f;
                    break;
                case "zany":
                    applied = ContainsThree(result.Kind);
                    if (applied) result.Multiplier += 12f;
                    break;
                case "mad":
                    applied = ContainsTwoPair(result.Kind);
                    if (applied) result.Multiplier += 10f;
                    break;
                case "crazy":
                    applied = ContainsStraight(result.Kind);
                    if (applied) result.Multiplier += 12f;
                    break;
                case "droll":
                    applied = ContainsFlush(result.Kind);
                    if (applied) result.Multiplier += 10f;
                    break;
                case "sly":
                    applied = ContainsPair(result.Kind);
                    if (applied) result.Chips += 50;
                    break;
                case "wily":
                    applied = ContainsThree(result.Kind);
                    if (applied) result.Chips += 100;
                    break;
                case "clever":
                    applied = ContainsTwoPair(result.Kind);
                    if (applied) result.Chips += 80;
                    break;
                case "devious":
                    applied = ContainsStraight(result.Kind);
                    if (applied) result.Chips += 100;
                    break;
                case "crafty":
                    applied = ContainsFlush(result.Kind);
                    if (applied) result.Chips += 80;
                    break;
                case "half":
                    applied = playedCards.Count <= 3;
                    if (applied) result.Multiplier += 20f;
                    break;
                case "stencil":
                    result.Multiplier *= Math.Max(1, MaxPerformerSlots - _performers.Count + 1);
                    break;
                case "ceremonial":
                case "ride_the_bus":
                case "green_joker":
                case "red_card":
                case "flash":
                case "trousers":
                    result.Multiplier += state.Value;
                    break;
                case "banner":
                    result.Chips += 30 * DiscardsRemaining;
                    break;
                case "mystic_summit":
                    applied = DiscardsRemaining == 0;
                    if (applied) result.Multiplier += 15f;
                    break;
                case "loyalty_card":
                    applied = state.Counter == 0;
                    if (applied) result.Multiplier *= 4f;
                    break;
                case "misprint":
                    result.Multiplier += _random.Next(0, 24);
                    break;
                case "steel_joker":
                    result.Multiplier *= 1f + 0.2f * CountCards(card =>
                        card.Enhancement == CarnivalCardEnhancement.Steel);
                    break;
                case "abstract":
                    result.Multiplier += 3f * _performers.Count;
                    break;
                case "gros_michel":
                    result.Multiplier += 15f;
                    break;
                case "supernova":
                    _handPlayCounts.TryGetValue(result.Kind, out int handCount);
                    result.Multiplier += handCount;
                    break;
                case "blackboard":
                    applied = AllHeldCardsAreBlack(playedCards);
                    if (applied) result.Multiplier *= 3f;
                    break;
                case "runner":
                case "square":
                case "castle":
                case "wee":
                    result.Chips += (int)state.Value;
                    break;
                case "ice_cream":
                    result.Chips += Math.Max(0, (int)state.Value);
                    break;
                case "blue_joker":
                    result.Chips += 2 * _deck.Count;
                    break;
                case "constellation":
                case "cavendish":
                case "madness":
                case "vampire":
                case "hologram":
                case "lucky_cat":
                case "campfire":
                case "glass":
                case "hit_the_road":
                case "caino":
                case "yorick":
                    result.Multiplier *= Math.Max(1f, state.Value);
                    break;
                case "todo_list":
                    applied = (int)result.Kind == state.Rank;
                    if (applied) Money += 4;
                    break;
                case "card_sharp":
                    applied = _currentHandWasPlayedThisRound;
                    if (applied) result.Multiplier *= 3f;
                    break;
                case "stone":
                    result.Chips += 25 * CountCards(card =>
                        card.Enhancement == CarnivalCardEnhancement.Stone);
                    break;
                case "erosion":
                    result.Multiplier += 4f * Math.Max(0, _startingDeckSize - CountCards(card => true));
                    break;
                case "fortune_teller":
                    result.Multiplier += _tarotCardsUsedThisRun;
                    break;
                case "bull":
                    result.Chips += 2 * Math.Max(0, Money);
                    break;
                case "popcorn":
                    result.Multiplier += Math.Max(0, state.Value);
                    break;
                case "ramen":
                    result.Multiplier *= Math.Max(1f, state.Value);
                    break;
                case "acrobat":
                    applied = HandsRemaining == 0;
                    if (applied) result.Multiplier *= 3f;
                    break;
                case "swashbuckler":
                    result.Multiplier += SumOtherJokerSellValues(performer);
                    break;
                case "throwback":
                    result.Multiplier *= 1f + 0.25f * _blindsSkippedThisRun;
                    break;
                case "flower_pot":
                    applied = ContainsAllSuits(playedCards, result);
                    if (applied) result.Multiplier *= 3f;
                    break;
                case "seeing_double":
                    applied = ContainsClubAndOtherSuit(playedCards, result);
                    if (applied) result.Multiplier *= 2f;
                    break;
                case "matador":
                    applied = DidTriggerBossRule(playedCards, result);
                    if (applied) Money += 8;
                    break;
                case "duo":
                    applied = ContainsPair(result.Kind);
                    if (applied) result.Multiplier *= 2f;
                    break;
                case "trio":
                    applied = ContainsThree(result.Kind);
                    if (applied) result.Multiplier *= 3f;
                    break;
                case "family":
                    applied = ContainsFour(result.Kind);
                    if (applied) result.Multiplier *= 4f;
                    break;
                case "order":
                    applied = ContainsStraight(result.Kind);
                    if (applied) result.Multiplier *= 3f;
                    break;
                case "tribe":
                    applied = ContainsFlush(result.Kind);
                    if (applied) result.Multiplier *= 2f;
                    break;
                case "stuntman":
                    result.Chips += 250;
                    break;
                case "drivers_license":
                    applied = CountCards(card => card.Enhancement != CarnivalCardEnhancement.None) >= 16;
                    if (applied) result.Multiplier *= 3f;
                    break;
                case "bootstraps":
                    result.Multiplier += 2f * Math.Max(0, Money / 5);
                    break;
                case "obelisk":
                    result.Multiplier *= Math.Max(1f, state.Value);
                    break;
                default:
                    break;
            }

            if (!applied)
                return;

            if (applyEdition)
                ApplyJokerEdition(state, result);
            if (performer.Rarity == "罕见" && HasJoker("baseball"))
                result.Multiplier *= 1.5f;
        }

        private void ApplyCopiedJoker(
            CarnivalPerformer copier,
            int offset,
            List<CarnivalCard> playedCards,
            CarnivalScoreResult result,
            int copyDepth)
        {
            int targetIndex = GetPerformerIndex(copier) + offset;
            if (targetIndex < 0 || targetIndex >= _performers.Count)
                return;

            CarnivalPerformer target = _performers[targetIndex];
            if (!target.BlueprintCompatible)
                return;
            ApplyBalatroIndependentJoker(target, playedCards, result, copyDepth + 1, false);
        }

        private int GetPerformerIndex(CarnivalPerformer performer)
        {
            for (int index = 0; index < _performers.Count; index++)
            {
                if (ReferenceEquals(_performers[index], performer))
                    return index;
            }

            return -1;
        }

        private static void ApplyJokerEdition(CarnivalJokerState state, CarnivalScoreResult result)
        {
            switch (state.Edition)
            {
                case CarnivalCardEdition.Foil:
                    result.Chips += 50;
                    break;
                case CarnivalCardEdition.Holographic:
                    result.Multiplier += 10f;
                    break;
                case CarnivalCardEdition.Polychrome:
                    result.Multiplier *= 1.5f;
                    break;
            }
        }

        private bool RollChance(int denominator)
        {
            int numerator = HasJoker("oops") ? 2 : 1;
            return _random.NextDouble() < Math.Min(1d, (double)numerator / denominator);
        }

        private bool AllHeldCardsAreBlack(List<CarnivalCard> playedCards)
        {
            var playedIds = new HashSet<int>();
            foreach (CarnivalCard card in playedCards)
                playedIds.Add(card.Id);

            foreach (CarnivalCard card in _hand)
            {
                if (playedIds.Contains(card.Id))
                    continue;
                if (!IsSuit(card, CarnivalSuit.Spades) && !IsSuit(card, CarnivalSuit.Clubs))
                    return false;
            }

            return true;
        }

        private int CountCards(Predicate<CarnivalCard> predicate)
        {
            int count = 0;
            CountCardsIn(_deck, predicate, ref count);
            CountCardsIn(_hand, predicate, ref count);
            CountCardsIn(_discardPile, predicate, ref count);
            return count;
        }

        private static void CountCardsIn(
            List<CarnivalCard> cards,
            Predicate<CarnivalCard> predicate,
            ref int count)
        {
            foreach (CarnivalCard card in cards)
            {
                if (predicate(card))
                    count++;
            }
        }

        private int SumOtherJokerSellValues(CarnivalPerformer excluded)
        {
            int total = 0;
            foreach (CarnivalPerformer performer in _performers)
            {
                if (!ReferenceEquals(performer, excluded))
                    total += GetJokerState(performer).SellValue;
            }

            return total;
        }

        private bool ContainsAllSuits(List<CarnivalCard> cards, CarnivalScoreResult result)
        {
            foreach (CarnivalSuit suit in Enum.GetValues(typeof(CarnivalSuit)))
            {
                bool found = false;
                foreach (CarnivalCard card in cards)
                {
                    if (result.ScoringCardIds.Contains(card.Id) && IsSuit(card, suit))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return false;
            }

            return true;
        }

        private bool ContainsClubAndOtherSuit(List<CarnivalCard> cards, CarnivalScoreResult result)
        {
            bool club = false;
            bool other = false;
            foreach (CarnivalCard card in cards)
            {
                if (!result.ScoringCardIds.Contains(card.Id))
                    continue;
                club |= IsSuit(card, CarnivalSuit.Clubs);
                other |= IsSuit(card, CarnivalSuit.Spades) ||
                         IsSuit(card, CarnivalSuit.Hearts) ||
                         IsSuit(card, CarnivalSuit.Diamonds);
            }

            return club && other;
        }

        private static bool ContainsThree(CarnivalHandKind kind)
        {
            return kind == CarnivalHandKind.ThreeOfAKind ||
                   kind == CarnivalHandKind.FullHouse ||
                   kind == CarnivalHandKind.FlushHouse ||
                   kind == CarnivalHandKind.FourOfAKind ||
                   kind == CarnivalHandKind.FiveOfAKind ||
                   kind == CarnivalHandKind.FlushFive;
        }

        private static bool ContainsTwoPair(CarnivalHandKind kind)
        {
            return kind == CarnivalHandKind.TwoPair ||
                   kind == CarnivalHandKind.FullHouse ||
                   kind == CarnivalHandKind.FlushHouse;
        }

        private static bool ContainsFour(CarnivalHandKind kind)
        {
            return kind == CarnivalHandKind.FourOfAKind ||
                   kind == CarnivalHandKind.FiveOfAKind ||
                   kind == CarnivalHandKind.FlushFive;
        }

        private static bool ContainsStraight(CarnivalHandKind kind)
        {
            return kind == CarnivalHandKind.Straight ||
                   kind == CarnivalHandKind.StraightFlush;
        }

        private static bool ContainsFlush(CarnivalHandKind kind)
        {
            return kind == CarnivalHandKind.Flush ||
                   kind == CarnivalHandKind.StraightFlush ||
                   kind == CarnivalHandKind.FlushHouse ||
                   kind == CarnivalHandKind.FlushFive;
        }

        private bool DidTriggerBossRule(
            List<CarnivalCard> playedCards,
            CarnivalScoreResult result)
        {
            if (CurrentBlind == null || CurrentBlind.Tier != CarnivalBlindTier.Boss || _bossBlindDisabled)
                return false;
            if (IsBossRuleActive(CarnivalBossRule.HalveBaseScore))
                return true;
            if (!IsBossRuleActive(CarnivalBossRule.DebuffFaceCards))
                return false;

            bool pareidolia = HasJoker("pareidolia");
            foreach (CarnivalCard card in playedCards)
            {
                if (result.ScoringCardIds.Contains(card.Id) && IsFaceCard(card, pareidolia))
                    return true;
            }

            return false;
        }
    }
}
