using System;
using GameLogic.Core.Ctrl;
using GameLogic.Core.Model;
using GameLogic.Core.View;
using TEngine;

namespace GameLogic.Core
{
    /// <summary>
    /// Stacklands 游戏专属边界与唯一协调器；Model、Ctrl、View 不直接互相持有。
    /// </summary>
    public static class CoreSystem
    {
        public static bool IsInitialized => Model != null;
        internal static StacklandsGameModel Model { get; private set; }
        internal static IStacklandsContentModel Content => Model?.Content;
        internal static StacklandsBoardView View { get; private set; }

        internal static StacklandsRunCtrl RunCtrl { get; private set; }
        internal static StacklandsBoardCtrl BoardCtrl { get; private set; }
        internal static StacklandsWorkCtrl WorkCtrl { get; private set; }
        internal static StacklandsLootCtrl LootCtrl { get; private set; }
        internal static StacklandsCombatCtrl CombatCtrl { get; private set; }
        internal static StacklandsWorldCtrl WorldCtrl { get; private set; }
        internal static StacklandsQuestCtrl QuestCtrl { get; private set; }
        internal static StacklandsViewCtrl ViewCtrl { get; private set; }

        public static void Initialize(IStacklandsContentModel content, IStacklandsSaveStore saveStore,
            StacklandsBoardView view)
        {
            Release();
            Model = new StacklandsGameModel(content ?? throw new ArgumentNullException(nameof(content)),
                saveStore ?? throw new ArgumentNullException(nameof(saveStore)));
            View = view ?? throw new ArgumentNullException(nameof(view));
            RunCtrl = new StacklandsRunCtrl();
            BoardCtrl = new StacklandsBoardCtrl();
            WorkCtrl = new StacklandsWorkCtrl();
            LootCtrl = new StacklandsLootCtrl();
            CombatCtrl = new StacklandsCombatCtrl();
            WorldCtrl = new StacklandsWorldCtrl();
            QuestCtrl = new StacklandsQuestCtrl();
            ViewCtrl = new StacklandsViewCtrl();
            GameEvent.AddEventListener<StacklandsCommandDto>(EventDefine.StacklandsCommand, SubmitCommand);
            RunCtrl.Start();
        }

        public static void SubmitCommand(StacklandsCommandDto command) => RunCtrl?.Handle(command);
        public static void Tick(float unscaledDeltaTime) => RunCtrl?.Tick(unscaledDeltaTime);
        public static void Save() => RunCtrl?.SaveNow();

        public static void Release()
        {
            if (Model != null)
            {
                GameEvent.RemoveEventListener<StacklandsCommandDto>(EventDefine.StacklandsCommand, SubmitCommand);
                RunCtrl?.SaveNow();
            }
            ViewCtrl = null;
            QuestCtrl = null;
            WorldCtrl = null;
            CombatCtrl = null;
            LootCtrl = null;
            WorkCtrl = null;
            BoardCtrl = null;
            RunCtrl = null;
            View = null;
            Model = null;
        }

        internal static void PublishBoard(BoardSnapshot snapshot)
        {
            View?.Render(snapshot);
            GameEvent.Send(EventDefine.StacklandsBoardChanged, snapshot);
        }

        internal static void PublishHud(HudSnapshot snapshot) =>
            GameEvent.Send(EventDefine.StacklandsHudChanged, snapshot);
        internal static void RequestFlow(FlowRequest request) =>
            GameEvent.Send(EventDefine.StacklandsFlowRequested, request);
        internal static void Notify(string message) =>
            GameEvent.Send(EventDefine.StacklandsNotification, message);
    }
}
