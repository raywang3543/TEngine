using System.Collections.Generic;

namespace GameLogic.Core.Model
{
    /// <summary>
    /// 按稳定字符串 ID 查询的只读内容索引。
    /// </summary>
    public interface IContentIndex<out T>
    {
        int Count { get; }
        IReadOnlyCollection<T> All { get; }
        T Get(string id);
        bool Contains(string id);
    }

    public interface IRecipeIndex : IContentIndex<RecipeDefinition>
    {
        IReadOnlyList<RecipeDefinition> GetByResult(string cardId);
        IReadOnlyList<RecipeDefinition> GetByBlueprint(string blueprintId);
    }

    /// <summary>
    /// Stacklands Original 玩法规则可依赖的唯一内容边界。
    /// </summary>
    public interface IStacklandsContentModel
    {
        IContentIndex<CardDefinition> Cards { get; }
        IContentIndex<UnitDefinition> Units { get; }
        IContentIndex<EquipmentDefinition> Equipment { get; }
        IContentIndex<StructureDefinition> Structures { get; }
        IRecipeIndex Recipes { get; }
        IContentIndex<LootPoolDefinition> LootPools { get; }
        IContentIndex<CardActionDefinition> Actions { get; }
        IContentIndex<CardEffectDefinition> Effects { get; }
        IContentIndex<MilestoneDefinition> Milestones { get; }
        IContentIndex<BoosterDefinition> Boosters { get; }
        IContentIndex<QuestDefinition> Quests { get; }
        WorldRules WorldRules { get; }
        ContentValidationReport Validation { get; }
    }
}
