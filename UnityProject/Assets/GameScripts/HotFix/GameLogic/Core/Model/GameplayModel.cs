using System.Collections.Generic;

namespace GameLogic.Core.Model
{
    public enum UnitFaction { Player, Friendly, Hostile, Boss }
    public enum AttackKind { None, Melee, Ranged, Magic }
    public enum EquipmentSlotKind { None, Hand, Head, Body }
    public enum StructureKind { Harvestable, Production, Storage, Market, Housing, Portal, Special }
    public enum CardActionKind { Harvest, Produce, Explore, Open, Sell, Breed, Grow, Summon, Trade, Spawn, Defeat }
    public enum WorkerKind { None, Worker, Adult, Human, Villager, Explorer }
    public enum OnceKind { None, Run, Profile }
    public enum QuestMetricKind
    {
        EventCount, CardCount, TotalFood, CoinCount, IdeaDiscoveredCount, ReachMoon, PackUnlocked,
        PackOpened, PackPurchased, CardObtained, CardCreated, CardKilled, LocationExplored, StateCheck,
    }
    public enum CompareKind { AtLeast, Equal }
    public enum QuestPersistenceKind { Run, Profile }
    public enum ConditionModeKind { All, Any }

    public sealed class UnitDefinition
    {
        internal UnitDefinition(string cardId, UnitFaction faction, int? foodPerMoon, int? maxHp, int? combatLevel,
            AttackKind attackType, float? attackInterval, float? hitChance, int? damageMin, int? damageMax,
            int? defense, float? critChance, float? workSpeed, float? exploreSpeed, string deathResultCardId,
            bool canEquip)
        {
            CardId = cardId; Faction = faction; FoodPerMoon = foodPerMoon; MaxHp = maxHp;
            CombatLevel = combatLevel; AttackType = attackType; AttackInterval = attackInterval;
            HitChance = hitChance; DamageMin = damageMin; DamageMax = damageMax; Defense = defense;
            CritChance = critChance; WorkSpeed = workSpeed; ExploreSpeed = exploreSpeed;
            DeathResultCardId = deathResultCardId; CanEquip = canEquip;
        }

        public string CardId { get; }
        public UnitFaction Faction { get; }
        public int? FoodPerMoon { get; }
        public int? MaxHp { get; }
        public int? CombatLevel { get; }
        public AttackKind AttackType { get; }
        public float? AttackInterval { get; }
        public float? HitChance { get; }
        public int? DamageMin { get; }
        public int? DamageMax { get; }
        public int? Defense { get; }
        public float? CritChance { get; }
        public float? WorkSpeed { get; }
        public float? ExploreSpeed { get; }
        public string DeathResultCardId { get; }
        public bool CanEquip { get; }
    }

    public sealed class EquipmentDefinition
    {
        internal EquipmentDefinition(string cardId, EquipmentSlotKind slot, AttackKind attackType,
            string professionCardId, int attackSpeedDelta, int hitDelta, int damageDelta, int defenseDelta,
            float exploreSpeedMultiplier, float workSpeedMultiplier)
        {
            CardId = cardId; Slot = slot; AttackType = attackType; ProfessionCardId = professionCardId;
            AttackSpeedDelta = attackSpeedDelta; HitDelta = hitDelta; DamageDelta = damageDelta;
            DefenseDelta = defenseDelta; ExploreSpeedMultiplier = exploreSpeedMultiplier;
            WorkSpeedMultiplier = workSpeedMultiplier;
        }

        public string CardId { get; }
        public EquipmentSlotKind Slot { get; }
        public AttackKind AttackType { get; }
        public string ProfessionCardId { get; }
        public int AttackSpeedDelta { get; }
        public int HitDelta { get; }
        public int DamageDelta { get; }
        public int DefenseDelta { get; }
        public float ExploreSpeedMultiplier { get; }
        public float WorkSpeedMultiplier { get; }
    }

    public sealed class StructureDefinition
    {
        internal StructureDefinition(string cardId, StructureKind type, int? capacity, int cardCapDelta,
            bool isInfinite, float? sellDuration, float? sellMultiplier)
        {
            CardId = cardId; Type = type; Capacity = capacity; CardCapDelta = cardCapDelta;
            IsInfinite = isInfinite; SellDuration = sellDuration; SellMultiplier = sellMultiplier;
        }
        public string CardId { get; }
        public StructureKind Type { get; }
        public int? Capacity { get; }
        public int CardCapDelta { get; }
        public bool IsInfinite { get; }
        public float? SellDuration { get; }
        public float? SellMultiplier { get; }
    }

    public sealed class CardActionDefinition
    {
        internal CardActionDefinition(string id, string sourceCardId, CardActionKind type, WorkerKind worker,
            IReadOnlyList<CardRequirementDefinition> requirements, float? duration, float? repeatInterval,
            int? maxUses, string lootPoolId, string milestoneGroupId, bool destroySource)
        {
            Id = id; SourceCardId = sourceCardId; Type = type; Worker = worker; Requirements = requirements;
            Duration = duration; RepeatInterval = repeatInterval; MaxUses = maxUses; LootPoolId = lootPoolId;
            MilestoneGroupId = milestoneGroupId; DestroySource = destroySource;
        }
        public string Id { get; }
        public string SourceCardId { get; }
        public CardActionKind Type { get; }
        public WorkerKind Worker { get; }
        public IReadOnlyList<CardRequirementDefinition> Requirements { get; }
        public float? Duration { get; }
        public float? RepeatInterval { get; }
        public int? MaxUses { get; }
        public string LootPoolId { get; }
        public string MilestoneGroupId { get; }
        public bool DestroySource { get; }
    }

    public sealed class CardEffectDefinition
    {
        internal CardEffectDefinition(string id, string sourceCardId, string trigger, string effectType,
            string target, float chance, float duration, float magnitude, int maxTriggers, OnceKind once,
            string conditionArg)
        {
            Id = id; SourceCardId = sourceCardId; Trigger = trigger; EffectType = effectType; Target = target;
            Chance = chance; Duration = duration; Magnitude = magnitude; MaxTriggers = maxTriggers;
            Once = once; ConditionArg = conditionArg;
        }
        public string Id { get; }
        public string SourceCardId { get; }
        public string Trigger { get; }
        public string EffectType { get; }
        public string Target { get; }
        public float Chance { get; }
        public float Duration { get; }
        public float Magnitude { get; }
        public int MaxTriggers { get; }
        public OnceKind Once { get; }
        public string ConditionArg { get; }
    }

    public sealed class MilestoneDefinition
    {
        internal MilestoneDefinition(string id, string groupId, int triggerCount, string outputCardId,
            int outputCount, bool replaceRandomResult, bool destroySource, OnceKind once)
        {
            Id = id; GroupId = groupId; TriggerCount = triggerCount; OutputCardId = outputCardId;
            OutputCount = outputCount; ReplaceRandomResult = replaceRandomResult;
            DestroySource = destroySource; Once = once;
        }
        public string Id { get; }
        public string GroupId { get; }
        public int TriggerCount { get; }
        public string OutputCardId { get; }
        public int OutputCount { get; }
        public bool ReplaceRandomResult { get; }
        public bool DestroySource { get; }
        public OnceKind Once { get; }
    }

    public sealed class QuestConditionDefinition
    {
        internal QuestConditionDefinition(QuestMetricKind metric, string targetId, string cardId, string packId,
            string recipeId, CompareKind compare, int threshold, QuestPersistenceKind scope)
        {
            Metric = metric; TargetId = targetId; CardId = cardId; PackId = packId; RecipeId = recipeId;
            Compare = compare; Threshold = threshold; Scope = scope;
        }
        public QuestMetricKind Metric { get; }
        public string TargetId { get; }
        public string CardId { get; }
        public string PackId { get; }
        public string RecipeId { get; }
        public CompareKind Compare { get; }
        public int Threshold { get; }
        public QuestPersistenceKind Scope { get; }
    }

    public sealed class QuestDefinition
    {
        internal QuestDefinition(string id, string series, int order, string nameEn, string nameZh,
            string descriptionEn, string descriptionZh, IReadOnlyList<QuestConditionDefinition> conditions,
            ConditionModeKind mode, QuestPersistenceKind persistence, bool isMain)
        {
            Id = id; Series = series; Order = order; NameEn = nameEn; NameZh = nameZh;
            DescriptionEn = descriptionEn; DescriptionZh = descriptionZh; Conditions = conditions;
            Mode = mode; Persistence = persistence; IsMain = isMain;
        }
        public string Id { get; }
        public string Series { get; }
        public int Order { get; }
        public string NameEn { get; }
        public string NameZh { get; }
        public string DescriptionEn { get; }
        public string DescriptionZh { get; }
        public IReadOnlyList<QuestConditionDefinition> Conditions { get; }
        public ConditionModeKind Mode { get; }
        public QuestPersistenceKind Persistence { get; }
        public bool IsMain { get; }
    }
}
