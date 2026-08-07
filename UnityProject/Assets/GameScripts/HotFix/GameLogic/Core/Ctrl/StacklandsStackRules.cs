using System;
using System.Collections.Generic;
using System.Linq;
using GameLogic.Core.Model;

namespace GameLogic.Core.Ctrl
{
    /// <summary>
    /// 卡牌是否允许堆叠的成对判定（对齐原作：同类归并、村民互叠、装备互叠、装备佩戴、
    /// 敌我接触、配方/动作交互），与容量限制、敌对单位不可拖动相互独立。
    /// 配方/动作的需求匹配与工人匹配也以这里为唯一实现，供 WorkCtrl 复用。
    /// </summary>
    internal static class StacklandsStackRules
    {
        /// <summary>整堆移动时，每张被拖卡都必须与落点命中的目标卡兼容。</summary>
        internal static bool CanStackOn(IStacklandsContentModel content, List<CardRunData> moving,
            CardRunData target) => moving.All(card => IsPairCompatible(content, card, target));

        internal static bool IsPairCompatible(IStacklandsContentModel content, CardRunData a, CardRunData b)
        {
            // 完全相同类型：同类归并。
            if (a.CardId == b.CardId) return true;

            bool aUnit = content.Units.Contains(a.CardId);
            bool bUnit = content.Units.Contains(b.CardId);
            if (aUnit && bUnit)
            {
                bool aHostile = IsHostile(content, a.CardId);
                bool bHostile = IsHostile(content, b.CardId);
                // 敌我单位接触：允许堆叠并触发战斗。
                if (aHostile != bHostile) return true;
                // 双方都是村民类（category VILLAGER，含职业、Dog、Baby）；敌对单位之间不合并。
                return !aHostile && IsVillagerClass(content, a.CardId) && IsVillagerClass(content, b.CardId);
            }

            bool aEquipment = content.Equipment.Contains(a.CardId);
            bool bEquipment = content.Equipment.Contains(b.CardId);
            // 双方都是装备类。
            if (aEquipment && bEquipment) return true;
            // 装备与可佩戴单位：佩戴交互。
            if (aEquipment && bUnit && content.Units.Get(b.CardId).CanEquip) return true;
            if (bEquipment && aUnit && content.Units.Get(a.CardId).CanEquip) return true;

            // 存在配方/动作交互关系：两卡可参与同一配方，或一方为动作来源、另一方匹配其原料或工人要求。
            return HasRecipeRelation(content, a, b) || HasActionRelation(content, a, b) ||
                   HasActionRelation(content, b, a);
        }

        internal static bool IsHostile(IStacklandsContentModel content, string cardId)
        {
            if (!content.Units.Contains(cardId)) return false;
            UnitFaction faction = content.Units.Get(cardId).Faction;
            return faction == UnitFaction.Hostile || faction == UnitFaction.Boss;
        }

        private static bool IsVillagerClass(IStacklandsContentModel content, string cardId) =>
            string.Equals(content.Cards.Get(cardId).Category, "VILLAGER", StringComparison.Ordinal);

        private static bool HasRecipeRelation(IStacklandsContentModel content, CardRunData a, CardRunData b)
            => content.Recipes.All.Any(recipe => Participates(content, a, recipe.Requirements) &&
                                                 Participates(content, b, recipe.Requirements));

        private static bool HasActionRelation(IStacklandsContentModel content, CardRunData card, CardRunData source)
            => content.Actions.All.Any(action => action.Type != CardActionKind.Defeat &&
                action.SourceCardId == source.CardId &&
                (Participates(content, card, action.Requirements) ||
                 action.Worker != WorkerKind.None && WorkerMatches(content, card, action.Worker)));

        private static bool Participates(IStacklandsContentModel content, CardRunData card,
            IReadOnlyList<CardRequirementDefinition> requirements)
            => requirements.Any(requirement => RequirementMatches(content, card, requirement));

        internal static bool RequirementMatches(IStacklandsContentModel content, CardRunData card,
            CardRequirementDefinition requirement)
        {
            if (requirement.Matcher == "EXACT") return card.CardId == requirement.CardId;
            CardDefinition definition = content.Cards.Get(card.CardId);
            if (requirement.Matcher == "CATEGORY")
                return string.Equals(definition.Category, requirement.Tag, StringComparison.OrdinalIgnoreCase);
            return definition.Tags.Any(tag => string.Equals(tag, requirement.Tag, StringComparison.OrdinalIgnoreCase));
        }

        internal static bool WorkerMatches(IStacklandsContentModel content, CardRunData card, WorkerKind kind)
        {
            CardDefinition definition = content.Cards.Get(card.CardId);
            string tag = kind.ToString().ToUpperInvariant();
            return definition.Tags.Contains(tag) || kind == WorkerKind.Worker && definition.Tags.Contains("WORKER");
        }
    }
}
