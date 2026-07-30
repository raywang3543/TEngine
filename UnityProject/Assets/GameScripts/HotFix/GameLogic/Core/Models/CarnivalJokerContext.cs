using System.Collections.Generic;

namespace GameLogic.Core
{
    /// <summary>
    /// 一次小丑牌触发所需的只读规则上下文。
    /// </summary>
    public sealed class CarnivalJokerContext
    {
        public CarnivalJokerTrigger Trigger { get; set; }
        public CarnivalScoreResult ScoreResult { get; set; }
        public IReadOnlyList<CarnivalCard> PlayedCards { get; set; }
        public CarnivalCard? CurrentCard { get; set; }
        public CarnivalCard? DestroyedCard { get; set; }
        public CarnivalDestroyReason DestroyReason { get; set; }
        public CarnivalConsumableState Consumable { get; set; }
        public CarnivalPerformer SoldJoker { get; set; }
        public CarnivalSoldCardKind SoldCardKind { get; set; }
        public CarnivalHandKind? HandKind { get; set; }
        public bool TriggeredBossRule { get; set; }
        public bool IsFirstDiscard { get; set; }
        public bool IsCopiedEffect { get; internal set; }
        public CarnivalPerformer CopiedBy { get; internal set; }
    }
}
