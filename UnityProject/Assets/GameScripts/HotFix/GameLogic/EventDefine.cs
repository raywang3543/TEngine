using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 游戏逻辑跨模块事件定义。
    /// </summary>
    public static class EventDefine
    {
        #region Stacklands Original

        public static readonly int StacklandsCommand =
            RuntimeId.ToRuntimeId("EventDefine.StacklandsCommand");
        public static readonly int StacklandsBoardChanged =
            RuntimeId.ToRuntimeId("EventDefine.StacklandsBoardChanged");
        public static readonly int StacklandsCardProgressChanged =
            RuntimeId.ToRuntimeId("EventDefine.StacklandsCardProgressChanged");
        public static readonly int StacklandsHudChanged =
            RuntimeId.ToRuntimeId("EventDefine.StacklandsHudChanged");
        public static readonly int StacklandsFlowRequested =
            RuntimeId.ToRuntimeId("EventDefine.StacklandsFlowRequested");
        public static readonly int StacklandsNotification =
            RuntimeId.ToRuntimeId("EventDefine.StacklandsNotification");
        public static readonly int StacklandsFeeding =
            RuntimeId.ToRuntimeId("EventDefine.StacklandsFeeding");

        #endregion
    }
}
