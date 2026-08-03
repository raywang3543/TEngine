using System.Collections.Generic;

namespace GameLogic.Core.Content
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
    public interface IStacklandsContentCatalog
    {
        IContentIndex<CardDefinition> Cards { get; }
        IContentIndex<ContentRecordDefinition> Units { get; }
        IContentIndex<ContentRecordDefinition> Equipment { get; }
        IContentIndex<ContentRecordDefinition> Structures { get; }
        IRecipeIndex Recipes { get; }
        IContentIndex<LootPoolDefinition> LootPools { get; }
        IContentIndex<ContentRecordDefinition> Actions { get; }
        IContentIndex<ContentRecordDefinition> Effects { get; }
        IContentIndex<ContentRecordDefinition> Milestones { get; }
        IContentIndex<BoosterDefinition> Boosters { get; }
        IContentIndex<ContentRecordDefinition> Quests { get; }
        WorldRules WorldRules { get; }
        ContentValidationReport Validation { get; }
    }
}
