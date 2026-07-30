namespace GameLogic.Core
{
    /// <summary>
    /// 原创表演者卡定义。
    /// </summary>
    public sealed class CarnivalPerformer
    {
        public CarnivalPerformer(
            string id,
            string name,
            string shortName,
            string description,
            int cost,
            string rarity,
            string icon,
            bool isStarting,
            CarnivalPerformerEffect effect = CarnivalPerformerEffect.Custom,
            float effectValue = 0f,
            int conditionValue = 0,
            CarnivalHandKind? handKind = null,
            CarnivalSuit? suit = null,
            int originalOrder = 0,
            string nameEn = "",
            string descriptionEn = "",
            string unlockRequirement = "",
            bool unlockedByDefault = true,
            string activation = "",
            string jokerType = "",
            bool blueprintCompatible = true,
            bool perishableCompatible = true,
            bool eternalCompatible = true,
            string parameters = "")
        {
            Id = id;
            Name = name;
            ShortName = shortName;
            Description = description;
            Cost = cost;
            Rarity = rarity;
            Icon = icon;
            IsStarting = isStarting;
            Effect = effect;
            EffectValue = effectValue;
            ConditionValue = conditionValue;
            HandKind = handKind;
            Suit = suit;
            OriginalOrder = originalOrder;
            NameEn = nameEn;
            DescriptionEn = descriptionEn;
            UnlockRequirement = unlockRequirement;
            UnlockedByDefault = unlockedByDefault;
            Activation = activation;
            JokerType = jokerType;
            BlueprintCompatible = blueprintCompatible;
            PerishableCompatible = perishableCompatible;
            EternalCompatible = eternalCompatible;
            Parameters = parameters;
        }

        public string Id { get; }
        public string Name { get; }
        public string ShortName { get; }
        public string Description { get; }
        public int Cost { get; }
        public string Rarity { get; }
        public string Icon { get; }
        public bool IsStarting { get; }
        public CarnivalPerformerEffect Effect { get; }
        public float EffectValue { get; }
        public int ConditionValue { get; }
        public CarnivalHandKind? HandKind { get; }
        public CarnivalSuit? Suit { get; }
        public int OriginalOrder { get; }
        public string NameEn { get; }
        public string DescriptionEn { get; }
        public string UnlockRequirement { get; }
        public bool UnlockedByDefault { get; }
        public string Activation { get; }
        public string JokerType { get; }
        public bool BlueprintCompatible { get; }
        public bool PerishableCompatible { get; }
        public bool EternalCompatible { get; }
        public string Parameters { get; }

        public CarnivalPerformer CreateRuntimeCopy()
        {
            return new CarnivalPerformer(
                Id,
                Name,
                ShortName,
                Description,
                Cost,
                Rarity,
                Icon,
                IsStarting,
                Effect,
                EffectValue,
                ConditionValue,
                HandKind,
                Suit,
                OriginalOrder,
                NameEn,
                DescriptionEn,
                UnlockRequirement,
                UnlockedByDefault,
                Activation,
                JokerType,
                BlueprintCompatible,
                PerishableCompatible,
                EternalCompatible,
                Parameters);
        }
    }
}
