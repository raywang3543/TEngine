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
            float breakChance,
            float heldMultiplierFactor,
            int heldMoney,
            float chanceAdditiveMultiplier,
            float additiveMultiplierChance,
            int chanceMoney,
            float moneyChance,
            bool alwaysScores,
            bool ignoresRankSuit)
        {
            Id = id;
            Name = name;
            Description = description;
            Chips = chips;
            AdditiveMultiplier = additiveMultiplier;
            MultiplierFactor = multiplierFactor;
            BreakChance = breakChance;
            HeldMultiplierFactor = heldMultiplierFactor;
            HeldMoney = heldMoney;
            ChanceAdditiveMultiplier = chanceAdditiveMultiplier;
            AdditiveMultiplierChance = additiveMultiplierChance;
            ChanceMoney = chanceMoney;
            MoneyChance = moneyChance;
            AlwaysScores = alwaysScores;
            IgnoresRankSuit = ignoresRankSuit;
        }

        public CarnivalCardEnhancement Id { get; }
        public string Name { get; }
        public string Description { get; }
        public int Chips { get; }
        public float AdditiveMultiplier { get; }
        public float MultiplierFactor { get; }
        public float BreakChance { get; }
        public float HeldMultiplierFactor { get; }
        public int HeldMoney { get; }
        public float ChanceAdditiveMultiplier { get; }
        public float AdditiveMultiplierChance { get; }
        public int ChanceMoney { get; }
        public float MoneyChance { get; }
        public bool AlwaysScores { get; }
        public bool IgnoresRankSuit { get; }
    }
}
