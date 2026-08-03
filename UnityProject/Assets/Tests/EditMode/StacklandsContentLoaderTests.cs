using System.IO;
using System.Linq;
using GameConfig;
using GameLogic.Core.Content;
using Luban;
using NUnit.Framework;
using UnityEngine;

namespace GameLogic.Tests
{
    public sealed class StacklandsContentLoaderTests
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
            IStacklandsContentCatalog content = StacklandsContentLoader.Build(_tables);

            Assert.That(content.Cards.Count, Is.EqualTo(121));
            Assert.That(content.Cards.All.Count(card => card.Category == "IDEA"), Is.EqualTo(32));
            Assert.That(content.Cards.All.Count(card => card.Category == "RUMOR"), Is.EqualTo(2));
            Assert.That(content.Quests.Count, Is.EqualTo(56));
            Assert.That(content.Boosters.Count, Is.EqualTo(10));
            Assert.That(content.Cards.Contains("filter_crossroads"), Is.False);
            Assert.That(content.Cards.Contains("blueprint_filter_crossroads"), Is.False);
        }

        [Test]
        public void Build_CreatesRecipeAndBoosterIndexes()
        {
            IStacklandsContentCatalog content = StacklandsContentLoader.Build(_tables);

            Assert.That(content.Cards.Get("berry").NameEn, Is.EqualTo("Berry"));
            Assert.That(content.Recipes.GetByResult("brick"), Is.Not.Empty);

            BoosterDefinition pack = content.Boosters.Get("humble_beginnings");
            Assert.That(pack.Slots.Count, Is.EqualTo(pack.CardCount));
            Assert.That(content.LootPools.Contains(pack.Slots[0].NormalPoolId), Is.True);
        }

        [Test]
        public void Build_IncludesMarkdownRuntimeRecipesAndSpecialOutputs()
        {
            IStacklandsContentCatalog content = StacklandsContentLoader.Build(_tables);

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

            var templeRequirements = (object[])content.Actions.Get("action_temple").Values["requirements"];
            var chestRequirements = (object[])content.Actions.Get("action_treasure_chest").Values["requirements"];
            var cartRequirements = (object[])content.Actions.Get("action_travelling_cart").Values["requirements"];
            Assert.That(templeRequirements, Has.Length.EqualTo(1));
            Assert.That(chestRequirements, Has.Length.EqualTo(1));
            Assert.That(cartRequirements, Has.Length.EqualTo(1));

            Assert.That(content.Effects.Count, Is.EqualTo(6));
            Assert.That(content.Effects.Get("effect_cow_stun").Values["chance"], Is.EqualTo(0.1f));
            Assert.That(content.Effects.Get("effect_demon_stun_all").Values["target"], Is.EqualTo("ALL_ENEMIES"));
            Assert.That(content.Effects.Get("effect_frittata_well_fed").Values["magnitude"], Is.EqualTo(2f));
            Assert.That(content.Effects.Get("effect_wicked_witch_once").Values["once_scope"], Is.EqualTo("RUN"));
        }

        [Test]
        public void Validate_AllPoolsAreRunnableAndReportIsClean()
        {
            ContentValidationReport report = StacklandsContentLoader.Validate(_tables);
            IStacklandsContentCatalog content = StacklandsContentLoader.Build(_tables);

            Assert.That(report.HasErrors, Is.False, report.ToString());
            Assert.That(report.Issues, Is.Empty, report.ToString());
            Assert.That(content.LootPools.All.All(pool => pool.CanRoll), Is.True);
            Assert.DoesNotThrow(() => content.LootPools.All.SelectMany(pool => pool.Entries)
                .Select(entry => entry.RequireWeight()).ToArray());
        }

        [Test]
        public void RequiredRuntimeValuesArePopulated()
        {
            IStacklandsContentCatalog content = StacklandsContentLoader.Build(_tables);

            Assert.That(content.WorldRules.RequireBaseCardCap(), Is.EqualTo(20));
            Assert.That(content.Cards.All.All(card => card.SellPrice.HasValue), Is.True);
            Assert.That(content.Cards.All.All(card => card.FoodValue.HasValue), Is.True);
            Assert.That(content.Cards.All.All(card => card.IsSellable.HasValue), Is.True);
            Assert.That(content.Cards.All.All(card => card.IsFoilEligible.HasValue), Is.True);
            Assert.That(content.Cards.All.All(card => card.IsUnique.HasValue), Is.True);
        }
    }
}
