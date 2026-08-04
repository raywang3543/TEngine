using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GameLogic.Core.Model
{
    public sealed class CardDefinition
    {
        internal CardDefinition(string id, string nameEn, string nameZh, string descriptionEn, string descriptionZh,
            string category, string color, IReadOnlyList<string> tags, int? sellPrice, int? foodValue, int cardCapCost,
            bool? isSellable, bool? isFoilEligible, bool? isUnique)
        {
            Id = id; NameEn = nameEn; NameZh = nameZh; DescriptionEn = descriptionEn; DescriptionZh = descriptionZh;
            Category = category; Color = color; Tags = tags; SellPrice = sellPrice; FoodValue = foodValue;
            CardCapCost = cardCapCost; IsSellable = isSellable; IsFoilEligible = isFoilEligible; IsUnique = isUnique;
        }

        public string Id { get; }
        public string NameEn { get; }
        public string NameZh { get; }
        public string DescriptionEn { get; }
        public string DescriptionZh { get; }
        public string Category { get; }
        public string Color { get; }
        public IReadOnlyList<string> Tags { get; }
        public int? SellPrice { get; }
        public int? FoodValue { get; }
        public int CardCapCost { get; }
        public bool? IsSellable { get; }
        public bool? IsFoilEligible { get; }
        public bool? IsUnique { get; }
    }

    public sealed class CardRequirementDefinition
    {
        internal CardRequirementDefinition(string matcher, string cardId, string tag, int count, string consumeMode)
        {
            Matcher = matcher; CardId = cardId; Tag = tag; Count = count; ConsumeMode = consumeMode;
        }

        public string Matcher { get; }
        public string CardId { get; }
        public string Tag { get; }
        public int Count { get; }
        public string ConsumeMode { get; }
    }

    public sealed class CardAmountDefinition
    {
        internal CardAmountDefinition(string cardId, int count) { CardId = cardId; Count = count; }
        public string CardId { get; }
        public int Count { get; }
    }

    public sealed class RecipeDefinition
    {
        internal RecipeDefinition(string id, string blueprintId, string ideaCardId, string group, int priority,
            float? durationSeconds, IReadOnlyList<CardRequirementDefinition> requirements,
            IReadOnlyList<CardAmountDefinition> results, bool allowExtraCards)
        {
            Id = id; BlueprintId = blueprintId; IdeaCardId = ideaCardId; Group = group; Priority = priority;
            DurationSeconds = durationSeconds; Requirements = requirements; Results = results;
            AllowExtraCards = allowExtraCards;
        }

        public string Id { get; }
        public string BlueprintId { get; }
        public string IdeaCardId { get; }
        public string Group { get; }
        public int Priority { get; }
        public float? DurationSeconds { get; }
        public IReadOnlyList<CardRequirementDefinition> Requirements { get; }
        public IReadOnlyList<CardAmountDefinition> Results { get; }
        public bool AllowExtraCards { get; }
    }

    public sealed class LootEntryDefinition
    {
        internal LootEntryDefinition(string id, string poolId, string resultCardId, int minCount, int maxCount,
            float? weight, string conditionType, string conditionArg, string onceScope, int priority)
        {
            Id = id; PoolId = poolId; ResultCardId = resultCardId; MinCount = minCount; MaxCount = maxCount;
            Weight = weight; ConditionType = conditionType; ConditionArg = conditionArg; OnceScope = onceScope;
            Priority = priority;
        }

        public string Id { get; }
        public string PoolId { get; }
        public string ResultCardId { get; }
        public int MinCount { get; }
        public int MaxCount { get; }
        public float? Weight { get; }
        public string ConditionType { get; }
        public string ConditionArg { get; }
        public string OnceScope { get; }
        public int Priority { get; }

        public float RequireWeight()
        {
            if (!Weight.HasValue)
            {
                throw new ContentDataUnavailableException("TbLootEntry", Id, "weight");
            }

            return Weight.Value;
        }
    }

    public sealed class LootPoolDefinition
    {
        internal LootPoolDefinition(string id, int drawMin, int drawMax, bool normalizeWeights,
            bool withoutReplacement, string fallbackPoolId, IReadOnlyList<LootEntryDefinition> entries)
        {
            Id = id; DrawMin = drawMin; DrawMax = drawMax; NormalizeWeights = normalizeWeights;
            WithoutReplacement = withoutReplacement; FallbackPoolId = fallbackPoolId; Entries = entries;
        }

        public string Id { get; }
        public int DrawMin { get; }
        public int DrawMax { get; }
        public bool NormalizeWeights { get; }
        public bool WithoutReplacement { get; }
        public string FallbackPoolId { get; }
        public IReadOnlyList<LootEntryDefinition> Entries { get; }
        public bool CanRoll => Entries.Count > 0 && Array.TrueForAll(ToArray(Entries), entry => entry.Weight.HasValue);

        private static LootEntryDefinition[] ToArray(IReadOnlyList<LootEntryDefinition> entries)
        {
            var result = new LootEntryDefinition[entries.Count];
            for (var i = 0; i < entries.Count; i++) result[i] = entries[i];
            return result;
        }
    }

    public sealed class BoosterSlotDefinition
    {
        internal BoosterSlotDefinition(string id, int slotIndex, string ideaPoolId, string normalPoolId,
            string peacefulPoolId, string guaranteeCardId, string guaranteeCondition)
        {
            Id = id; SlotIndex = slotIndex; IdeaPoolId = ideaPoolId; NormalPoolId = normalPoolId;
            PeacefulPoolId = peacefulPoolId; GuaranteeCardId = guaranteeCardId;
            GuaranteeCondition = guaranteeCondition;
        }

        public string Id { get; }
        public int SlotIndex { get; }
        public string IdeaPoolId { get; }
        public string NormalPoolId { get; }
        public string PeacefulPoolId { get; }
        public string GuaranteeCardId { get; }
        public string GuaranteeCondition { get; }
    }

    public sealed class BoosterDefinition
    {
        internal BoosterDefinition(string id, string nameEn, string nameZh, string descriptionEn, string descriptionZh,
            string priceCardId, int priceAmount, int cardCount, string acquireMode, int unlockQuestCount,
            int purchaseThreshold, bool grantOnce, float foilChance, float foilSellMultiplier,
            IReadOnlyList<BoosterSlotDefinition> slots)
        {
            Id = id; NameEn = nameEn; NameZh = nameZh; DescriptionEn = descriptionEn; DescriptionZh = descriptionZh;
            PriceCardId = priceCardId; PriceAmount = priceAmount; CardCount = cardCount; AcquireMode = acquireMode;
            UnlockQuestCount = unlockQuestCount; PurchaseThreshold = purchaseThreshold; GrantOnce = grantOnce;
            FoilChance = foilChance; FoilSellMultiplier = foilSellMultiplier; Slots = slots;
        }

        public string Id { get; }
        public string NameEn { get; }
        public string NameZh { get; }
        public string DescriptionEn { get; }
        public string DescriptionZh { get; }
        public string PriceCardId { get; }
        public int PriceAmount { get; }
        public int CardCount { get; }
        public string AcquireMode { get; }
        public int UnlockQuestCount { get; }
        public int PurchaseThreshold { get; }
        public bool GrantOnce { get; }
        public float FoilChance { get; }
        public float FoilSellMultiplier { get; }
        public IReadOnlyList<BoosterSlotDefinition> Slots { get; }
    }

    public sealed class WorldRules
    {
        internal WorldRules(int moonShortSeconds, int moonNormalSeconds, int moonLongSeconds, int? baseCardCap,
            IReadOnlyList<string> feedingPriority, IReadOnlyList<float> speedOptions, int portalStartMoon,
            int portalInterval, int rarePortalFrequency, int portalDelay, int threatCapMoon, int cartStartMoon,
            float cartChance, int cartGuaranteeMoon, int cartPrice, int cartGobletPurchase,
            float combatAdvantageMultiplier, int maxStackSize, int secondVillagerGuaranteePack,
            float singleVillagerPackChance, int portalBaseThreat, int portalThreatPerInterval,
            float rarePortalMultiplier)
        {
            MoonShortSeconds = moonShortSeconds; MoonNormalSeconds = moonNormalSeconds; MoonLongSeconds = moonLongSeconds;
            BaseCardCap = baseCardCap; FeedingPriority = feedingPriority; SpeedOptions = speedOptions;
            PortalStartMoon = portalStartMoon; PortalInterval = portalInterval; RarePortalFrequency = rarePortalFrequency;
            PortalDelay = portalDelay; ThreatCapMoon = threatCapMoon; CartStartMoon = cartStartMoon;
            CartChance = cartChance; CartGuaranteeMoon = cartGuaranteeMoon; CartPrice = cartPrice;
            CartGobletPurchase = cartGobletPurchase; CombatAdvantageMultiplier = combatAdvantageMultiplier;
            MaxStackSize = maxStackSize; SecondVillagerGuaranteePack = secondVillagerGuaranteePack;
            SingleVillagerPackChance = singleVillagerPackChance; PortalBaseThreat = portalBaseThreat;
            PortalThreatPerInterval = portalThreatPerInterval; RarePortalMultiplier = rarePortalMultiplier;
        }

        public int MoonShortSeconds { get; }
        public int MoonNormalSeconds { get; }
        public int MoonLongSeconds { get; }
        public int? BaseCardCap { get; }
        public IReadOnlyList<string> FeedingPriority { get; }
        public IReadOnlyList<float> SpeedOptions { get; }
        public int PortalStartMoon { get; }
        public int PortalInterval { get; }
        public int RarePortalFrequency { get; }
        public int PortalDelay { get; }
        public int ThreatCapMoon { get; }
        public int CartStartMoon { get; }
        public float CartChance { get; }
        public int CartGuaranteeMoon { get; }
        public int CartPrice { get; }
        public int CartGobletPurchase { get; }
        public float CombatAdvantageMultiplier { get; }
        public int MaxStackSize { get; }
        public int SecondVillagerGuaranteePack { get; }
        public float SingleVillagerPackChance { get; }
        public int PortalBaseThreat { get; }
        public int PortalThreatPerInterval { get; }
        public float RarePortalMultiplier { get; }

        public int RequireBaseCardCap()
        {
            if (!BaseCardCap.HasValue)
                throw new ContentDataUnavailableException("TbWorldRule", "original", "base_card_cap");
            return BaseCardCap.Value;
        }
    }

    /// <summary>
    /// 不在首批解析器 API 中参与决策，但仍完成生成类型隔离的通用定义。
    /// </summary>
    public sealed class ContentRecordDefinition
    {
        internal ContentRecordDefinition(string id, IReadOnlyDictionary<string, object> values)
        {
            Id = id; Values = values;
        }
        public string Id { get; }
        public IReadOnlyDictionary<string, object> Values { get; }
    }
}
