using System;
using System.Collections.Generic;

namespace GameLogic.Core
{
    public sealed partial class CarnivalPokerGame
    {
        private void ApplyJokers(CarnivalJokerTrigger trigger, CarnivalJokerContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.Trigger = trigger;
            var snapshot = new List<CarnivalPerformer>(_performers);
            foreach (CarnivalPerformer performer in snapshot)
            {
                if (!_performers.Contains(performer))
                    continue;

                ExecuteJoker(
                    performer,
                    context,
                    null,
                    0,
                    new HashSet<CarnivalPerformer>());
            }
        }

        private void ExecuteJoker(
            CarnivalPerformer performer,
            CarnivalJokerContext context,
            CarnivalPerformer copiedBy,
            int copyDepth,
            HashSet<CarnivalPerformer> copyChain)
        {
            if (performer == null || copyDepth > _performers.Count || !copyChain.Add(performer))
                return;

            if (performer.Id == "blueprint" || performer.Id == "brainstorm")
            {
                CarnivalPerformer target = ResolveCopyTarget(performer);
                if (target != null && target.BlueprintCompatible)
                    ExecuteJoker(target, context, performer, copyDepth + 1, copyChain);
                copyChain.Remove(performer);
                return;
            }

            bool previousCopiedEffect = context.IsCopiedEffect;
            CarnivalPerformer previousCopiedBy = context.CopiedBy;
            context.IsCopiedEffect = copiedBy != null;
            context.CopiedBy = copiedBy;
            ExecuteJokerEffect(performer, context);
            context.IsCopiedEffect = previousCopiedEffect;
            context.CopiedBy = previousCopiedBy;
            copyChain.Remove(performer);
        }

        private CarnivalPerformer ResolveCopyTarget(CarnivalPerformer copier)
        {
            int copierIndex = GetPerformerIndex(copier);
            if (copierIndex < 0)
                return null;

            int targetIndex = copier.Id == "blueprint" ? copierIndex + 1 : 0;
            if (targetIndex < 0 || targetIndex >= _performers.Count || targetIndex == copierIndex)
                return null;
            return _performers[targetIndex];
        }

        private int CountJokerAbilityOccurrences(string performerId)
        {
            int count = 0;
            foreach (CarnivalPerformer performer in _performers)
            {
                CarnivalPerformer ability = ResolveCopiedAbility(
                    performer,
                    0,
                    new HashSet<CarnivalPerformer>());
                if (ability != null && ability.Id == performerId)
                    count++;
            }

            return count;
        }

        private CarnivalPerformer ResolveCopiedAbility(
            CarnivalPerformer performer,
            int copyDepth,
            HashSet<CarnivalPerformer> copyChain)
        {
            if (performer == null || copyDepth > _performers.Count || !copyChain.Add(performer))
                return null;
            if (performer.Id != "blueprint" && performer.Id != "brainstorm")
                return performer;

            CarnivalPerformer target = ResolveCopyTarget(performer);
            return target != null && target.BlueprintCompatible
                ? ResolveCopiedAbility(target, copyDepth + 1, copyChain)
                : null;
        }

        private void ExecuteJokerEffect(CarnivalPerformer performer, CarnivalJokerContext context)
        {
            CarnivalJokerState state = GetJokerState(performer);
            switch (context.Trigger)
            {
                case CarnivalJokerTrigger.BlindSelected:
                    ApplyBlindSelectedJoker(performer, state);
                    break;
                case CarnivalJokerTrigger.InitialHandDrawn:
                    if (performer.Id == "certificate")
                    {
                        var card = new CarnivalCard(
                            _nextCardId++,
                            (CarnivalSuit)_random.Next(0, 4),
                            _random.Next(2, 15),
                            CarnivalCardEnhancement.None,
                            (CarnivalCardSeal)_random.Next(1, 5));
                        _hand.Add(card);
                        NotifyPlayingCardAdded();
                    }
                    break;
                case CarnivalJokerTrigger.BeforeHandScored:
                    ApplyBeforeHandScoredJoker(performer, state, context);
                    break;
                case CarnivalJokerTrigger.AfterHandScored:
                    ApplyAfterHandScoredJoker(performer, state, context);
                    break;
                case CarnivalJokerTrigger.CardDiscarded:
                    ApplyCardDiscardedJoker(performer, state, context);
                    break;
                case CarnivalJokerTrigger.CardDestroyed:
                    ApplyCardDestroyedJoker(performer, state, context);
                    break;
                case CarnivalJokerTrigger.PlayingCardAdded:
                    if (performer.Id == "hologram")
                        state.Value += CarnivalJokerParameters.GetFloat(performer, "extra", 0.25f);
                    break;
                case CarnivalJokerTrigger.ConsumableUsed:
                    if (performer.Id == "constellation" &&
                        context.Consumable?.Family == CarnivalConsumableFamily.Planet)
                    {
                        state.Value += CarnivalJokerParameters.GetFloat(performer, "extra", 0.1f);
                    }
                    break;
                case CarnivalJokerTrigger.CardSold:
                    if (performer.Id == "campfire")
                        state.Value += CarnivalJokerParameters.GetFloat(performer, "extra", 0.25f);
                    break;
                case CarnivalJokerTrigger.BoosterOpened:
                    if (performer.Id == "hallucination" && RollChance(2))
                        TryCreateConsumable(CarnivalConsumableFamily.Tarot);
                    break;
                case CarnivalJokerTrigger.BoosterSkipped:
                    if (performer.Id == "red_card")
                        state.Value += CarnivalJokerParameters.GetFloat(performer, "extra", 3f);
                    break;
                case CarnivalJokerTrigger.RoundEnded:
                    ApplyRoundEndedJoker(performer, state);
                    break;
                case CarnivalJokerTrigger.ShopEnded:
                    if (performer.Id == "perkeo" && _consumables.Count > 0)
                    {
                        CarnivalConsumableState source = _consumables[_random.Next(_consumables.Count)];
                        _consumables.Add(source.CreateCopy(CarnivalCardEdition.Negative));
                    }
                    break;
            }
        }

        private void ApplyBlindSelectedJoker(CarnivalPerformer performer, CarnivalJokerState state)
        {
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
                        state.Value += CarnivalJokerParameters.GetFloat(performer, "extra", 0.5f);
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

        private void ApplyBeforeHandScoredJoker(
            CarnivalPerformer performer,
            CarnivalJokerState state,
            CarnivalJokerContext context)
        {
            if (!context.HandKind.HasValue || context.PlayedCards == null || context.ScoreResult == null)
                return;

            switch (performer.Id)
            {
                case "runner":
                    if (ContainsStraight(context.HandKind.Value))
                        state.Value += CarnivalJokerParameters.GetFloat(performer, "extra", 15f);
                    break;
                case "square":
                    if (context.PlayedCards.Count == 4)
                        state.Value += CarnivalJokerParameters.GetFloat(performer, "extra", 4f);
                    break;
                case "trousers":
                    if (ContainsTwoPair(context.HandKind.Value))
                        state.Value += CarnivalJokerParameters.GetFloat(performer, "extra", 2f);
                    break;
                case "ride_the_bus":
                    bool hasScoringFace = false;
                    foreach (CarnivalCard card in context.PlayedCards)
                    {
                        if (context.ScoreResult.ScoringCardIds.Contains(card.Id) &&
                            IsFaceCard(card, HasJoker("pareidolia")))
                        {
                            hasScoringFace = true;
                            break;
                        }
                    }

                    state.Value = hasScoringFace
                        ? 0f
                        : state.Value + CarnivalJokerParameters.GetFloat(performer, "extra", 1f);
                    break;
                case "green_joker":
                    state.Value += CarnivalJokerParameters.GetFloat(performer, "extra", 1f);
                    break;
                case "obelisk":
                    if (IsMostPlayedHandBeforeCurrent(context.HandKind.Value))
                        state.Value = 1f;
                    else
                        state.Value += CarnivalJokerParameters.GetFloat(performer, "extra", 0.2f);
                    break;
            }
        }

        private void ApplyAfterHandScoredJoker(
            CarnivalPerformer performer,
            CarnivalJokerState state,
            CarnivalJokerContext context)
        {
            if (context.PlayedCards == null || context.ScoreResult == null)
                return;

            switch (performer.Id)
            {
                case "loyalty_card":
                    state.Counter = state.Counter == 0 ? 5 : state.Counter - 1;
                    break;
                case "space":
                    if (RollChance(4))
                        _handLevels[context.ScoreResult.Kind].Upgrade();
                    break;
                case "ice_cream":
                    state.Value = Math.Max(
                        0f,
                        state.Value - CarnivalJokerParameters.GetFloat(performer, "extra", 5f));
                    if (state.Value <= 0f)
                        RemoveOwnedPerformer(performer);
                    break;
                case "dna":
                    if (_handsPlayedThisRound == 1 && context.PlayedCards.Count == 1)
                    {
                        CarnivalCard source = context.PlayedCards[0];
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
                    break;
                case "sixth_sense":
                    if (_handsPlayedThisRound == 1 &&
                        context.PlayedCards.Count == 1 &&
                        context.PlayedCards[0].Rank == 6)
                    {
                        DestroyPlayingCard(
                            context.PlayedCards[0].Id,
                            CarnivalDestroyReason.Joker);
                        TryCreateConsumable(CarnivalConsumableFamily.Spectral);
                    }
                    break;
                case "superposition":
                    if (ContainsStraight(context.ScoreResult.Kind) &&
                        ContainsRank(new List<CarnivalCard>(context.PlayedCards), 14))
                    {
                        TryCreateConsumable(CarnivalConsumableFamily.Tarot);
                    }
                    break;
                case "seance":
                    if (context.ScoreResult.Kind == CarnivalHandKind.StraightFlush)
                        TryCreateConsumable(CarnivalConsumableFamily.Spectral);
                    break;
                case "vagabond":
                    if (Money <= 4)
                        TryCreateConsumable(CarnivalConsumableFamily.Tarot);
                    break;
                case "selzer":
                    state.Counter = Math.Max(0, state.Counter - 1);
                    if (state.Counter == 0)
                        RemoveOwnedPerformer(performer);
                    break;
            }
        }

        private void ApplyCardDiscardedJoker(
            CarnivalPerformer performer,
            CarnivalJokerState state,
            CarnivalJokerContext context)
        {
            if (context.PlayedCards == null || !context.HandKind.HasValue)
                return;

            var discardedCards = new List<CarnivalCard>(context.PlayedCards);
            switch (performer.Id)
            {
                case "faceless":
                    if (CountMatches(
                            discardedCards,
                            card => IsFaceCard(card, HasJoker("pareidolia"))) >= 3)
                    {
                        Money += 5;
                    }
                    break;
                case "green_joker":
                    state.Value = Math.Max(
                        0f,
                        state.Value - CarnivalJokerParameters.GetFloat(performer, "extra", 1f));
                    break;
                case "mail":
                    Money += 5 * CountMatches(discardedCards, card => card.Rank == state.Rank);
                    break;
                case "trading":
                    if (context.IsFirstDiscard && discardedCards.Count == 1)
                    {
                        DestroyPlayingCard(
                            discardedCards[0].Id,
                            CarnivalDestroyReason.DiscardEffect);
                        Money += 3;
                    }
                    break;
                case "ramen":
                    state.Value = Math.Max(1f, state.Value - 0.01f * discardedCards.Count);
                    if (state.Value <= 1f)
                        RemoveOwnedPerformer(performer);
                    break;
                case "castle":
                    state.Value += 3f * CountMatches(
                        discardedCards,
                        card => IsSuit(card, state.Suit));
                    break;
                case "hit_the_road":
                    state.Value += 0.5f * CountMatches(discardedCards, card => card.Rank == 11);
                    break;
                case "burnt":
                    if (context.IsFirstDiscard && discardedCards.Count > 0)
                        _handLevels[context.HandKind.Value].Upgrade();
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

        private void ApplyCardDestroyedJoker(
            CarnivalPerformer performer,
            CarnivalJokerState state,
            CarnivalJokerContext context)
        {
            if (!context.DestroyedCard.HasValue)
                return;

            CarnivalCard destroyedCard = context.DestroyedCard.Value;
            if (performer.Id == "caino" && destroyedCard.IsFace)
                state.Value += CarnivalJokerParameters.GetFloat(performer, "extra", 1f);
            else if (performer.Id == "glass" &&
                     destroyedCard.Enhancement == CarnivalCardEnhancement.Glass)
                state.Value += CarnivalJokerParameters.GetFloat(performer, "extra", 0.75f);
        }

        private void ApplyRoundEndedJoker(CarnivalPerformer performer, CarnivalJokerState state)
        {
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
                        RemoveOwnedPerformer(performer);
                    }
                    break;
                case "cavendish":
                    if (RollChance(1000))
                        RemoveOwnedPerformer(performer);
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
                    foreach (CarnivalConsumableState consumable in _consumables)
                        consumable.SellValue++;
                    break;
                case "turtle_bean":
                    state.Counter = Math.Max(0, state.Counter - 1);
                    if (state.Counter == 0)
                        RemoveOwnedPerformer(performer);
                    break;
                case "golden":
                    Money += 4;
                    break;
                case "popcorn":
                    state.Value = Math.Max(0f, state.Value - 4f);
                    if (state.Value <= 0f)
                        RemoveOwnedPerformer(performer);
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
        }

        private bool IsBossRuleActive(CarnivalBossRule rule)
        {
            return CurrentBlind != null &&
                   CurrentBlind.Tier == CarnivalBlindTier.Boss &&
                   !_bossBlindDisabled &&
                   CurrentBlind.BossRule == rule;
        }

        private bool HasConsumableSlot(int releasingOccupiedSlots = 0)
        {
            return CountOccupiedConsumableSlots() - releasingOccupiedSlots < MaxConsumables;
        }

        private int CountOccupiedConsumableSlots()
        {
            int count = 0;
            foreach (CarnivalConsumableState consumable in _consumables)
            {
                if (consumable.OccupiesSlot)
                    count++;
            }

            return count;
        }

        private bool HasConsumable(string consumableId)
        {
            foreach (CarnivalConsumableState consumable in _consumables)
            {
                if (consumable.Id == consumableId)
                    return true;
            }

            return false;
        }

        private CarnivalConsumableState AddOwnedConsumable(
            CarnivalConsumable content,
            CarnivalCardEdition edition = CarnivalCardEdition.Base)
        {
            var state = new CarnivalConsumableState(content, edition);
            _consumables.Add(state);
            RecordConsumableDiscovered(content);
            return state;
        }

        private bool DestroyPlayingCard(int cardId, CarnivalDestroyReason reason)
        {
            if (!TryGetCard(cardId, out CarnivalCard card))
                return false;

            _deck.RemoveAll(item => item.Id == cardId);
            _hand.RemoveAll(item => item.Id == cardId);
            _discardPile.RemoveAll(item => item.Id == cardId);
            _selectedCardIds.Remove(cardId);
            ApplyJokers(
                CarnivalJokerTrigger.CardDestroyed,
                new CarnivalJokerContext
                {
                    Trigger = CarnivalJokerTrigger.CardDestroyed,
                    DestroyedCard = card,
                    DestroyReason = reason,
                });
            return true;
        }

        private bool TryGetCard(int cardId, out CarnivalCard card)
        {
            if (TryGetCardIn(_hand, cardId, out card) ||
                TryGetCardIn(_deck, cardId, out card) ||
                TryGetCardIn(_discardPile, cardId, out card))
            {
                return true;
            }

            card = default;
            return false;
        }

        private static bool TryGetCardIn(List<CarnivalCard> cards, int cardId, out CarnivalCard card)
        {
            foreach (CarnivalCard candidate in cards)
            {
                if (candidate.Id == cardId)
                {
                    card = candidate;
                    return true;
                }
            }

            card = default;
            return false;
        }

        private bool IsMostPlayedHandBeforeCurrent(CarnivalHandKind kind)
        {
            _handPlayCounts.TryGetValue(kind, out int current);
            foreach (KeyValuePair<CarnivalHandKind, int> pair in _handPlayCounts)
            {
                if (pair.Key != kind && pair.Value >= current + 1)
                    return false;
            }

            return true;
        }
    }
}
