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
            CarnivalSuit? suit = null)
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
    }
}
