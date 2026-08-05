using System.IO;
using System.Linq;
using GameConfig;
using GameLogic.Core;
using GameLogic.Core.Model;
using GameLogic.Core.View;
using Luban;
using NUnit.Framework;
using UnityEngine;

namespace GameLogic.Tests
{
    public sealed class StacklandsModelLoaderTests
    {
        private Tables _tables;

        [SetUp]
        public void SetUp()
        {
            string bytesDirectory = Path.Combine(Application.dataPath, "AssetRaw/Configs/bytes");
            _tables = new Tables(name => new ByteBuf(File.ReadAllBytes(Path.Combine(bytesDirectory, name + ".bytes"))));
        }

        [Test]
        public void Build_LoadsExpectedOriginalScope()
        {
            IStacklandsContentModel content = StacklandsModelLoader.Build(_tables);

            Assert.That(content.Cards.Count, Is.EqualTo(121));
            Assert.That(content.Cards.All.Count(card => card.Category == "IDEA"), Is.EqualTo(32));
            Assert.That(content.Cards.All.Count(card => card.Category == "RUMOR"), Is.EqualTo(2));
            Assert.That(content.Quests.Count, Is.EqualTo(56));
            Assert.That(content.Boosters.Count, Is.EqualTo(10));
            Assert.That(content.Cards.Contains("gold"), Is.True, "出售和购买使用的金币卡必须存在");
            Assert.That(content.Cards.Contains("filter_crossroads"), Is.False);
            Assert.That(content.Cards.Contains("blueprint_filter_crossroads"), Is.False);
        }

        [Test]
        public void Build_CreatesRecipeAndBoosterIndexes()
        {
            IStacklandsContentModel content = StacklandsModelLoader.Build(_tables);

            Assert.That(content.Cards.Get("berry").NameEn, Is.EqualTo("Berry"));
            Assert.That(content.Recipes.GetByResult("brick"), Is.Not.Empty);

            BoosterDefinition pack = content.Boosters.Get("humble_beginnings");
            Assert.That(pack.Slots.Count, Is.EqualTo(pack.CardCount));
            Assert.That(content.LootPools.Contains(pack.Slots[0].NormalPoolId), Is.True);
        }

        [Test]
        public void Build_IncludesMarkdownRuntimeRecipesAndSpecialOutputs()
        {
            IStacklandsContentModel content = StacklandsModelLoader.Build(_tables);

            Assert.That(content.Recipes.Count, Is.EqualTo(69));
            Assert.That(content.Recipes.Get("behavior_brickyard_brick").Results[0].CardId, Is.EqualTo("brick"));
            Assert.That(content.Recipes.Get("behavior_baby_grow").Results[0].CardId, Is.EqualTo("villager"));
            Assert.That(content.Recipes.Get("behavior_tame_wolf").Results[0].CardId, Is.EqualTo("dog"));
            Assert.That(content.Recipes.Get("behavior_create_graveyard").Results[0].CardId, Is.EqualTo("graveyard"));
            Assert.That(content.LootPools.Get("pool_treasure_chest_original").CanRoll, Is.True);
            Assert.That(content.LootPools.Get("pool_travelling_cart_original").CanRoll, Is.True);
            Assert.That(content.LootPools.Get("pool_strange_portal_enemies").CanRoll, Is.True);
            Assert.That(content.LootPools.Get("pool_rare_portal_enemies").CanRoll, Is.True);
            Assert.That(content.LootPools.Get("pool_temple_demon").Entries.Single().ResultCardId,
                Is.EqualTo("demon"));
            Assert.That(content.LootPools.Get("pool_old_tome_ideas").FallbackPoolId,
                Is.EqualTo("pool_old_tome_map"));

            var templeRequirements = content.Actions.Get("action_temple").Requirements;
            var chestRequirements = content.Actions.Get("action_treasure_chest").Requirements;
            var cartRequirements = content.Actions.Get("action_travelling_cart").Requirements;
            Assert.That(templeRequirements, Has.Count.EqualTo(1));
            Assert.That(chestRequirements, Has.Count.EqualTo(1));
            Assert.That(cartRequirements, Has.Count.EqualTo(1));

            Assert.That(content.Effects.Count, Is.EqualTo(6));
            Assert.That(content.Effects.Get("effect_cow_stun").Chance, Is.EqualTo(0.1f));
            Assert.That(content.Effects.Get("effect_demon_stun_all").Target, Is.EqualTo("ALL_ENEMIES"));
            Assert.That(content.Effects.Get("effect_frittata_well_fed").Magnitude, Is.EqualTo(2f));
            Assert.That(content.Effects.Get("effect_wicked_witch_once").Once, Is.EqualTo(OnceKind.Run));
        }

        [Test]
        public void Validate_AllPoolsAreRunnableAndReportIsClean()
        {
            ContentValidationReport report = StacklandsModelLoader.Validate(_tables);
            IStacklandsContentModel content = StacklandsModelLoader.Build(_tables);

            Assert.That(report.HasErrors, Is.False, report.ToString());
            Assert.That(report.Issues, Is.Empty, report.ToString());
            Assert.That(content.LootPools.All.All(pool => pool.CanRoll), Is.True);
            Assert.DoesNotThrow(() => content.LootPools.All.SelectMany(pool => pool.Entries)
                .Select(entry => entry.RequireWeight()).ToArray());
        }

        [Test]
        public void RequiredRuntimeValuesArePopulated()
        {
            IStacklandsContentModel content = StacklandsModelLoader.Build(_tables);

            Assert.That(content.WorldRules.RequireBaseCardCap(), Is.EqualTo(20));
            Assert.That(content.Cards.All.All(card => card.SellPrice.HasValue), Is.True);
            Assert.That(content.Cards.All.All(card => card.FoodValue.HasValue), Is.True);
            Assert.That(content.Cards.All.All(card => card.IsSellable.HasValue), Is.True);
            Assert.That(content.Cards.All.All(card => card.IsFoilEligible.HasValue), Is.True);
            Assert.That(content.Cards.All.All(card => card.IsUnique.HasValue), Is.True);
            Assert.That(content.WorldRules.MaxStackSize, Is.EqualTo(20));
            Assert.That(content.WorldRules.SecondVillagerGuaranteePack, Is.EqualTo(7));
            Assert.That(content.WorldRules.SingleVillagerPackChance, Is.EqualTo(0.5f));
            Assert.That(content.WorldRules.PortalBaseThreat, Is.EqualTo(6));
            Assert.That(content.WorldRules.PortalThreatPerInterval, Is.EqualTo(4));
            Assert.That(content.WorldRules.RarePortalMultiplier, Is.EqualTo(2f));
        }

        [Test]
        public void OriginalEnemyPool_PreservesMarkdownFallbackProbability()
        {
            IStacklandsContentModel content = StacklandsModelLoader.Build(_tables);
            LootPoolDefinition pool = content.LootPools.Get("pool_original_pack_enemies");

            Assert.That(pool.NormalizeWeights, Is.False);
            Assert.That(pool.FallbackPoolId, Is.EqualTo("pool_default_mainland"));
            Assert.That(pool.Entries, Has.Count.EqualTo(5));
            Assert.That(pool.Entries.All(entry => entry.Weight == 12.5f), Is.True);
            Assert.That(pool.Entries.Sum(entry => entry.RequireWeight()), Is.EqualTo(62.5f));
        }

        [Test]
        public void WorkingStack_PreservesProgressAndRejectsNewCards()
        {
            IStacklandsContentModel content = StacklandsModelLoader.Build(_tables);
            var run = new StacklandsRunData
            {
                RandomState = 1,
                MoonDuration = 120f,
                MoonRemaining = 120f,
                Cards =
                {
                    new CardRunData
                    {
                        InstanceId = "card_a", CardId = "wood", StackId = "stack_a", StackOrder = 0,
                    },
                    new CardRunData
                    {
                        InstanceId = "card_b", CardId = "stone", StackId = "stack_a", StackOrder = 1,
                    },
                    new CardRunData
                    {
                        InstanceId = "card_c", CardId = "berry", StackId = "stack_b", StackOrder = 0,
                        X = -3f,
                    },
                },
                Works =
                {
                    new WorkRunData
                    {
                        Id = "work_a", DefinitionId = "behavior_brickyard_brick", IsRecipe = true,
                        StackId = "stack_a", Remaining = 7f, Duration = 10f,
                        CardIds = { "card_a", "card_b" },
                    },
                },
            };
            var store = new MemorySaveStore(run);
            var cameraObject = new GameObject("Stacklands Test Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();
            var boardObject = new GameObject("Stacklands Test Board");
            StacklandsBoardView boardView = boardObject.AddComponent<StacklandsBoardView>();

            try
            {
                CoreSystem.Initialize(content, store, boardView);
                CoreSystem.SubmitCommand(new StacklandsCommandDto
                {
                    Kind = StacklandsCommandKind.MoveStack,
                    InstanceId = "card_a",
                    X = 4f,
                    Y = -2f,
                });

                Assert.That(run.Works, Has.Count.EqualTo(1));
                Assert.That(run.Works[0].Remaining, Is.EqualTo(7f));
                Assert.That(run.Works[0].StackId, Is.EqualTo("stack_a"));
                Assert.That(run.Cards.Where(card => card.InstanceId != "card_c")
                    .All(card => card.StackId == "stack_a"), Is.True);
                Assert.That(run.Cards.Where(card => card.InstanceId != "card_c")
                    .All(card => card.X == 4f && card.Y == -2f), Is.True);
                Transform[] activeProgressTracks = boardObject.GetComponentsInChildren<Transform>(true)
                    .Where(item => item.name == "ProgressTrack" && item.gameObject.activeSelf).ToArray();
                Assert.That(activeProgressTracks, Has.Length.EqualTo(1));
                Assert.That(activeProgressTracks[0].parent.name, Is.EqualTo("Card card_b"));

                CoreSystem.SubmitCommand(new StacklandsCommandDto
                {
                    Kind = StacklandsCommandKind.MoveCard,
                    InstanceId = "card_c",
                    TargetInstanceId = "card_a",
                });

                CardRunData rejectedCard = run.Cards.Single(card => card.InstanceId == "card_c");
                Assert.That(rejectedCard.StackId, Is.EqualTo("stack_b"));
                Assert.That(rejectedCard.X, Is.EqualTo(-3f));
                Assert.That(run.Works, Has.Count.EqualTo(1));
                Assert.That(run.Works[0].Remaining, Is.EqualTo(7f));
            }
            finally
            {
                CoreSystem.Release();
                Object.DestroyImmediate(boardObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void MoveBooster_UpdatesPositionWithoutOpeningPack()
        {
            IStacklandsContentModel content = StacklandsModelLoader.Build(_tables);
            var booster = new BoosterRunData
            {
                InstanceId = "pack_a",
                BoosterId = "humble_beginnings",
                X = -1f,
                Y = 2f,
                Results = { "berry", "wood" },
                Foils = { false, false },
            };
            var run = new StacklandsRunData
            {
                RandomState = 1,
                MoonDuration = 120f,
                MoonRemaining = 120f,
                Boosters = { booster },
            };
            var store = new MemorySaveStore(run);
            var cameraObject = new GameObject("Stacklands Booster Test Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();
            var boardObject = new GameObject("Stacklands Booster Test Board");
            StacklandsBoardView boardView = boardObject.AddComponent<StacklandsBoardView>();

            try
            {
                CoreSystem.Initialize(content, store, boardView);
                CoreSystem.SubmitCommand(new StacklandsCommandDto
                {
                    Kind = StacklandsCommandKind.MoveBooster,
                    InstanceId = booster.InstanceId,
                    X = 3f,
                    Y = -2f,
                });

                Assert.That(booster.X, Is.EqualTo(3f));
                Assert.That(booster.Y, Is.EqualTo(-2f));
                Assert.That(booster.Revealed, Is.Zero);
                Assert.That(booster.Results, Has.Count.EqualTo(2));
            }
            finally
            {
                CoreSystem.Release();
                Object.DestroyImmediate(boardObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void BuyBooster_ConsumesOnlyPriceFromDraggedCoinStack()
        {
            IStacklandsContentModel content = StacklandsModelLoader.Build(_tables);
            BoosterDefinition pack = content.Boosters.All.First(item =>
                item.AcquireMode == "PURCHASE" && item.PriceAmount > 0);
            const string paymentStackId = "payment_stack";
            var run = new StacklandsRunData
            {
                RandomState = 1,
                MoonDuration = 120f,
                MoonRemaining = 120f,
            };
            for (int index = 0; index < pack.PriceAmount + 2; index++)
                run.Cards.Add(new CardRunData
                {
                    InstanceId = "payment_" + index,
                    CardId = pack.PriceCardId,
                    StackId = paymentStackId,
                    StackOrder = index,
                    X = -2f,
                    Y = 1f,
                });
            run.Cards.Add(new CardRunData
            {
                InstanceId = "other_gold",
                CardId = pack.PriceCardId,
                StackId = "other_stack",
                X = 4f,
                Y = -1f,
            });
            var profile = new StacklandsProfileData
            {
                CompletedQuests = Enumerable.Range(0, pack.UnlockQuestCount)
                    .Select(index => "completed_" + index).ToList(),
            };
            var store = new MemorySaveStore(run, profile);
            var cameraObject = new GameObject("Stacklands Purchase Test Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();
            var boardObject = new GameObject("Stacklands Purchase Test Board");
            StacklandsBoardView boardView = boardObject.AddComponent<StacklandsBoardView>();

            try
            {
                CoreSystem.Initialize(content, store, boardView);
                CoreSystem.SubmitCommand(new StacklandsCommandDto
                {
                    Kind = StacklandsCommandKind.BuyBooster,
                    InstanceId = "payment_" + (pack.PriceAmount + 1),
                    ContentId = pack.Id,
                });

                CardRunData[] remainingPayment = run.Cards.Where(card => card.StackId == paymentStackId).ToArray();
                Assert.That(remainingPayment, Has.Length.EqualTo(2));
                Assert.That(remainingPayment.Select(card => card.StackOrder), Is.EqualTo(new[] { 0, 1 }));
                Assert.That(remainingPayment.All(card => card.X == -2f && card.Y == 1f), Is.True);
                Assert.That(run.Cards.Any(card => card.InstanceId == "other_gold"), Is.True,
                    "不能从未拖动的金币堆扣款");
                Assert.That(run.Boosters.Count(item => item.BoosterId == pack.Id), Is.EqualTo(1));
            }
            finally
            {
                CoreSystem.Release();
                Object.DestroyImmediate(boardObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void BuyBooster_WhenDraggedCoinStackIsShort_LeavesWholeStackInPlace()
        {
            IStacklandsContentModel content = StacklandsModelLoader.Build(_tables);
            BoosterDefinition pack = content.Boosters.All.First(item =>
                item.AcquireMode == "PURCHASE" && item.PriceAmount > 1);
            const string paymentStackId = "short_payment_stack";
            var run = new StacklandsRunData
            {
                RandomState = 1,
                MoonDuration = 120f,
                MoonRemaining = 120f,
            };
            for (int index = 0; index < pack.PriceAmount - 1; index++)
                run.Cards.Add(new CardRunData
                {
                    InstanceId = "short_payment_" + index,
                    CardId = pack.PriceCardId,
                    StackId = paymentStackId,
                    StackOrder = index,
                    X = 3f,
                    Y = 2f,
                });
            string draggedCardId = "short_payment_" + (pack.PriceAmount - 2);
            var profile = new StacklandsProfileData
            {
                CompletedQuests = Enumerable.Range(0, pack.UnlockQuestCount)
                    .Select(index => "completed_" + index).ToList(),
            };
            var store = new MemorySaveStore(run, profile);
            var cameraObject = new GameObject("Stacklands Short Purchase Test Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();
            var boardObject = new GameObject("Stacklands Short Purchase Test Board");
            StacklandsBoardView boardView = boardObject.AddComponent<StacklandsBoardView>();

            try
            {
                CoreSystem.Initialize(content, store, boardView);
                CoreSystem.SubmitCommand(new StacklandsCommandDto
                {
                    Kind = StacklandsCommandKind.BuyBooster,
                    InstanceId = draggedCardId,
                    ContentId = pack.Id,
                });

                CardRunData[] remainingPayment = run.Cards.Where(card => card.StackId == paymentStackId).ToArray();
                Assert.That(remainingPayment, Has.Length.EqualTo(pack.PriceAmount - 1));
                Assert.That(remainingPayment.All(card => card.X == 3f && card.Y == 2f), Is.True);
                Assert.That(run.Boosters.Count(item => item.BoosterId == pack.Id), Is.Zero);
            }
            finally
            {
                CoreSystem.Release();
                Object.DestroyImmediate(boardObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        private sealed class MemorySaveStore : IStacklandsSaveStore
        {
            private readonly StacklandsRunData _run;
            private readonly StacklandsProfileData _profile;

            public MemorySaveStore(StacklandsRunData run, StacklandsProfileData profile = null)
            {
                _run = run;
                _profile = profile ?? new StacklandsProfileData();
            }

            public StacklandsProfileData LoadProfile() => _profile;
            public StacklandsRunData LoadRun() => _run;
            public void SaveProfile(StacklandsProfileData profile) { }
            public void SaveRun(StacklandsRunData run) { }
            public void DeleteRun() { }
        }
    }
}
