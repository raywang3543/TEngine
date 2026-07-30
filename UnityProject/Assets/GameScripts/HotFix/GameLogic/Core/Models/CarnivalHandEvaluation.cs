using System.Collections.Generic;

namespace GameLogic.Core
{
    /// <summary>
    /// 牌型与实际计分牌集合。
    /// </summary>
    public readonly struct CarnivalHandEvaluation
    {
        public CarnivalHandEvaluation(CarnivalHandKind kind, IReadOnlyList<int> scoringCardIds)
        {
            Kind = kind;
            ScoringCardIds = scoringCardIds;
        }

        public CarnivalHandKind Kind { get; }
        public IReadOnlyList<int> ScoringCardIds { get; }
    }
}
