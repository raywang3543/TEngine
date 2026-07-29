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
            CarnivalPerformerEffect effect = CarnivalPerformerEffect.Custom,
            float effectValue = 0f,
            CarnivalHandKind? handKind = null,
            CarnivalSuit? suit = null)
        {
            Id = id;
            Name = name;
            ShortName = shortName;
            Description = description;
            Cost = cost;
            Rarity = rarity;
            Effect = effect;
            EffectValue = effectValue;
            HandKind = handKind;
            Suit = suit;
        }

        public string Id { get; }
        public string Name { get; }
        public string ShortName { get; }
        public string Description { get; }
        public int Cost { get; }
        public string Rarity { get; }
        public CarnivalPerformerEffect Effect { get; }
        public float EffectValue { get; }
        public CarnivalHandKind? HandKind { get; }
        public CarnivalSuit? Suit { get; }
    }
}
