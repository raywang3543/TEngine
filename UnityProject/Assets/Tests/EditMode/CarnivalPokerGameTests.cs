using GameLogic.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace GameLogic.Tests
{
    public sealed class CarnivalPokerGameTests
    {
        public static IEnumerable<TestCaseData> AllJokerCases()
        {
            CarnivalContentModel content = CreateContent();
            foreach (CarnivalPerformer performer in content.Performers)
            {
                yield return new TestCaseData(performer.Id)
                    .SetName($"Joker_{performer.Id}_RuntimeSmoke");
            }
        }

        [TestCaseSource(nameof(AllJokerCases))]
        public void EveryJoker_CanResolveARealScoringHand(string jokerId)
        {
            CarnivalContentModel baseContent = CreateContent();
            CarnivalPerformer source = baseContent.FindPerformer(jokerId);
            var game = new CarnivalPokerGame(
                new TestContentModel(
                    baseContent,
                    new[] { CreateStartingJoker(source) }),
                163);

            Assert.DoesNotThrow(game.StartNewRun);
            Assert.That(game.Performers, Has.Some.Matches<CarnivalPerformer>(item => item.Id == jokerId));
            game.ToggleCard(game.Hand[0].Id);
            CarnivalScoreResult result = null;
            Assert.DoesNotThrow(() => result = game.PlaySelected());
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Score, Is.GreaterThan(0));
        }

        [Test]
        public void StartNewRun_CreatesSmallBlindWithNoStartingJokers()
        {
            CarnivalPokerGame game = CreateGame(17);

            game.StartNewRun();

            Assert.That(game.Phase, Is.EqualTo(CarnivalRunPhase.Playing));
            Assert.That(game.Ante, Is.EqualTo(1));
            Assert.That(game.CurrentBlind.Tier, Is.EqualTo(CarnivalBlindTier.Small));
            Assert.That(game.Hand, Has.Count.EqualTo(8));
            Assert.That(game.Performers, Is.Empty);
            Assert.That(game.HandLevels[CarnivalHandKind.Pair].Level, Is.EqualTo(1));
        }

        [Test]
        public void SkipBlind_AdvancesToBossAndBossCannotBeSkipped()
        {
            CarnivalPokerGame game = CreateGame(31);
            game.StartNewRun();

            Assert.That(game.SkipBlind(), Is.True);
            Assert.That(game.CurrentBlind.Tier, Is.EqualTo(CarnivalBlindTier.Big));
            Assert.That(game.SkipBlind(), Is.True);
            Assert.That(game.CurrentBlind.Tier, Is.EqualTo(CarnivalBlindTier.Boss));
            Assert.That(game.SkipBlind(), Is.False);
            Assert.That(game.Round, Is.EqualTo(3));
        }

        [Test]
        public void FiveCardBoss_RejectsAHandWithFewerThanFiveCards()
        {
            CarnivalPokerGame game = CreateGame(53);
            game.StartNewRun();
            game.SkipBlind();
            game.SkipBlind();
            CarnivalCard card = game.Hand[0];

            game.ToggleCard(card.Id);
            CarnivalScoreResult result = game.PlaySelected();

            Assert.That(result, Is.Null);
            Assert.That(game.HandsRemaining, Is.EqualTo(4));
            Assert.That(game.StatusMessage, Does.Contain("5 张"));
        }

        [Test]
        public void ContentCatalog_MatchesBalatro101oCategoryCounts()
        {
            CarnivalContentModel content = CreateContent();
            var familyCounts = new Dictionary<CarnivalConsumableFamily, int>();
            var performerIds = new HashSet<string>();
            var consumableIds = new HashSet<string>();

            foreach (CarnivalPerformer performer in content.Performers)
                performerIds.Add(performer.Id);
            foreach (CarnivalConsumable consumable in content.Consumables)
            {
                consumableIds.Add(consumable.Id);
                familyCounts.TryGetValue(consumable.Family, out int count);
                familyCounts[consumable.Family] = count + 1;
            }

            Assert.That(content.Performers.Count, Is.EqualTo(150));
            Assert.That(performerIds.Count, Is.EqualTo(150));
            Assert.That(familyCounts[CarnivalConsumableFamily.Tarot], Is.EqualTo(22));
            Assert.That(familyCounts[CarnivalConsumableFamily.Planet], Is.EqualTo(12));
            Assert.That(familyCounts[CarnivalConsumableFamily.Spectral], Is.EqualTo(18));
            Assert.That(consumableIds.Count, Is.EqualTo(52));
        }

        [Test]
        public void PerformerCatalog_UsesOfficialRarityDistribution()
        {
            CarnivalContentModel content = CreateContent();
            var counts = new Dictionary<string, int>();

            foreach (CarnivalPerformer performer in content.Performers)
            {
                counts.TryGetValue(performer.Rarity, out int count);
                counts[performer.Rarity] = count + 1;
            }

            Assert.That(counts["普通"], Is.EqualTo(61));
            Assert.That(counts["罕见"], Is.EqualTo(64));
            Assert.That(counts["稀有"], Is.EqualTo(20));
            Assert.That(counts["传说"], Is.EqualTo(5));
        }

        [Test]
        public void PlanetCatalog_CoversAllTwelveHandKinds()
        {
            CarnivalContentModel content = CreateContent();
            var kinds = new HashSet<CarnivalHandKind>();

            foreach (CarnivalConsumable consumable in content.Consumables)
            {
                if (consumable.Family == CarnivalConsumableFamily.Planet)
                    kinds.Add(consumable.HandKind.Value);
            }

            Assert.That(kinds.Count, Is.EqualTo(12));
            Assert.That(kinds, Does.Contain(CarnivalHandKind.FiveOfAKind));
            Assert.That(kinds, Does.Contain(CarnivalHandKind.FlushHouse));
            Assert.That(kinds, Does.Contain(CarnivalHandKind.FlushFive));
        }

        [Test]
        public void Hallucination_CreatesTarotWhenBoosterPackOpens()
        {
            CarnivalContentModel baseContent = CreateContent();
            var performers = new List<CarnivalPerformer>
            {
                CreateStartingJoker("hallucination", "幻觉"),
                CreateStartingJoker("oops", "六六大顺"),
                new CarnivalPerformer(
                    "test_score",
                    "测试计分",
                    "测试计分",
                    "确保一手击破盲注。",
                    0,
                    "普通",
                    "🃏",
                    true,
                    CarnivalPerformerEffect.FlatChips,
                    1000f),
            };
            var game = new CarnivalPokerGame(
                new TestContentModel(baseContent, performers),
                73);
            game.StartNewRun();
            game.ToggleCard(game.Hand[0].Id);

            game.PlaySelected();

            Assert.That(game.Phase, Is.EqualTo(CarnivalRunPhase.Shop));
            Assert.That(game.BuyBoosterPack(), Is.True);
            Assert.That(game.IsBoosterOpen, Is.True);
            Assert.That(game.Consumables, Has.Count.EqualTo(1));
            Assert.That(game.Consumables[0].Family, Is.EqualTo(CarnivalConsumableFamily.Tarot));
        }

        [Test]
        public void DietCola_DoublesNextBlindTagAndIsConsumed()
        {
            CarnivalContentModel baseContent = CreateContent();
            var performers = new List<CarnivalPerformer>
            {
                CreateStartingJoker("diet_cola", "饮料可乐"),
            };
            var game = new CarnivalPokerGame(
                new TestContentModel(baseContent, performers),
                97);
            game.StartNewRun();

            Assert.That(game.SellPerformer(0), Is.True);
            Assert.That(game.DoubleTagCount, Is.EqualTo(1));
            Assert.That(game.SkipBlind(), Is.True);
            Assert.That(game.DoubleTagCount, Is.Zero);
            Assert.That(game.TagsCollectedThisRun, Is.EqualTo(2));
        }

        [Test]
        public void Chicot_DisablesFiveCardBossRuleAtEveryEntryPoint()
        {
            CarnivalContentModel baseContent = CreateContent();
            var game = new CarnivalPokerGame(
                new TestContentModel(
                    baseContent,
                    new[] { CreateStartingJoker("chicot", "希科") }),
                113);
            game.StartNewRun();
            game.SkipBlind();
            game.SkipBlind();
            game.ToggleCard(game.Hand[0].Id);

            CarnivalScoreResult result = game.PlaySelected();

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void FourFingers_StraightScoresOnlyTheFourCardsFormingIt()
        {
            CarnivalContentModel baseContent = CreateContent();
            var game = new CarnivalPokerGame(
                new TestContentModel(
                    baseContent,
                    new[] { CreateStartingJoker("four_fingers", "四指") }),
                127);
            game.StartNewRun();
            var cards = new List<CarnivalCard>
            {
                new CarnivalCard(1001, CarnivalSuit.Hearts, 2),
                new CarnivalCard(1002, CarnivalSuit.Spades, 3),
                new CarnivalCard(1003, CarnivalSuit.Diamonds, 4),
                new CarnivalCard(1004, CarnivalSuit.Clubs, 5),
                new CarnivalCard(1005, CarnivalSuit.Hearts, 13),
            };

            CarnivalHandEvaluation evaluation = InvokePrivate<CarnivalHandEvaluation>(
                game,
                "EvaluateHand",
                cards);

            Assert.That(evaluation.Kind, Is.EqualTo(CarnivalHandKind.Straight));
            Assert.That(evaluation.ScoringCardIds, Has.Count.EqualTo(4));
            Assert.That(evaluation.ScoringCardIds, Has.None.EqualTo(1005));
        }

        [Test]
        public void StoneCard_DoesNotBuildRankGroupButAlwaysScores()
        {
            CarnivalPokerGame game = CreateGame(131);
            game.StartNewRun();
            var cards = new List<CarnivalCard>
            {
                new CarnivalCard(1101, CarnivalSuit.Hearts, 8),
                new CarnivalCard(1102, CarnivalSuit.Spades, 8),
                new CarnivalCard(
                    1103,
                    CarnivalSuit.Diamonds,
                    8,
                    CarnivalCardEnhancement.Stone),
            };

            CarnivalHandEvaluation evaluation = InvokePrivate<CarnivalHandEvaluation>(
                game,
                "EvaluateHand",
                cards);

            Assert.That(evaluation.Kind, Is.EqualTo(CarnivalHandKind.Pair));
            Assert.That(evaluation.ScoringCardIds, Is.EquivalentTo(new[] { 1101, 1102, 1103 }));
        }

        [Test]
        public void Constellation_PlanetUseGrowsBeforeLaterScoring()
        {
            CarnivalContentModel baseContent = CreateContent();
            var game = new CarnivalPokerGame(
                new TestContentModel(
                    baseContent,
                    new[] { CreateStartingJoker("constellation", "星座") }),
                137);
            game.StartNewRun();
            CarnivalConsumable planet = FindConsumable(baseContent, CarnivalConsumableFamily.Planet);
            GetPrivateField<List<CarnivalConsumableState>>(game, "_consumables")
                .Add(new CarnivalConsumableState(planet));

            Assert.That(game.UseConsumable(planet.Id), Is.True);

            CarnivalJokerState state = GetJokerState(game, "constellation");
            Assert.That(state.Value, Is.EqualTo(1.1f).Within(0.0001f));
        }

        [Test]
        public void Canio_DestroyedFaceCardGrowsExactlyOnce()
        {
            CarnivalContentModel baseContent = CreateContent();
            var game = new CarnivalPokerGame(
                new TestContentModel(
                    baseContent,
                    new[] { CreateStartingJoker("caino", "卡尼奥") }),
                139);
            game.StartNewRun();
            CarnivalCard faceCard = FindCard(game, card => card.IsFace);

            bool destroyed = InvokePrivate<bool>(
                game,
                "DestroyPlayingCard",
                faceCard.Id,
                CarnivalDestroyReason.Consumable);

            Assert.That(destroyed, Is.True);
            Assert.That(GetJokerState(game, "caino").Value, Is.EqualTo(2f));
            Assert.That(
                InvokePrivate<bool>(
                    game,
                    "DestroyPlayingCard",
                    faceCard.Id,
                    CarnivalDestroyReason.Consumable),
                Is.False);
            Assert.That(GetJokerState(game, "caino").Value, Is.EqualTo(2f));
        }

        [Test]
        public void GiftCard_RoundEndRaisesBothOwnedCardSellValues()
        {
            CarnivalContentModel baseContent = CreateContent();
            var game = new CarnivalPokerGame(
                new TestContentModel(
                    baseContent,
                    new[] { CreateStartingJoker("gift", "礼品卡") }),
                149);
            game.StartNewRun();
            CarnivalConsumableState consumable = new CarnivalConsumableState(
                baseContent.Consumables[0]);
            GetPrivateField<List<CarnivalConsumableState>>(game, "_consumables").Add(consumable);
            int jokerSellValue = game.GetPerformerSellValue(0);
            int consumableSellValue = consumable.SellValue;

            InvokePrivate<object>(game, "ApplyEndOfRoundJokers");

            Assert.That(game.GetPerformerSellValue(0), Is.EqualTo(jokerSellValue + 1));
            Assert.That(consumable.SellValue, Is.EqualTo(consumableSellValue + 1));
        }

        [Test]
        public void Perkeo_ShopEndCreatesIndependentNegativeConsumable()
        {
            CarnivalContentModel baseContent = CreateContent();
            var game = new CarnivalPokerGame(
                new TestContentModel(
                    baseContent,
                    new[] { CreateStartingJoker("perkeo", "帕奇欧") }),
                151);
            game.StartNewRun();
            var consumable = new CarnivalConsumableState(
                baseContent.Consumables[0]);
            List<CarnivalConsumableState> owned =
                GetPrivateField<List<CarnivalConsumableState>>(game, "_consumables");
            owned.Add(consumable);

            InvokePrivate<object>(game, "ApplyEndOfShopJokers");

            Assert.That(owned, Has.Count.EqualTo(2));
            Assert.That(owned[1], Is.Not.SameAs(consumable));
            Assert.That(owned[1].Content, Is.SameAs(consumable.Content));
            Assert.That(owned[1].Edition, Is.EqualTo(CarnivalCardEdition.Negative));
            Assert.That(owned[1].OccupiesSlot, Is.False);
        }

        [Test]
        public void Campfire_SellingConsumableGrowsItsMultiplier()
        {
            CarnivalContentModel baseContent = CreateContent();
            var game = new CarnivalPokerGame(
                new TestContentModel(
                    baseContent,
                    new[] { CreateStartingJoker("campfire", "篝火") }),
                157);
            game.StartNewRun();
            var consumable = new CarnivalConsumableState(
                baseContent.Consumables[0]);
            GetPrivateField<List<CarnivalConsumableState>>(game, "_consumables").Add(consumable);

            Assert.That(game.SellConsumable(consumable.Id), Is.True);

            Assert.That(GetJokerState(game, "campfire").Value, Is.EqualTo(1.25f));
        }

        [Test]
        public void Runner_StraightGrowthAppliesToTheCurrentHand()
        {
            CarnivalContentModel baseContent = CreateContent();
            var game = new CarnivalPokerGame(
                new TestContentModel(
                    baseContent,
                    new[] { CreateStartingJoker("runner", "跑步选手") }),
                167);
            game.StartNewRun();
            var cards = new List<CarnivalCard>
            {
                new CarnivalCard(1201, CarnivalSuit.Hearts, 2),
                new CarnivalCard(1202, CarnivalSuit.Spades, 3),
                new CarnivalCard(1203, CarnivalSuit.Diamonds, 4),
                new CarnivalCard(1204, CarnivalSuit.Clubs, 5),
                new CarnivalCard(1205, CarnivalSuit.Hearts, 6),
            };

            CarnivalScoreResult result = InvokePrivate<CarnivalScoreResult>(
                game,
                "Evaluate",
                cards);

            Assert.That(GetJokerState(game, "runner").Value, Is.EqualTo(15f));
            Assert.That(result.Chips, Is.EqualTo(65));
        }

        [Test]
        public void Hiker_RetriggerReadsTheLatestPermanentChipState()
        {
            CarnivalContentModel baseContent = CreateContent();
            var game = new CarnivalPokerGame(
                new TestContentModel(
                    baseContent,
                    new[] { CreateStartingJoker("hiker", "徒步者") }),
                173);
            game.StartNewRun();
            List<CarnivalCard> hand = GetPrivateField<List<CarnivalCard>>(game, "_hand");
            hand.Clear();
            var card = new CarnivalCard(
                1301,
                CarnivalSuit.Hearts,
                2,
                seal: CarnivalCardSeal.Red);
            hand.Add(card);

            CarnivalScoreResult result = InvokePrivate<CarnivalScoreResult>(
                game,
                "Evaluate",
                new List<CarnivalCard> { card });

            Assert.That(result.Chips, Is.EqualTo(14));
            Assert.That(hand[0].PermanentChips, Is.EqualTo(10));
        }

        [Test]
        public void Vampire_RetriggerDoesNotConsumeOneEnhancementTwice()
        {
            CarnivalContentModel baseContent = CreateContent();
            var game = new CarnivalPokerGame(
                new TestContentModel(
                    baseContent,
                    new[] { CreateStartingJoker("vampire", "吸血鬼") }),
                179);
            game.StartNewRun();
            List<CarnivalCard> hand = GetPrivateField<List<CarnivalCard>>(game, "_hand");
            hand.Clear();
            var card = new CarnivalCard(
                1401,
                CarnivalSuit.Hearts,
                2,
                CarnivalCardEnhancement.Bonus,
                CarnivalCardSeal.Red);
            hand.Add(card);

            InvokePrivate<CarnivalScoreResult>(
                game,
                "Evaluate",
                new List<CarnivalCard> { card });

            Assert.That(GetJokerState(game, "vampire").Value, Is.EqualTo(1.1f).Within(0.0001f));
            Assert.That(hand[0].Enhancement, Is.EqualTo(CarnivalCardEnhancement.None));
        }

        [Test]
        public void UnlockModel_ContinueSavedRunUnlocksThrowback()
        {
            CarnivalContentModel baseContent = CreateContent();
            var unlocks = new CarnivalUnlockModel();
            var game = new CarnivalPokerGame(baseContent, unlocks, 181);

            game.NotifyRunContinued();

            Assert.That(unlocks.IsJokerUnlocked("throwback"), Is.True);
            Assert.That(unlocks.Statistics.ContinuedSavedRun, Is.True);
        }

        private static CarnivalPokerGame CreateGame(int seed)
        {
            return new CarnivalPokerGame(CreateContent(), seed);
        }

        private static CarnivalContentModel CreateContent()
        {
            string configDirectory = Path.Combine(Application.dataPath, "AssetRaw/Configs/bytes");
            return CarnivalContentModel.LoadFromBytes(file =>
                File.ReadAllBytes(Path.Combine(configDirectory, $"{file}.bytes")));
        }

        private static CarnivalPerformer CreateStartingJoker(string id, string name)
        {
            return new CarnivalPerformer(
                id,
                name,
                name,
                string.Empty,
                4,
                "普通",
                "🃏",
                true,
                CarnivalPerformerEffect.BalatroOriginal);
        }

        private static CarnivalPerformer CreateStartingJoker(CarnivalPerformer source)
        {
            return new CarnivalPerformer(
                source.Id,
                source.Name,
                source.ShortName,
                source.Description,
                source.Cost,
                source.Rarity,
                source.Icon,
                true,
                source.Effect,
                source.EffectValue,
                source.ConditionValue,
                source.HandKind,
                source.Suit,
                source.OriginalOrder,
                source.NameEn,
                source.DescriptionEn,
                source.UnlockRequirement,
                source.UnlockedByDefault,
                source.Activation,
                source.JokerType,
                source.BlueprintCompatible,
                source.PerishableCompatible,
                source.EternalCompatible,
                source.Parameters);
        }

        private static CarnivalConsumable FindConsumable(
            ICarnivalContentModel content,
            CarnivalConsumableFamily family)
        {
            foreach (CarnivalConsumable consumable in content.Consumables)
            {
                if (consumable.Family == family &&
                    (family != CarnivalConsumableFamily.Planet ||
                     consumable.Action == CarnivalConsumableAction.UpgradeHand))
                {
                    return consumable;
                }
            }

            Assert.Fail($"No {family} consumable suitable for the test.");
            return null;
        }

        private static CarnivalCard FindCard(
            CarnivalPokerGame game,
            Predicate<CarnivalCard> predicate)
        {
            foreach (string fieldName in new[] { "_hand", "_deck", "_discardPile" })
            {
                foreach (CarnivalCard card in GetPrivateField<List<CarnivalCard>>(game, fieldName))
                {
                    if (predicate(card))
                        return card;
                }
            }

            Assert.Fail("No matching playing card exists.");
            return default;
        }

        private static CarnivalJokerState GetJokerState(CarnivalPokerGame game, string jokerId)
        {
            foreach (CarnivalPerformer performer in game.Performers)
            {
                if (performer.Id == jokerId)
                {
                    return InvokePrivate<CarnivalJokerState>(
                        game,
                        "GetJokerState",
                        performer);
                }
            }

            Assert.Fail($"Joker {jokerId} is not owned.");
            return null;
        }

        private static TField GetPrivateField<TField>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field {fieldName}.");
            return (TField)field.GetValue(target);
        }

        private static TResult InvokePrivate<TResult>(
            object target,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method = null;
            foreach (MethodInfo candidate in target.GetType().GetMethods(
                         BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (candidate.Name == methodName &&
                    candidate.GetParameters().Length == arguments.Length)
                {
                    method = candidate;
                    break;
                }
            }

            Assert.That(method, Is.Not.Null, $"Missing private method {methodName}.");
            object result = method.Invoke(target, arguments);
            return result == null ? default : (TResult)result;
        }

        private sealed class TestContentModel : ICarnivalContentModel
        {
            private readonly ICarnivalContentModel _baseContent;

            public TestContentModel(
                ICarnivalContentModel baseContent,
                IReadOnlyList<CarnivalPerformer> performers)
            {
                _baseContent = baseContent;
                Performers = performers;
            }

            public IReadOnlyList<CarnivalPerformer> Performers { get; }
            public IReadOnlyList<CarnivalConsumable> Consumables => _baseContent.Consumables;
            public IReadOnlyDictionary<CarnivalCardEnhancement, CarnivalCardEnhancementContent> Enhancements =>
                _baseContent.Enhancements;

            public CarnivalPerformer FindPerformer(string performerId)
            {
                foreach (CarnivalPerformer performer in Performers)
                {
                    if (performer.Id == performerId)
                        return performer;
                }

                return null;
            }

            public CarnivalCardEnhancementContent FindEnhancement(CarnivalCardEnhancement enhancement)
            {
                return _baseContent.FindEnhancement(enhancement);
            }
        }
    }
}
