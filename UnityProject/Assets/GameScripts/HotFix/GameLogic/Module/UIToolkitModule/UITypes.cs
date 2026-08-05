using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// UI 类型枚举，每个值对应一个 UXML 资源路径。
    /// 新增 UI 时需要在此添加枚举值和对应的 YooAsset 地址映射。
    /// </summary>
    public enum UITypes
    {
        WelcomeScreen,
        BetDialog,
        CarnivalTable,
        HelloWorldScreen,
        StacklandsGameScreen,
        StacklandsStartScreen,
    }

    /// <summary>
    /// UITypes 的扩展方法，提供 UXML 资源路径映射。
    /// </summary>
    public static class UITypesExtensions
    {
        /// <summary>
        /// 获取 UI 类型对应的 YooAsset 地址。
        /// UIToolkit 收集器使用 AddressByFileName，因此地址为不带扩展名的文件名。
        /// </summary>
        public static string GetAssetLocation(this UITypes type)
        {
            switch (type)
            {
                case UITypes.WelcomeScreen:
                    return "WelcomeScreen";
                case UITypes.BetDialog:
                    return "BetDialog";
                case UITypes.CarnivalTable:
                    return "CarnivalTable";
                case UITypes.HelloWorldScreen:
                    return "HelloWorldScreen";
                case UITypes.StacklandsGameScreen:
                    return "StacklandsGameScreen";
                case UITypes.StacklandsStartScreen:
                    return "StacklandsStartScreen";
                default:
                    TEngine.Log.Error($"[UITypes] 未定义的 YooAsset 地址: {type}");
                    return null;
            }
        }
    }
}
