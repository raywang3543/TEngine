using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameLogic.Core;
using TEngine;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLogic
{
    /// <summary>
    /// Stacklands 开始界面：开始菜单与新回合设置弹窗，仅通过事件命令访问 Core。
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class StacklandsStartScreenController : MonoBehaviour
    {
        private UIDocument _document;
        private VisualElement _startMenu;
        private Button _menuContinue;
        private Button _menuNewGame;
        private Button _menuQuit;
        private VisualElement _modal;
        private Toggle _peaceful;
        private DropdownField _moonLength;
        private Button _newGame;
        private Button _closeModal;

        private void Awake() => _document = GetComponent<UIDocument>();

        private void OnEnable()
        {
            BindVisuals();
            // 重新打开时回到开始菜单状态，避免停留在上次的新回合弹窗。
            _modal.AddToClassList("hidden");
            _startMenu.RemoveFromClassList("hidden");
            GameEvent.AddEventListener<FlowRequest>(EventDefine.StacklandsFlowRequested, OnFlowRequested);
        }

        private void OnDisable()
        {
            GameEvent.RemoveEventListener<FlowRequest>(EventDefine.StacklandsFlowRequested, OnFlowRequested);
            UnbindButtons();
        }

        private void BindVisuals()
        {
            VisualElement root = _document.rootVisualElement;
            _startMenu = root.Q("start-menu");
            _menuContinue = root.Q<Button>("menu-continue");
            _menuNewGame = root.Q<Button>("menu-new-game");
            _menuQuit = root.Q<Button>("menu-quit");
            _modal = root.Q("modal-overlay");
            _peaceful = root.Q<Toggle>("peaceful-toggle");
            _moonLength = root.Q<DropdownField>("moon-length");
            _moonLength.choices = new List<string> { "短（60 秒）", "普通（120 秒）", "长（180 秒）" };
            _moonLength.index = 1;
            _newGame = root.Q<Button>("new-game-button");
            _closeModal = root.Q<Button>("close-modal-button");
            _menuContinue.clicked += MenuContinue;
            _menuNewGame.clicked += OpenNewGame;
            _menuQuit.clicked += QuitGame;
            _newGame.clicked += NewGame;
            _closeModal.clicked += CloseModal;
        }

        private void UnbindButtons()
        {
            if (_menuContinue == null) return;
            _menuContinue.clicked -= MenuContinue;
            _menuNewGame.clicked -= OpenNewGame;
            _menuQuit.clicked -= QuitGame;
            _newGame.clicked -= NewGame;
            _closeModal.clicked -= CloseModal;
        }

        private void OnFlowRequested(FlowRequest request)
        {
            // 开始界面只响应主菜单流程，其余流程由游戏界面处理。
            if (request.Kind != StacklandsFlowKind.MainMenu) return;
            _modal.AddToClassList("hidden");
            _menuContinue.style.display = request.CanContinue ? DisplayStyle.Flex : DisplayStyle.None;
            _startMenu.RemoveFromClassList("hidden");
        }

        private void MenuContinue()
        {
            Send(new StacklandsCommandDto { Kind = StacklandsCommandKind.ContinueGame });
            SwitchToGameUI();
        }

        /// <summary>
        /// 开始新回合：隐藏开始菜单，弹出模式与 Moon 长度设置，确定后走原新游戏逻辑。
        /// </summary>
        private void OpenNewGame()
        {
            _startMenu.AddToClassList("hidden");
            _modal.RemoveFromClassList("hidden");
        }

        private void CloseModal()
        {
            _modal.AddToClassList("hidden");
            _startMenu.RemoveFromClassList("hidden");
        }

        private void NewGame()
        {
            Send(StacklandsCommandDto.NewGame(_peaceful.value, _moonLength.index));
            SwitchToGameUI();
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// 关闭开始界面并打开游戏界面。
        /// </summary>
        private void SwitchToGameUI() => SwitchToGameUIAsync().Forget();

        private async UniTaskVoid SwitchToGameUIAsync()
        {
            await GameModule.UIToolkit.ShowUIAsync<StacklandsGameScreenController>(UITypes.StacklandsGameScreen);
            GameModule.UIToolkit.CloseUI(UITypes.StacklandsStartScreen);
        }

        private static void Send(StacklandsCommandDto command) => GameEvent.Send(EventDefine.StacklandsCommand, command);
    }
}
