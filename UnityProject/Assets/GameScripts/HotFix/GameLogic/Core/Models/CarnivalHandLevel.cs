namespace GameLogic.Core
{
    /// <summary>
    /// 扑克牌型的永久等级。
    /// </summary>
    public sealed class CarnivalHandLevel
    {
        public CarnivalHandLevel(int baseChips, float baseMultiplier, int chipGrowth, float multiplierGrowth)
        {
            BaseChips = baseChips;
            BaseMultiplier = baseMultiplier;
            ChipGrowth = chipGrowth;
            MultiplierGrowth = multiplierGrowth;
            Level = 1;
        }

        public int Level { get; private set; }
        public int BaseChips { get; }
        public float BaseMultiplier { get; }
        public int ChipGrowth { get; }
        public float MultiplierGrowth { get; }
        public int Chips => BaseChips + (Level - 1) * ChipGrowth;
        public float Multiplier => BaseMultiplier + (Level - 1) * MultiplierGrowth;

        public void Upgrade()
        {
            Level++;
        }
    }
}
