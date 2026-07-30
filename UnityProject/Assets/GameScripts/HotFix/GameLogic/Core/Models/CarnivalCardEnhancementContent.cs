namespace GameLogic.Core
{
    /// <summary>
    /// 扑克牌强化的配置内容。
    /// </summary>
    public sealed class CarnivalCardEnhancementContent
    {
        public CarnivalCardEnhancementContent(
            CarnivalCardEnhancement id,
            string name,
            string description,
            int chips,
            float additiveMultiplier,
            float multiplierFactor,
            float breakChance)
        {
            Id = id;
            Name = name;
            Description = description;
            Chips = chips;
            AdditiveMultiplier = additiveMultiplier;
            MultiplierFactor = multiplierFactor;
            BreakChance = breakChance;
        }

        public CarnivalCardEnhancement Id { get; }
        public string Name { get; }
        public string Description { get; }
        public int Chips { get; }
        public float AdditiveMultiplier { get; }
        public float MultiplierFactor { get; }
        public float BreakChance { get; }
    }
}
