using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using GameLogic.Core;
using GameLogic.Core.Model;
using GameLogic.Core.View;
using GameLogic;
using System.IO;
using UnityEngine;
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
    private static GameObject _stacklandsRoot;

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
        ContentValidationReport report = StacklandsModelLoader.Validate(ConfigSystem.Instance.Tables);
        if (report.HasErrors)
        {
            throw new InvalidOperationException("Stacklands Original 内容配置校验失败：\n" + report);
        }

        IStacklandsContentModel content = StacklandsModelLoader.Build(ConfigSystem.Instance.Tables);
        GameModule.Debugger.ActiveWindow = false;
        _stacklandsRoot = new GameObject("Stacklands Original Runtime");
        UnityEngine.Object.DontDestroyOnLoad(_stacklandsRoot);
        _stacklandsRoot.AddComponent<StacklandsGameDriver>();
        StacklandsBoardView boardView = _stacklandsRoot.AddComponent<StacklandsBoardView>();

        await GameModule.UIToolkit.ShowUIAsync<StacklandsGameScreenController>(UITypes.StacklandsGameScreen);
        string savePath = Path.Combine(Application.persistentDataPath, "StacklandsOriginal");
        CoreSystem.Initialize(content, new JsonStacklandsSaveStore(savePath), boardView);
        GameModule.Audio.Play(TEngine.AudioType.Music, StacklandsGameModel.BgmLocation, bLoop: true, bAsync: true);
    }

    private static void Release()
    {
        CoreSystem.Release();
        GameModule.Audio.Stop(TEngine.AudioType.Music, false);
        if (_stacklandsRoot != null)
        {
            UnityEngine.Object.Destroy(_stacklandsRoot);
            _stacklandsRoot = null;
        }
        SingletonSystem.Release();
        Log.Warning("======= Release GameApp =======");
    }
}
