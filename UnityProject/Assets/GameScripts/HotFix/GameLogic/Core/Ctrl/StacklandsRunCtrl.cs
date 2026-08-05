using System;
using GameLogic.Core.Model;

namespace GameLogic.Core.Ctrl
{
    /// <summary>
    /// 当前局生命周期、命令路由、固定步长与存档控制器。
    /// </summary>
    internal sealed class StacklandsRunCtrl
    {
        // 临时验收开关：正式卡包流程完成后删除测试卡包及两张 test_ 配置卡。
        private const bool EnableTemporaryEquipmentTestBooster = true;

        private StacklandsGameModel Model => CoreSystem.Model;

        internal void Start()
        {
            // 启动只请求开始菜单，不自动加载存档；继续回合或开始新回合后才加载对局。
            bool hasSave = Model.SaveStore.LoadRun() != null;
            CoreSystem.RequestFlow(new FlowRequest
            {
                Kind = StacklandsFlowKind.MainMenu, Title = "堆叠大陆",
                Message = hasSave ? "继续当前冒险，或保留跨局进度开始新游戏" : "开始新的 Original 主大陆冒险",
                CanContinue = hasSave,
            });
            CoreSystem.ViewCtrl.PublishAll();
        }

        internal void Handle(StacklandsCommandDto command)
        {
            if (command == null) return;
            switch (command.Kind)
            {
                case StacklandsCommandKind.NewGame:
                    NewGame(command.Flag, (int)command.Number); break;
                case StacklandsCommandKind.ContinueGame:
                    LoadRun(Model.SaveStore.LoadRun()); break;
                case StacklandsCommandKind.SetSpeed:
                    if (Model.Run != null && !Model.Run.AwaitingCardLimit) Model.Run.Speed = command.Number;
                    CoreSystem.ViewCtrl.PublishHud(); break;
                case StacklandsCommandKind.MoveCard:
                case StacklandsCommandKind.MoveStack:
                    CoreSystem.BoardCtrl.Move(command.InstanceId, command.TargetInstanceId, command.X, command.Y,
                        command.Kind == StacklandsCommandKind.MoveStack); break;
                case StacklandsCommandKind.SelectCard:
                    Model.SelectedId = command.InstanceId; CoreSystem.ViewCtrl.PublishBoard(); break;
                case StacklandsCommandKind.BuyBooster:
                    CoreSystem.LootCtrl.BuyBooster(command.ContentId, command.InstanceId); break;
                case StacklandsCommandKind.OpenBooster:
                    CoreSystem.LootCtrl.OpenBooster(command.InstanceId); break;
                case StacklandsCommandKind.MoveBooster:
                    CoreSystem.LootCtrl.MoveBooster(command.InstanceId, command.X, command.Y); break;
                case StacklandsCommandKind.SellCard:
                    CoreSystem.BoardCtrl.Sell(command.InstanceId); break;
                case StacklandsCommandKind.Equip:
                    CoreSystem.EquipmentCtrl.Equip(command.InstanceId, command.TargetInstanceId); break;
                case StacklandsCommandKind.Unequip:
                    CoreSystem.EquipmentCtrl.Unequip(command.InstanceId, command.EquipmentSlot); break;
                case StacklandsCommandKind.ConfirmSummon:
                    if (command.Flag) CoreSystem.WorkCtrl.StartSummonAction(command.InstanceId); break;
                case StacklandsCommandKind.SaveGame:
                    SaveNow(); break;
            }
        }

        internal void Tick(float unscaledDeltaTime)
        {
            if (Model.Run == null) return;
            if (Model.SaveDelay >= 0f)
            {
                Model.SaveDelay -= unscaledDeltaTime;
                if (Model.SaveDelay <= 0f) SaveNow();
            }

            float delta = unscaledDeltaTime * Model.Run.Speed;
            if (delta <= 0f || Model.Run.AwaitingCardLimit) return;
            CoreSystem.WorkCtrl.Tick(delta);
            CoreSystem.ViewCtrl.PublishCardProgress();
            CoreSystem.CombatCtrl.Tick(delta);
            CoreSystem.WorldCtrl.TickMovement(delta);
            Model.Run.MoonRemaining -= delta;
            if (Model.Run.MoonRemaining <= 0f) CoreSystem.WorldCtrl.EndMoon();
            CoreSystem.ViewCtrl.PublishHud();
        }

        internal void SaveNow()
        {
            if (Model.Run == null) return;
            try
            {
                Model.Run.RandomState = Model.Random.State;
                Model.SaveStore.SaveProfile(Model.Profile);
                Model.SaveStore.SaveRun(Model.Run);
                Model.SaveDelay = -1f;
            }
            catch (Exception exception)
            {
                Model.SaveDelay = -1f;
                CoreSystem.RequestFlow(new FlowRequest
                {
                    Kind = StacklandsFlowKind.SaveError, Title = "存档失败", Message = exception.Message,
                });
            }
        }

        private void NewGame(bool peaceful, int moonLengthIndex)
        {
            int duration = moonLengthIndex == 0 ? Model.Content.WorldRules.MoonShortSeconds :
                moonLengthIndex == 2 ? Model.Content.WorldRules.MoonLongSeconds :
                Model.Content.WorldRules.MoonNormalSeconds;
            Model.Run = new StacklandsRunData
            {
                RandomState = (uint)DateTime.UtcNow.Ticks, Moon = 1, MoonDuration = duration,
                MoonRemaining = duration, Peaceful = peaceful, Speed = 1f,
            };
            Model.Random = new DeterministicRandom(Model.Run.RandomState);
            CoreSystem.LootCtrl.CreateBooster("a_new_world", -4f, 2f, false);
            if (EnableTemporaryEquipmentTestBooster)
                CoreSystem.LootCtrl.CreateTemporaryEquipmentTestBooster(0f, 2f);
            Model.Increment("EventCount:new_game");
            Model.MarkDirty();
            CoreSystem.ViewCtrl.PublishAll();
        }

        private void LoadRun(StacklandsRunData data)
        {
            if (data == null)
            {
                CoreSystem.RequestFlow(new FlowRequest
                    { Kind = StacklandsFlowKind.MainMenu, Title = "没有存档", Message = "请开始新游戏" });
                return;
            }
            Model.Run = data;
            Model.Random = new DeterministicRandom(data.RandomState);
            Model.RemoveInvalidSaveEntries();
            CoreSystem.EquipmentCtrl.ValidateRunEquipment();
            CoreSystem.ViewCtrl.PublishAll();
        }
    }
}
