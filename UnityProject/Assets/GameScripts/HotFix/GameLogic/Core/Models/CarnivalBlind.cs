namespace GameLogic.Core
{
    /// <summary>
    /// 当前盲注定义。
    /// </summary>
    public sealed class CarnivalBlind
    {
        public CarnivalBlind(
            string name,
            CarnivalBlindTier tier,
            CarnivalBossRule bossRule,
            float scoreScale,
            int reward,
            string description)
        {
            Name = name;
            Tier = tier;
            BossRule = bossRule;
            ScoreScale = scoreScale;
            Reward = reward;
            Description = description;
        }

        public string Name { get; }
        public CarnivalBlindTier Tier { get; }
        public CarnivalBossRule BossRule { get; }
        public float ScoreScale { get; }
        public int Reward { get; }
        public string Description { get; }
    }
}
