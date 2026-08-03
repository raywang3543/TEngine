using System;
using GameLogic.Core.Content;

namespace GameLogic.Core
{
    /// <summary>
    /// 游戏核心规则的统一入口；表现层不得直接持有此处的可变系统引用。
    /// </summary>
    public static class CoreSystem
    {
        public static bool IsInitialized => Content != null;
        public static IStacklandsContentCatalog Content { get; private set; }

        public static void Initialize(IStacklandsContentCatalog content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public static void Release()
        {
            Content = null;
        }
    }
}
