using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 游戏逻辑跨模块事件定义。
    /// </summary>
    public static class EventDefine
    {
        #region Login UI

        public static readonly int ShowLoginUI =
            RuntimeId.ToRuntimeId("EventDefine.ShowLoginUI");
        public static readonly int CloseLoginUI =
            RuntimeId.ToRuntimeId("EventDefine.CloseLoginUI");

        #endregion
    }
}
