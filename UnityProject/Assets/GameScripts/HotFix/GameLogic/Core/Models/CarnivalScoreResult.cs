using System.Collections.Generic;

namespace GameLogic.Core
{
    /// <summary>
    /// 单次计分结果。
    /// </summary>
    public sealed class CarnivalScoreResult
    {
        public CarnivalHandKind Kind { get; set; }
        public string HandName { get; set; }
        public int Chips { get; set; }
        public float Multiplier { get; set; }
        public int Score { get; set; }
        public List<int> ScoringCardIds { get; } = new List<int>();
        public List<string> Breakdown { get; } = new List<string>();
    }
}
