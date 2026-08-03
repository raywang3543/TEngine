using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GameConfig;
using Config = GameConfig.stacklands;

namespace GameLogic.Core.Content
{
    /// <summary>
    /// 将 Luban 生成对象转换为 Core 可用的只读 Original 内容目录。
    /// </summary>
    public static class StacklandsContentLoader
    {
        public static ContentValidationReport Validate(Tables tables)
        {
            if (tables == null) throw new ArgumentNullException(nameof(tables));
            return Create(tables).Validation;
        }

        public static IStacklandsContentCatalog Build(Tables tables)
        {
            if (tables == null) throw new ArgumentNullException(nameof(tables));
            var catalog = Create(tables);
            if (catalog.Validation.HasErrors)
                throw new InvalidOperationException("Stacklands Original 配置校验失败：\n" + catalog.Validation);
            return catalog;
        }

        private static StacklandsContentCatalog Create(Tables tables)
        {
            var report = new ContentValidationReport();
            var cards = tables.TbCard.DataList.ToDictionary(card => card.Id, ConvertCard, StringComparer.Ordinal);
            var recipes = tables.TbRecipe.DataList.ToDictionary(recipe => recipe.Id, ConvertRecipe, StringComparer.Ordinal);
            var entriesByPool = tables.TbLootEntry.DataList
                .Select(ConvertLootEntry)
                .GroupBy(entry => entry.PoolId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key,
                    group => (IReadOnlyList<LootEntryDefinition>)new ReadOnlyCollection<LootEntryDefinition>(
                        group.OrderByDescending(entry => entry.Priority).ToList()), StringComparer.Ordinal);
            var pools = tables.TbLootPool.DataList.ToDictionary(pool => pool.Id,
                pool => ConvertLootPool(pool, entriesByPool), StringComparer.Ordinal);
            var slotsByPack = tables.TbBoosterSlot.DataList
                .GroupBy(slot => slot.PackId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key,
                    group => (IReadOnlyList<BoosterSlotDefinition>)new ReadOnlyCollection<BoosterSlotDefinition>(
                        group.OrderBy(slot => slot.SlotIndex).Select(ConvertBoosterSlot).ToList()), StringComparer.Ordinal);
            var boosters = tables.TbBoosterPack.DataList.ToDictionary(pack => pack.Id,
                pack => ConvertBooster(pack, slotsByPack), StringComparer.Ordinal);

            ValidateCounts(tables, cards, boosters, report);
            ValidateReferences(tables, cards, recipes, pools, boosters, report);
            ValidateLootPools(pools.Values, report);

            var world = tables.TbWorldRule.GetOrDefault("original");
            if (world == null)
            {
                report.Error("TbWorldRule", "original", "缺少 Original 世界规则", string.Empty);
                world = tables.TbWorldRule.DataList.FirstOrDefault();
            }

            if (world == null)
                throw new InvalidOperationException("TbWorldRule 为空，无法构造运行时目录。");

            return new StacklandsContentCatalog(
                new ContentIndex<CardDefinition>(cards),
                new ContentIndex<ContentRecordDefinition>(ConvertUnits(tables)),
                new ContentIndex<ContentRecordDefinition>(ConvertEquipment(tables)),
                new ContentIndex<ContentRecordDefinition>(ConvertStructures(tables)),
                new RecipeIndex(recipes),
                new ContentIndex<LootPoolDefinition>(pools),
                new ContentIndex<ContentRecordDefinition>(ConvertActions(tables)),
                new ContentIndex<ContentRecordDefinition>(ConvertEffects(tables)),
                new ContentIndex<ContentRecordDefinition>(ConvertMilestones(tables)),
                new ContentIndex<BoosterDefinition>(boosters),
                new ContentIndex<ContentRecordDefinition>(ConvertQuests(tables)),
                ConvertWorldRules(world), report);
        }

        private static CardDefinition ConvertCard(Config.Card card)
        {
            return new CardDefinition(card.Id, card.NameEn, card.NameZh, card.DescriptionEn, card.DescriptionZh,
                card.Category.ToString(), card.Color.ToString(), ReadOnly(card.Tags.Select(tag => tag.ToString())),
                card.SellPrice, card.FoodValue, card.CardCapCost, card.IsSellable, card.IsFoilEligible, card.IsUnique,
                Source(card.VerifyStatus, card.SourceUrl, card.SourceRevision, card.SourceNote));
        }

        private static RecipeDefinition ConvertRecipe(Config.Recipe recipe)
        {
            var requirements = recipe.Requirements.Select(requirement => new CardRequirementDefinition(
                requirement.Matcher.ToString(), requirement.CardId, requirement.Tag, requirement.Count,
                requirement.ConsumeMode.ToString()));
            var results = recipe.Results.Select(result => new CardAmountDefinition(result.CardId, result.Count));
            return new RecipeDefinition(recipe.Id, recipe.BlueprintId, recipe.IdeaCardId, recipe.Group.ToString(),
                recipe.Priority, recipe.DurationSeconds, ReadOnly(requirements), ReadOnly(results),
                recipe.AllowExtraCards, Source(recipe.VerifyStatus, recipe.SourceUrl, recipe.SourceRevision,
                    recipe.SourceNote));
        }

        private static LootEntryDefinition ConvertLootEntry(Config.LootEntry entry)
        {
            return new LootEntryDefinition(entry.Id, entry.PoolId, entry.ResultCardId, entry.MinCount, entry.MaxCount,
                entry.Weight, entry.ConditionType.ToString(), entry.ConditionArg, entry.OnceScope.ToString(),
                entry.Priority, Source(entry.VerifyStatus, entry.SourceUrl, entry.SourceRevision, entry.SourceNote));
        }

        private static LootPoolDefinition ConvertLootPool(Config.LootPool pool,
            IReadOnlyDictionary<string, IReadOnlyList<LootEntryDefinition>> entriesByPool)
        {
            if (!entriesByPool.TryGetValue(pool.Id, out var entries)) entries = Array.Empty<LootEntryDefinition>();
            return new LootPoolDefinition(pool.Id, pool.DrawMin, pool.DrawMax, pool.NormalizeWeights,
                pool.WithoutReplacement, pool.FallbackPoolId, entries,
                Source(pool.VerifyStatus, pool.SourceUrl, pool.SourceRevision, pool.SourceNote));
        }

        private static BoosterSlotDefinition ConvertBoosterSlot(Config.BoosterSlot slot)
        {
            return new BoosterSlotDefinition(slot.Id, slot.SlotIndex, slot.IdeaPoolId, slot.NormalPoolId,
                slot.PeacefulPoolId, slot.GuaranteeCardId, slot.GuaranteeCondition,
                Source(slot.VerifyStatus, slot.SourceUrl, slot.SourceRevision, slot.SourceNote));
        }

        private static BoosterDefinition ConvertBooster(Config.BoosterPack pack,
            IReadOnlyDictionary<string, IReadOnlyList<BoosterSlotDefinition>> slotsByPack)
        {
            if (!slotsByPack.TryGetValue(pack.Id, out var slots)) slots = Array.Empty<BoosterSlotDefinition>();
            return new BoosterDefinition(pack.Id, pack.NameEn, pack.NameZh, pack.DescriptionEn, pack.DescriptionZh,
                pack.PriceCardId, pack.PriceAmount, pack.CardCount, pack.AcquireMode.ToString(), pack.UnlockQuestCount,
                pack.PurchaseThreshold, pack.GrantOnce, pack.FoilChance, pack.FoilSellMultiplier, slots,
                Source(pack.VerifyStatus, pack.SourceUrl, pack.SourceRevision, pack.SourceNote));
        }

        private static WorldRules ConvertWorldRules(Config.WorldRule world)
        {
            return new WorldRules(world.MoonShortSeconds, world.MoonNormalSeconds, world.MoonLongSeconds,
                world.BaseCardCap, ReadOnly(world.FeedingPriority), ReadOnly(world.SpeedOptions), world.PortalStartMoon,
                world.PortalInterval, world.RarePortalFrequency, world.PortalDelay, world.ThreatCapMoon,
                world.CartStartMoon, world.CartChance, world.CartGuaranteeMoon, world.CartPrice,
                world.CartGobletPurchase, world.CombatAdvantageMultiplier,
                Source(world.VerifyStatus, world.SourceUrl, world.SourceRevision, world.SourceNote));
        }

        private static Dictionary<string, ContentRecordDefinition> ConvertUnits(Tables tables)
        {
            return tables.TbUnit.DataList.ToDictionary(unit => unit.CardId, unit => Record(unit.CardId,
                Source(unit.VerifyStatus, unit.SourceUrl, unit.SourceRevision, unit.SourceNote),
                ("faction", unit.Faction.ToString()), ("food_per_moon", unit.FoodPerMoon), ("max_hp", unit.MaxHp),
                ("combat_level", unit.CombatLevel), ("attack_type", unit.AttackType.ToString()),
                ("attack_interval", unit.AttackInterval), ("hit_chance", unit.HitChance),
                ("damage_min", unit.DamageMin), ("damage_max", unit.DamageMax), ("defense", unit.Defense),
                ("crit_chance", unit.CritChance), ("work_speed", unit.WorkSpeed),
                ("explore_speed", unit.ExploreSpeed), ("death_result_card_id", unit.DeathResultCardId)),
                StringComparer.Ordinal);
        }

        private static Dictionary<string, ContentRecordDefinition> ConvertEquipment(Tables tables)
        {
            return tables.TbEquipment.DataList.ToDictionary(item => item.CardId, item => Record(item.CardId,
                Source(item.VerifyStatus, item.SourceUrl, item.SourceRevision, item.SourceNote),
                ("slot", item.Slot.ToString()), ("attack_type", item.AttackType.ToString()),
                ("profession_card_id", item.ProfessionCardId), ("attack_speed_delta", item.AttackSpeedDelta),
                ("hit_delta", item.HitDelta), ("damage_delta", item.DamageDelta),
                ("defense_delta", item.DefenseDelta), ("explore_speed_multiplier", item.ExploreSpeedMultiplier),
                ("work_speed_multiplier", item.WorkSpeedMultiplier)), StringComparer.Ordinal);
        }

        private static Dictionary<string, ContentRecordDefinition> ConvertStructures(Tables tables)
        {
            return tables.TbStructure.DataList.ToDictionary(item => item.CardId, item => Record(item.CardId,
                Source(item.VerifyStatus, item.SourceUrl, item.SourceRevision, item.SourceNote),
                ("structure_type", item.StructureType.ToString()), ("capacity", item.Capacity),
                ("card_cap_delta", item.CardCapDelta), ("is_infinite", item.IsInfinite),
                ("sell_duration", item.SellDuration), ("sell_multiplier", item.SellMultiplier)),
                StringComparer.Ordinal);
        }

        private static Dictionary<string, ContentRecordDefinition> ConvertActions(Tables tables)
        {
            return tables.TbCardAction.DataList.ToDictionary(item => item.Id, item => Record(item.Id,
                Source(item.VerifyStatus, item.SourceUrl, item.SourceRevision, item.SourceNote),
                ("source_card_id", item.SourceCardId), ("action_type", item.ActionType.ToString()),
                ("worker_requirement", item.WorkerRequirement.ToString()), ("duration_seconds", item.DurationSeconds),
                ("requirements", item.Requirements.Select(requirement => new Dictionary<string, object>
                {
                    ["matcher"] = requirement.Matcher.ToString(), ["card_id"] = requirement.CardId,
                    ["tag"] = requirement.Tag, ["count"] = requirement.Count,
                    ["consume_mode"] = requirement.ConsumeMode.ToString(),
                }).ToArray()),
                ("repeat_interval", item.RepeatInterval), ("max_uses", item.MaxUses),
                ("loot_pool_id", item.LootPoolId), ("milestone_group_id", item.MilestoneGroupId),
                ("destroy_source_on_complete", item.DestroySourceOnComplete)), StringComparer.Ordinal);
        }

        private static Dictionary<string, ContentRecordDefinition> ConvertMilestones(Tables tables)
        {
            return tables.TbMilestone.DataList.ToDictionary(item => item.Id, item => Record(item.Id,
                Source(item.VerifyStatus, item.SourceUrl, item.SourceRevision, item.SourceNote),
                ("group_id", item.GroupId), ("trigger_count", item.TriggerCount),
                ("output_card_id", item.OutputCardId), ("output_count", item.OutputCount),
                ("replace_random_result", item.ReplaceRandomResult), ("destroy_source", item.DestroySource),
                ("once_scope", item.OnceScope.ToString())), StringComparer.Ordinal);
        }

        private static Dictionary<string, ContentRecordDefinition> ConvertEffects(Tables tables)
        {
            return tables.TbCardEffect.DataList.ToDictionary(item => item.Id, item => Record(item.Id,
                Source(item.VerifyStatus, item.SourceUrl, item.SourceRevision, item.SourceNote),
                ("source_card_id", item.SourceCardId), ("trigger", item.Trigger),
                ("effect_type", item.EffectType), ("target", item.Target), ("chance", item.Chance),
                ("duration_seconds", item.DurationSeconds), ("magnitude", item.Magnitude),
                ("max_triggers", item.MaxTriggers), ("once_scope", item.OnceScope.ToString()),
                ("condition_arg", item.ConditionArg)), StringComparer.Ordinal);
        }

        private static Dictionary<string, ContentRecordDefinition> ConvertQuests(Tables tables)
        {
            return tables.TbQuest.DataList.ToDictionary(item => item.Id, item => Record(item.Id,
                Source(item.VerifyStatus, item.SourceUrl, item.SourceRevision, item.SourceNote),
                ("series", item.Series.ToString()), ("order", item.Order), ("name_en", item.NameEn),
                ("name_zh", item.NameZh), ("description_en", item.DescriptionEn),
                ("description_zh", item.DescriptionZh), ("condition_mode", item.ConditionMode.ToString()),
                ("persistence", item.Persistence.ToString()), ("is_main", item.IsMain),
                ("conditions", item.Conditions.Select(condition => new Dictionary<string, object>
                {
                    ["metric"] = condition.Metric.ToString(), ["target_id"] = condition.TargetId,
                    ["target_card_id"] = condition.TargetCardId, ["target_pack_id"] = condition.TargetPackId,
                    ["target_recipe_id"] = condition.TargetRecipeId, ["compare"] = condition.Compare.ToString(),
                    ["threshold"] = condition.Threshold, ["scope"] = condition.Scope.ToString(),
                }).ToArray())), StringComparer.Ordinal);
        }

        private static ContentRecordDefinition Record(string id, ContentSource source,
            params (string Key, object Value)[] values)
        {
            return new ContentRecordDefinition(id,
                new ReadOnlyDictionary<string, object>(values.ToDictionary(pair => pair.Key, pair => pair.Value)),
                source);
        }

        private static void ValidateCounts(Tables tables, IReadOnlyDictionary<string, CardDefinition> cards,
            IReadOnlyDictionary<string, BoosterDefinition> boosters, ContentValidationReport report)
        {
            RequireCount("TbCard", cards.Count, 121, report);
            RequireCount("TbCard/Idea", cards.Values.Count(card => card.Category == "IDEA"), 32, report);
            RequireCount("TbCard/Rumor", cards.Values.Count(card => card.Category == "RUMOR"), 2, report);
            RequireCount("TbQuest", tables.TbQuest.DataList.Count, 56, report);
            RequireCount("TbBoosterPack", boosters.Count, 10, report);
            foreach (var excluded in new[] { "filter_crossroads", "blueprint_filter_crossroads" })
                if (cards.ContainsKey(excluded)) report.Error("TbCard", excluded, "Original 范围明确排除此卡", string.Empty);
        }

        private static void RequireCount(string table, int actual, int expected, ContentValidationReport report)
        {
            if (actual != expected) report.Error(table, "*", $"期望 {expected} 行，实际 {actual} 行", string.Empty);
        }

        private static void ValidateReferences(Tables tables, IReadOnlyDictionary<string, CardDefinition> cards,
            IReadOnlyDictionary<string, RecipeDefinition> recipes, IReadOnlyDictionary<string, LootPoolDefinition> pools,
            IReadOnlyDictionary<string, BoosterDefinition> boosters, ContentValidationReport report)
        {
            foreach (var unit in tables.TbUnit.DataList)
            {
                var source = Source(unit.VerifyStatus, unit.SourceUrl, unit.SourceRevision, unit.SourceNote);
                CheckCard(unit.CardId, "TbUnit", unit.CardId, source, cards, report);
                if (!string.IsNullOrEmpty(unit.DeathResultCardId))
                    CheckCard(unit.DeathResultCardId, "TbUnit", unit.CardId, source, cards, report);
            }

            foreach (var equipment in tables.TbEquipment.DataList)
            {
                var source = Source(equipment.VerifyStatus, equipment.SourceUrl, equipment.SourceRevision,
                    equipment.SourceNote);
                CheckCard(equipment.CardId, "TbEquipment", equipment.CardId, source, cards, report);
                CheckCard(equipment.ProfessionCardId, "TbEquipment", equipment.CardId, source, cards, report);
            }

            foreach (var structure in tables.TbStructure.DataList)
            {
                var source = Source(structure.VerifyStatus, structure.SourceUrl, structure.SourceRevision,
                    structure.SourceNote);
                CheckCard(structure.CardId, "TbStructure", structure.CardId, source, cards, report);
            }

            foreach (var recipe in recipes.Values)
            {
                if (!string.IsNullOrEmpty(recipe.IdeaCardId))
                    CheckCard(recipe.IdeaCardId, "TbRecipe", recipe.Id, recipe.Source, cards, report);
                foreach (var requirement in recipe.Requirements)
                    if (!string.IsNullOrEmpty(requirement.CardId))
                        CheckCard(requirement.CardId, "TbRecipe", recipe.Id, recipe.Source, cards, report);
                foreach (var result in recipe.Results)
                    CheckCard(result.CardId, "TbRecipe", recipe.Id, recipe.Source, cards, report);
            }

            foreach (var pool in pools.Values)
            {
                if (!string.IsNullOrEmpty(pool.FallbackPoolId) && !pools.ContainsKey(pool.FallbackPoolId))
                    report.Error("TbLootPool", pool.Id, $"后备池不存在：{pool.FallbackPoolId}", pool.Source.Url);
                foreach (var entry in pool.Entries)
                    CheckCard(entry.ResultCardId, "TbLootEntry", entry.Id, entry.Source, cards, report);
            }

            foreach (var pack in boosters.Values)
            {
                if (pack.Slots.Count != pack.CardCount)
                    report.Error("TbBoosterPack", pack.Id, $"card_count={pack.CardCount}，但卡槽数={pack.Slots.Count}", pack.Source.Url);
                if (!string.IsNullOrEmpty(pack.PriceCardId)) CheckCard(pack.PriceCardId, "TbBoosterPack", pack.Id, pack.Source, cards, report);
                foreach (var slot in pack.Slots)
                {
                    CheckPool(slot.NormalPoolId, slot.Id, slot.Source, pools, report);
                    if (!string.IsNullOrEmpty(slot.IdeaPoolId)) CheckPool(slot.IdeaPoolId, slot.Id, slot.Source, pools, report);
                    if (!string.IsNullOrEmpty(slot.PeacefulPoolId)) CheckPool(slot.PeacefulPoolId, slot.Id, slot.Source, pools, report);
                    if (!string.IsNullOrEmpty(slot.GuaranteeCardId)) CheckCard(slot.GuaranteeCardId, "TbBoosterSlot", slot.Id, slot.Source, cards, report);
                }
            }

            foreach (var action in tables.TbCardAction.DataList)
            {
                var source = Source(action.VerifyStatus, action.SourceUrl, action.SourceRevision, action.SourceNote);
                CheckCard(action.SourceCardId, "TbCardAction", action.Id, source, cards, report);
                foreach (var requirement in action.Requirements)
                    if (!string.IsNullOrEmpty(requirement.CardId))
                        CheckCard(requirement.CardId, "TbCardAction", action.Id, source, cards, report);
                if (!string.IsNullOrEmpty(action.LootPoolId)) CheckPool(action.LootPoolId, action.Id, source, pools, report);
            }

            foreach (var effect in tables.TbCardEffect.DataList)
            {
                var source = Source(effect.VerifyStatus, effect.SourceUrl, effect.SourceRevision, effect.SourceNote);
                CheckCard(effect.SourceCardId, "TbCardEffect", effect.Id, source, cards, report);
                if (effect.Chance < 0 || effect.Chance > 1)
                    report.Error("TbCardEffect", effect.Id, $"触发概率必须在 0 到 1 之间：{effect.Chance}", source.Url);
                if (effect.MaxTriggers == 0 || effect.MaxTriggers < -1)
                    report.Error("TbCardEffect", effect.Id, $"max_triggers 必须为 -1 或正整数：{effect.MaxTriggers}", source.Url);
            }

            foreach (var milestone in tables.TbMilestone.DataList)
            {
                var source = Source(milestone.VerifyStatus, milestone.SourceUrl, milestone.SourceRevision,
                    milestone.SourceNote);
                if (!string.IsNullOrEmpty(milestone.OutputCardId))
                    CheckCard(milestone.OutputCardId, "TbMilestone", milestone.Id, source, cards, report);
            }

            foreach (var quest in tables.TbQuest.DataList)
            {
                var source = Source(quest.VerifyStatus, quest.SourceUrl, quest.SourceRevision, quest.SourceNote);
                foreach (var condition in quest.Conditions)
                {
                    if (!string.IsNullOrEmpty(condition.TargetCardId))
                        CheckCard(condition.TargetCardId, "TbQuest", quest.Id, source, cards, report);
                    if (!string.IsNullOrEmpty(condition.TargetPackId) && !boosters.ContainsKey(condition.TargetPackId))
                        report.Error("TbQuest", quest.Id, $"引用的卡包不存在：{condition.TargetPackId}", source.Url);
                    if (!string.IsNullOrEmpty(condition.TargetRecipeId) && !recipes.ContainsKey(condition.TargetRecipeId))
                        report.Error("TbQuest", quest.Id, $"引用的配方不存在：{condition.TargetRecipeId}", source.Url);
                }
            }
        }

        private static void ValidateLootPools(IEnumerable<LootPoolDefinition> pools, ContentValidationReport report)
        {
            foreach (var pool in pools)
            {
                if (pool.DrawMin < 0 || pool.DrawMax < pool.DrawMin)
                    report.Error("TbLootPool", pool.Id, "抽取数量范围非法", pool.Source.Url);
                if (pool.Entries.Count == 0)
                    report.Error("TbLootPool", pool.Id, "掉落池没有条目", pool.Source.Url);
                foreach (var entry in pool.Entries.Where(entry => !entry.Weight.HasValue))
                    report.Warning("TbLootEntry", entry.Id, "weight 未验证；该池不可执行随机抽取", entry.Source.Url);
            }
        }

        private static void CheckCard(string cardId, string table, string rowId, ContentSource source,
            IReadOnlyDictionary<string, CardDefinition> cards, ContentValidationReport report)
        {
            if (!cards.ContainsKey(cardId)) report.Error(table, rowId, $"引用的卡牌不存在：{cardId}", source.Url);
        }

        private static void CheckPool(string poolId, string rowId, ContentSource source,
            IReadOnlyDictionary<string, LootPoolDefinition> pools, ContentValidationReport report)
        {
            if (!pools.ContainsKey(poolId)) report.Error("TbBoosterSlot", rowId, $"引用的掉落池不存在：{poolId}", source.Url);
        }

        private static ContentSource Source(Config.EVerifyStatus status, string url, string revision, string note)
        {
            VerificationStatus runtimeStatus;
            switch (status)
            {
                case Config.EVerifyStatus.VERIFIED: runtimeStatus = VerificationStatus.Verified; break;
                case Config.EVerifyStatus.PARTIAL: runtimeStatus = VerificationStatus.Partial; break;
                case Config.EVerifyStatus.CONFLICT: runtimeStatus = VerificationStatus.Conflict; break;
                default: runtimeStatus = VerificationStatus.Unverified; break;
            }
            return new ContentSource(runtimeStatus, url, revision, note);
        }

        private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values)
        {
            return new ReadOnlyCollection<T>(values.ToList());
        }
    }
}
