using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using GameLogic.Core;
using GameLogic.Core.Content;
using GameLogic;
#if ENABLE_OBFUZ
using Obfuz;
#endif
using TEngine;
#pragma warning disable CS0436


/// <summary>
/// 游戏App。
/// </summary>
#if ENABLE_OBFUZ
[ObfuzIgnore(ObfuzScope.TypeName | ObfuzScope.MethodName)]
#endif
public partial class GameApp
{
    private static List<Assembly> _hotfixAssembly;

    /// <summary>
    /// 热更域App主入口。
    /// </summary>
    /// <param name="objects"></param>
    public static void Entrance(object[] objects)
    {
        _hotfixAssembly = (List<Assembly>)objects[0];
        Log.Warning("======= 看到此条日志代表你成功运行了热更新代码 =======");
        Log.Warning("======= Entrance GameApp =======");
        Utility.Unity.AddDestroyListener(Release);
        Log.Warning("======= StartGameLogic =======");
        StartGameLogic().Forget();
    }
    
    private static async UniTaskVoid StartGameLogic()
    {
        ContentValidationReport report = StacklandsContentLoader.Validate(ConfigSystem.Instance.Tables);
        if (report.HasErrors)
        {
            throw new InvalidOperationException("Stacklands Original 内容配置校验失败：\n" + report);
        }

        IStacklandsContentCatalog content = StacklandsContentLoader.Build(ConfigSystem.Instance.Tables);
        CoreSystem.Initialize(content);

        // 测试：显示 Hello World UI Toolkit 界面。
        await GameModule.UIToolkit.ShowUIAsync<HelloWorldScreenController>(UITypes.HelloWorldScreen);
    }
    
    private static void Release()
    {
        CoreSystem.Release();
        SingletonSystem.Release();
        Log.Warning("======= Release GameApp =======");
    }
}
