using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GameConfig;
using Config = GameConfig.stacklands;

namespace GameLogic.Core.Model
{
    /// <summary>
    /// 将 Luban 生成对象转换为 Core 可用的只读 Original 内容目录。
    /// </summary>
    public static class StacklandsModelLoader
    {
        public static ContentValidationReport Validate(Tables tables)
        {
            if (tables == null) throw new ArgumentNullException(nameof(tables));
            return Create(tables).Validation;
        }

        public static IStacklandsContentModel Build(Tables tables)
        {
            if (tables == null) throw new ArgumentNullException(nameof(tables));
            var catalog = Create(tables);
            if (catalog.Validation.HasErrors)
                throw new InvalidOperationException("Stacklands Original 配置校验失败：\n" + catalog.Validation);
            return catalog;
        }

        private static StacklandsContentModel Create(Tables tables)
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

            ValidateReferences(tables, cards, recipes, pools, boosters, report);
            ValidateLootPools(pools.Values, report);
            ValidateLootPoolCycles(pools, report);
            ValidateRecipeConflicts(recipes.Values, report);
            ValidateActionMilestones(tables, report);

            var world = tables.TbWorldRule.GetOrDefault("original");
            if (world == null)
            {
                report.Error("TbWorldRule", "original", "缺少运行时世界规则");
                world = tables.TbWorldRule.DataList.FirstOrDefault();
            }

            if (world == null)
                throw new InvalidOperationException("TbWorldRule 为空，无法构造运行时目录。");

            ValidateWorldRules(world, report);

            return new StacklandsContentModel(
                new ContentIndex<CardDefinition>(cards),
                new ContentIndex<UnitDefinition>(ConvertUnits(tables)),
                new ContentIndex<EquipmentDefinition>(ConvertEquipment(tables)),
                new ContentIndex<StructureDefinition>(ConvertStructures(tables)),
                new RecipeIndex(recipes),
                new ContentIndex<LootPoolDefinition>(pools),
                new ContentIndex<CardActionDefinition>(ConvertActions(tables)),
                new ContentIndex<CardEffectDefinition>(ConvertEffects(tables)),
                new ContentIndex<MilestoneDefinition>(ConvertMilestones(tables)),
                new ContentIndex<BoosterDefinition>(boosters),
                new ContentIndex<QuestDefinition>(ConvertQuests(tables)),
                ConvertWorldRules(world), report);
        }

        private static CardDefinition ConvertCard(Config.Card card)
        {
            return new CardDefinition(card.Id, card.NameEn, card.NameZh, card.DescriptionEn, card.DescriptionZh,
                card.Category.ToString(), card.Color.ToString(), ReadOnly(card.Tags.Select(tag => tag.ToString())),
                card.SellPrice, card.FoodValue, card.CardCapCost, card.IsSellable, card.IsFoilEligible, card.IsUnique);
        }

        private static RecipeDefinition ConvertRecipe(Config.Recipe recipe)
        {
            var requirements = recipe.Requirements.Select(requirement => new CardRequirementDefinition(
                requirement.Matcher.ToString(), requirement.CardId, requirement.Tag, requirement.Count,
                requirement.ConsumeMode.ToString()));
            var results = recipe.Results.Select(result => new CardAmountDefinition(result.CardId, result.Count));
            return new RecipeDefinition(recipe.Id, recipe.BlueprintId, recipe.IdeaCardId, recipe.Group.ToString(),
                recipe.Priority, recipe.DurationSeconds, ReadOnly(requirements), ReadOnly(results),
                recipe.AllowExtraCards);
        }

        private static LootEntryDefinition ConvertLootEntry(Config.LootEntry entry)
        {
            return new LootEntryDefinition(entry.Id, entry.PoolId, entry.ResultCardId, entry.MinCount, entry.MaxCount,
                entry.Weight, entry.ConditionType.ToString(), entry.ConditionArg, entry.OnceScope.ToString(),
                entry.Priority);
        }

        private static LootPoolDefinition ConvertLootPool(Config.LootPool pool,
            IReadOnlyDictionary<string, IReadOnlyList<LootEntryDefinition>> entriesByPool)
        {
            if (!entriesByPool.TryGetValue(pool.Id, out var entries)) entries = Array.Empty<LootEntryDefinition>();
            return new LootPoolDefinition(pool.Id, pool.DrawMin, pool.DrawMax, pool.NormalizeWeights,
                pool.WithoutReplacement, pool.FallbackPoolId, entries);
        }

        private static BoosterSlotDefinition ConvertBoosterSlot(Config.BoosterSlot slot)
        {
            return new BoosterSlotDefinition(slot.Id, slot.SlotIndex, slot.IdeaPoolId, slot.NormalPoolId,
                slot.PeacefulPoolId, slot.GuaranteeCardId, slot.GuaranteeCondition);
        }

        private static BoosterDefinition ConvertBooster(Config.BoosterPack pack,
            IReadOnlyDictionary<string, IReadOnlyList<BoosterSlotDefinition>> slotsByPack)
        {
            if (!slotsByPack.TryGetValue(pack.Id, out var slots)) slots = Array.Empty<BoosterSlotDefinition>();
            return new BoosterDefinition(pack.Id, pack.NameEn, pack.NameZh, pack.DescriptionEn, pack.DescriptionZh,
                pack.PriceCardId, pack.PriceAmount, pack.CardCount, pack.AcquireMode.ToString(), pack.UnlockQuestCount,
                pack.PurchaseThreshold, pack.GrantOnce, pack.FoilChance, pack.FoilSellMultiplier, slots);
        }

        private static WorldRules ConvertWorldRules(Config.WorldRule world)
        {
            return new WorldRules(world.MoonShortSeconds, world.MoonNormalSeconds, world.MoonLongSeconds,
                world.BaseCardCap, ReadOnly(world.FeedingPriority), ReadOnly(world.SpeedOptions), world.PortalStartMoon,
                world.PortalInterval, world.RarePortalFrequency, world.PortalDelay, world.ThreatCapMoon,
                world.CartStartMoon, world.CartChance, world.CartGuaranteeMoon, world.CartPrice,
                world.CartGobletPurchase, world.CombatAdvantageMultiplier, world.MaxStackSize,
                world.SecondVillagerGuaranteePack, world.SingleVillagerPackChance, world.PortalBaseThreat,
                world.PortalThreatPerInterval, world.RarePortalMultiplier);
        }

        private static Dictionary<string, UnitDefinition> ConvertUnits(Tables tables)
        {
            return tables.TbUnit.DataList.ToDictionary(unit => unit.CardId, unit => new UnitDefinition(unit.CardId,
                ParseEnum<UnitFaction>(unit.Faction.ToString()), unit.FoodPerMoon, unit.MaxHp, unit.CombatLevel,
                ParseEnum<AttackKind>(unit.AttackType.ToString()), unit.AttackInterval, unit.HitChance,
                unit.DamageMin, unit.DamageMax, unit.Defense, unit.CritChance, unit.WorkSpeed, unit.ExploreSpeed,
                unit.DeathResultCardId),
                StringComparer.Ordinal);
        }

        private static Dictionary<string, EquipmentDefinition> ConvertEquipment(Tables tables)
        {
            return tables.TbEquipment.DataList.ToDictionary(item => item.CardId, item => new EquipmentDefinition(
                item.CardId, ParseEnum<EquipmentSlotKind>(item.Slot.ToString()),
                ParseEnum<AttackKind>(item.AttackType.ToString()), item.ProfessionCardId, item.AttackSpeedDelta,
                item.HitDelta, item.DamageDelta, item.DefenseDelta, item.ExploreSpeedMultiplier,
                item.WorkSpeedMultiplier), StringComparer.Ordinal);
        }

        private static Dictionary<string, StructureDefinition> ConvertStructures(Tables tables)
        {
            return tables.TbStructure.DataList.ToDictionary(item => item.CardId, item => new StructureDefinition(
                item.CardId, ParseEnum<StructureKind>(item.StructureType.ToString()), item.Capacity,
                item.CardCapDelta, item.IsInfinite, item.SellDuration, item.SellMultiplier), StringComparer.Ordinal);
        }

        private static Dictionary<string, CardActionDefinition> ConvertActions(Tables tables)
        {
            return tables.TbCardAction.DataList.ToDictionary(item => item.Id, item => new CardActionDefinition(item.Id,
                item.SourceCardId, ParseEnum<CardActionKind>(item.ActionType.ToString()),
                ParseEnum<WorkerKind>(item.WorkerRequirement.ToString()), ReadOnly(item.Requirements.Select(
                    requirement => new CardRequirementDefinition(requirement.Matcher.ToString(), requirement.CardId,
                        requirement.Tag, requirement.Count, requirement.ConsumeMode.ToString()))),
                item.DurationSeconds, item.RepeatInterval, item.MaxUses, item.LootPoolId, item.MilestoneGroupId,
                item.DestroySourceOnComplete), StringComparer.Ordinal);
        }

        private static Dictionary<string, MilestoneDefinition> ConvertMilestones(Tables tables)
        {
            return tables.TbMilestone.DataList.ToDictionary(item => item.Id, item => new MilestoneDefinition(item.Id,
                item.GroupId, item.TriggerCount, item.OutputCardId, item.OutputCount, item.ReplaceRandomResult,
                item.DestroySource, ParseEnum<OnceKind>(item.OnceScope.ToString())), StringComparer.Ordinal);
        }

        private static Dictionary<string, CardEffectDefinition> ConvertEffects(Tables tables)
        {
            return tables.TbCardEffect.DataList.ToDictionary(item => item.Id, item => new CardEffectDefinition(item.Id,
                item.SourceCardId, item.Trigger, item.EffectType, item.Target, item.Chance, item.DurationSeconds,
                item.Magnitude, item.MaxTriggers, ParseEnum<OnceKind>(item.OnceScope.ToString()), item.ConditionArg),
                StringComparer.Ordinal);
        }

        private static Dictionary<string, QuestDefinition> ConvertQuests(Tables tables)
        {
            return tables.TbQuest.DataList.ToDictionary(item => item.Id, item => new QuestDefinition(item.Id,
                item.Series.ToString(), item.Order, item.NameEn, item.NameZh, item.DescriptionEn, item.DescriptionZh,
                ReadOnly(item.Conditions.Select(condition => new QuestConditionDefinition(
                    ParseEnum<QuestMetricKind>(condition.Metric.ToString()), condition.TargetId,
                    condition.TargetCardId, condition.TargetPackId, condition.TargetRecipeId,
                    ParseEnum<CompareKind>(condition.Compare.ToString()), condition.Threshold,
                    ParseEnum<QuestPersistenceKind>(condition.Scope.ToString())))),
                ParseEnum<ConditionModeKind>(item.ConditionMode.ToString()),
                ParseEnum<QuestPersistenceKind>(item.Persistence.ToString()), item.IsMain), StringComparer.Ordinal);
        }

        private static void ValidateReferences(Tables tables, IReadOnlyDictionary<string, CardDefinition> cards,
            IReadOnlyDictionary<string, RecipeDefinition> recipes, IReadOnlyDictionary<string, LootPoolDefinition> pools,
            IReadOnlyDictionary<string, BoosterDefinition> boosters, ContentValidationReport report)
        {
            foreach (var unit in tables.TbUnit.DataList)
            {
                CheckCard(unit.CardId, "TbUnit", unit.CardId, cards, report);
                if (!string.IsNullOrEmpty(unit.DeathResultCardId))
                    CheckCard(unit.DeathResultCardId, "TbUnit", unit.CardId, cards, report);
            }

            foreach (var equipment in tables.TbEquipment.DataList)
            {
                CheckCard(equipment.CardId, "TbEquipment", equipment.CardId, cards, report);
                CheckCard(equipment.ProfessionCardId, "TbEquipment", equipment.CardId, cards, report);
            }

            foreach (var structure in tables.TbStructure.DataList)
            {
                CheckCard(structure.CardId, "TbStructure", structure.CardId, cards, report);
            }

            foreach (var recipe in recipes.Values)
            {
                if (!string.IsNullOrEmpty(recipe.IdeaCardId))
                    CheckCard(recipe.IdeaCardId, "TbRecipe", recipe.Id, cards, report);
                foreach (var requirement in recipe.Requirements)
                    if (!string.IsNullOrEmpty(requirement.CardId))
                        CheckCard(requirement.CardId, "TbRecipe", recipe.Id, cards, report);
                foreach (var result in recipe.Results)
                    CheckCard(result.CardId, "TbRecipe", recipe.Id, cards, report);
            }

            foreach (var pool in pools.Values)
            {
                if (!string.IsNullOrEmpty(pool.FallbackPoolId) && !pools.ContainsKey(pool.FallbackPoolId))
                    report.Error("TbLootPool", pool.Id, $"后备池不存在：{pool.FallbackPoolId}");
                foreach (var entry in pool.Entries)
                    CheckCard(entry.ResultCardId, "TbLootEntry", entry.Id, cards, report);
            }

            foreach (var pack in boosters.Values)
            {
                if (pack.Slots.Count != pack.CardCount)
                    report.Error("TbBoosterPack", pack.Id, $"card_count={pack.CardCount}，但卡槽数={pack.Slots.Count}");
                if (!string.IsNullOrEmpty(pack.PriceCardId))
                    CheckCard(pack.PriceCardId, "TbBoosterPack", pack.Id, cards, report);
                foreach (var slot in pack.Slots)
                {
                    CheckPool(slot.NormalPoolId, slot.Id, pools, report);
                    if (!string.IsNullOrEmpty(slot.IdeaPoolId)) CheckPool(slot.IdeaPoolId, slot.Id, pools, report);
                    if (!string.IsNullOrEmpty(slot.PeacefulPoolId))
                        CheckPool(slot.PeacefulPoolId, slot.Id, pools, report);
                    if (!string.IsNullOrEmpty(slot.GuaranteeCardId))
                        CheckCard(slot.GuaranteeCardId, "TbBoosterSlot", slot.Id, cards, report);
                }
            }

            foreach (var action in tables.TbCardAction.DataList)
            {
                CheckCard(action.SourceCardId, "TbCardAction", action.Id, cards, report);
                foreach (var requirement in action.Requirements)
                    if (!string.IsNullOrEmpty(requirement.CardId))
                        CheckCard(requirement.CardId, "TbCardAction", action.Id, cards, report);
                if (!string.IsNullOrEmpty(action.LootPoolId)) CheckPool(action.LootPoolId, action.Id, pools, report);
            }

            foreach (var effect in tables.TbCardEffect.DataList)
            {
                CheckCard(effect.SourceCardId, "TbCardEffect", effect.Id, cards, report);
                if (effect.Chance < 0 || effect.Chance > 1)
                    report.Error("TbCardEffect", effect.Id, $"触发概率必须在 0 到 1 之间：{effect.Chance}");
                if (effect.MaxTriggers == 0 || effect.MaxTriggers < -1)
                    report.Error("TbCardEffect", effect.Id, $"max_triggers 必须为 -1 或正整数：{effect.MaxTriggers}");
            }

            foreach (var milestone in tables.TbMilestone.DataList)
            {
                if (!string.IsNullOrEmpty(milestone.OutputCardId))
                    CheckCard(milestone.OutputCardId, "TbMilestone", milestone.Id, cards, report);
            }

            foreach (var quest in tables.TbQuest.DataList)
            {
                foreach (var condition in quest.Conditions)
                {
                    if (!string.IsNullOrEmpty(condition.TargetCardId))
                        CheckCard(condition.TargetCardId, "TbQuest", quest.Id, cards, report);
                    if (!string.IsNullOrEmpty(condition.TargetPackId) && !boosters.ContainsKey(condition.TargetPackId))
                        report.Error("TbQuest", quest.Id, $"引用的卡包不存在：{condition.TargetPackId}");
                    if (!string.IsNullOrEmpty(condition.TargetRecipeId) && !recipes.ContainsKey(condition.TargetRecipeId))
                        report.Error("TbQuest", quest.Id, $"引用的配方不存在：{condition.TargetRecipeId}");
                }
            }
        }

        private static void ValidateLootPools(IEnumerable<LootPoolDefinition> pools, ContentValidationReport report)
        {
            foreach (var pool in pools)
            {
                if (pool.DrawMin < 0 || pool.DrawMax < pool.DrawMin)
                    report.Error("TbLootPool", pool.Id, "抽取数量范围非法");
                if (pool.Entries.Count == 0)
                    report.Error("TbLootPool", pool.Id, "掉落池没有条目");
                foreach (var entry in pool.Entries.Where(entry => !entry.Weight.HasValue))
                    report.Warning("TbLootEntry", entry.Id, "缺少 weight；该池不可执行随机抽取");
            }
        }

        private static void ValidateLootPoolCycles(IReadOnlyDictionary<string, LootPoolDefinition> pools,
            ContentValidationReport report)
        {
            foreach (LootPoolDefinition start in pools.Values)
            {
                var visited = new HashSet<string>(StringComparer.Ordinal);
                LootPoolDefinition current = start;
                while (current != null && !string.IsNullOrEmpty(current.FallbackPoolId))
                {
                    if (!visited.Add(current.Id))
                    {
                        report.Error("TbLootPool", start.Id, "掉落池后备链形成循环");
                        break;
                    }
                    pools.TryGetValue(current.FallbackPoolId, out current);
                }
            }
        }

        private static void ValidateRecipeConflicts(IEnumerable<RecipeDefinition> recipes,
            ContentValidationReport report)
        {
            foreach (var group in recipes.GroupBy(recipe => recipe.Priority + ":" + string.Join("|",
                         recipe.Requirements.OrderBy(item => item.Matcher).ThenBy(item => item.CardId)
                             .ThenBy(item => item.Tag).Select(item =>
                                 $"{item.Matcher}:{item.CardId}:{item.Tag}:{item.Count}"))))
            {
                if (group.Count() <= 1) continue;
                string outputs = string.Join(";", group.Select(recipe => recipe.Id));
                report.Error("TbRecipe", group.First().Id, "相同输入和优先级存在冲突：" + outputs);
            }
        }

        private static void ValidateActionMilestones(Tables tables, ContentValidationReport report)
        {
            var groups = new HashSet<string>(tables.TbMilestone.DataList.Select(item => item.GroupId),
                StringComparer.Ordinal);
            foreach (var action in tables.TbCardAction.DataList.Where(item =>
                         !string.IsNullOrEmpty(item.MilestoneGroupId) && !groups.Contains(item.MilestoneGroupId)))
                report.Error("TbCardAction", action.Id, "引用的里程碑组不存在：" + action.MilestoneGroupId);
        }

        private static void ValidateWorldRules(Config.WorldRule world, ContentValidationReport report)
        {
            void Positive(string field, float value)
            {
                if (value <= 0) report.Error("TbWorldRule", world.Id, field + " 必须大于 0");
            }

            Positive("max_stack_size", world.MaxStackSize);
            Positive("second_villager_guarantee_pack", world.SecondVillagerGuaranteePack);
            Positive("portal_base_threat", world.PortalBaseThreat);
            Positive("portal_threat_per_interval", world.PortalThreatPerInterval);
            Positive("rare_portal_multiplier", world.RarePortalMultiplier);
            if (world.SingleVillagerPackChance < 0f || world.SingleVillagerPackChance > 1f)
                report.Error("TbWorldRule", world.Id, "single_villager_pack_chance 必须在 0 到 1 之间");
        }

        private static void CheckCard(string cardId, string table, string rowId,
            IReadOnlyDictionary<string, CardDefinition> cards, ContentValidationReport report)
        {
            if (!cards.ContainsKey(cardId)) report.Error(table, rowId, $"引用的卡牌不存在：{cardId}");
        }

        private static void CheckPool(string poolId, string rowId,
            IReadOnlyDictionary<string, LootPoolDefinition> pools, ContentValidationReport report)
        {
            if (!pools.ContainsKey(poolId))
                report.Error("TbBoosterSlot", rowId, $"引用的掉落池不存在：{poolId}");
        }

        private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values)
        {
            return new ReadOnlyCollection<T>(values.ToList());
        }

        private static T ParseEnum<T>(string value) where T : struct
        {
            string normalized = value.Replace("_", string.Empty);
            foreach (string name in Enum.GetNames(typeof(T)))
                if (string.Equals(name, normalized, StringComparison.OrdinalIgnoreCase))
                    return (T)Enum.Parse(typeof(T), name);
            throw new InvalidOperationException($"无法把枚举值 {value} 转换为 {typeof(T).Name}");
        }
    }
}
