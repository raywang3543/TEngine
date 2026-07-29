using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 游戏逻辑跨模块事件定义。
    /// </summary>
    public static class EventDefine
    {
        #region Carnival Commands

        public static readonly int CarnivalRequestState =
            RuntimeId.ToRuntimeId("EventDefine.CarnivalRequestState");
        public static readonly int CarnivalStartNewRun =
            RuntimeId.ToRuntimeId("EventDefine.CarnivalStartNewRun");
        public static readonly int CarnivalToggleCard =
            RuntimeId.ToRuntimeId("EventDefine.CarnivalToggleCard");
        public static readonly int CarnivalPlaySelected =
            RuntimeId.ToRuntimeId("EventDefine.CarnivalPlaySelected");
        public static readonly int CarnivalDiscardSelected =
            RuntimeId.ToRuntimeId("EventDefine.CarnivalDiscardSelected");
        public static readonly int CarnivalBuyPerformer =
            RuntimeId.ToRuntimeId("EventDefine.CarnivalBuyPerformer");
        public static readonly int CarnivalBuyConsumable =
            RuntimeId.ToRuntimeId("EventDefine.CarnivalBuyConsumable");
        public static readonly int CarnivalUseConsumable =
            RuntimeId.ToRuntimeId("EventDefine.CarnivalUseConsumable");
        public static readonly int CarnivalContinueFromShop =
            RuntimeId.ToRuntimeId("EventDefine.CarnivalContinueFromShop");
        public static readonly int CarnivalSkipBlind =
            RuntimeId.ToRuntimeId("EventDefine.CarnivalSkipBlind");
        public static readonly int CarnivalSortHandByRank =
            RuntimeId.ToRuntimeId("EventDefine.CarnivalSortHandByRank");
        public static readonly int CarnivalSortHandBySuit =
            RuntimeId.ToRuntimeId("EventDefine.CarnivalSortHandBySuit");

        #endregion

        #region Carnival State

        public static readonly int CarnivalStateChanged =
            RuntimeId.ToRuntimeId("EventDefine.CarnivalStateChanged");

        #endregion

        #region Login UI

        public static readonly int ShowLoginUI =
            RuntimeId.ToRuntimeId("EventDefine.ShowLoginUI");
        public static readonly int CloseLoginUI =
            RuntimeId.ToRuntimeId("EventDefine.CloseLoginUI");

        #endregion
    }
}
