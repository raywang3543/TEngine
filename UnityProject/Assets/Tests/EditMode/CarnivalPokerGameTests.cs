using GameLogic.Core;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GameLogic.Tests
{
    public sealed class CarnivalPokerGameTests
    {
        [Test]
        public void StartNewRun_CreatesSmallBlindAndStartingBuild()
        {
            CarnivalPokerGame game = CreateGame(17);

            game.StartNewRun();

            Assert.That(game.Phase, Is.EqualTo(CarnivalRunPhase.Playing));
            Assert.That(game.Ante, Is.EqualTo(1));
            Assert.That(game.CurrentBlind.Tier, Is.EqualTo(CarnivalBlindTier.Small));
            Assert.That(game.Hand, Has.Count.EqualTo(8));
            Assert.That(game.Performers, Has.Count.EqualTo(2));
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
    }
}
