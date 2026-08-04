using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GameLogic.Core.Model
{
    internal class ContentIndex<T> : IContentIndex<T>
    {
        private readonly IReadOnlyDictionary<string, T> _items;
        private readonly IReadOnlyCollection<T> _all;

        internal ContentIndex(IDictionary<string, T> items)
        {
            var copy = new Dictionary<string, T>(items, StringComparer.Ordinal);
            _items = new ReadOnlyDictionary<string, T>(copy);
            _all = new ReadOnlyCollection<T>(copy.Values.ToList());
        }

        public int Count => _items.Count;
        public IReadOnlyCollection<T> All => _all;

        public T Get(string id)
        {
            if (!_items.TryGetValue(id, out var value))
                throw new KeyNotFoundException($"内容 ID 不存在：{id}");
            return value;
        }

        public bool Contains(string id) => _items.ContainsKey(id);
    }

    internal sealed class RecipeIndex : ContentIndex<RecipeDefinition>, IRecipeIndex
    {
        private readonly IReadOnlyDictionary<string, IReadOnlyList<RecipeDefinition>> _byResult;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<RecipeDefinition>> _byBlueprint;

        internal RecipeIndex(IDictionary<string, RecipeDefinition> recipes) : base(recipes)
        {
            _byResult = Group(recipes.Values.SelectMany(recipe =>
                recipe.Results.Select(result => new KeyValuePair<string, RecipeDefinition>(result.CardId, recipe))));
            _byBlueprint = Group(recipes.Values.Select(recipe =>
                new KeyValuePair<string, RecipeDefinition>(recipe.BlueprintId, recipe)));
        }

        public IReadOnlyList<RecipeDefinition> GetByResult(string cardId) => GetGroup(_byResult, cardId);
        public IReadOnlyList<RecipeDefinition> GetByBlueprint(string blueprintId) => GetGroup(_byBlueprint, blueprintId);

        private static IReadOnlyDictionary<string, IReadOnlyList<RecipeDefinition>> Group(
            IEnumerable<KeyValuePair<string, RecipeDefinition>> source)
        {
            return new ReadOnlyDictionary<string, IReadOnlyList<RecipeDefinition>>(source
                .GroupBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key,
                    group => (IReadOnlyList<RecipeDefinition>)new ReadOnlyCollection<RecipeDefinition>(
                        group.Select(pair => pair.Value).OrderByDescending(recipe => recipe.Priority).ToList()),
                    StringComparer.Ordinal));
        }

        private static IReadOnlyList<RecipeDefinition> GetGroup(
            IReadOnlyDictionary<string, IReadOnlyList<RecipeDefinition>> index, string key)
        {
            return index.TryGetValue(key, out var values) ? values : Array.Empty<RecipeDefinition>();
        }
    }

    internal sealed class StacklandsContentModel : IStacklandsContentModel
    {
        internal StacklandsContentModel(IContentIndex<CardDefinition> cards,
            IContentIndex<UnitDefinition> units, IContentIndex<EquipmentDefinition> equipment,
            IContentIndex<StructureDefinition> structures, IRecipeIndex recipes,
            IContentIndex<LootPoolDefinition> lootPools, IContentIndex<CardActionDefinition> actions,
            IContentIndex<CardEffectDefinition> effects, IContentIndex<MilestoneDefinition> milestones,
            IContentIndex<BoosterDefinition> boosters,
            IContentIndex<QuestDefinition> quests, WorldRules worldRules, ContentValidationReport validation)
        {
            Cards = cards; Units = units; Equipment = equipment; Structures = structures; Recipes = recipes;
            LootPools = lootPools; Actions = actions; Effects = effects; Milestones = milestones;
            Boosters = boosters; Quests = quests;
            WorldRules = worldRules; Validation = validation;
        }

        public IContentIndex<CardDefinition> Cards { get; }
        public IContentIndex<UnitDefinition> Units { get; }
        public IContentIndex<EquipmentDefinition> Equipment { get; }
        public IContentIndex<StructureDefinition> Structures { get; }
        public IRecipeIndex Recipes { get; }
        public IContentIndex<LootPoolDefinition> LootPools { get; }
        public IContentIndex<CardActionDefinition> Actions { get; }
        public IContentIndex<CardEffectDefinition> Effects { get; }
        public IContentIndex<MilestoneDefinition> Milestones { get; }
        public IContentIndex<BoosterDefinition> Boosters { get; }
        public IContentIndex<QuestDefinition> Quests { get; }
        public WorldRules WorldRules { get; }
        public ContentValidationReport Validation { get; }
    }
}
