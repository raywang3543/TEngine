using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using GameLogic.Core;
using TEngine;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLogic
{
    /// <summary>
    /// Stacklands HUD 与菜单，仅通过事件命令访问 Core。
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class StacklandsGameScreenController : MonoBehaviour
    {
        private UIDocument _document;
        private ScrollView _quests;
        private Label _questCount;
        private Label _moon;
        private Label _time;
        private Label _food;
        private Label _cap;
        private Label _detailTitle;
        private Label _detailDescription;
        private Label _detailStats;
        private VisualElement _modal;
        private Label _modalTitle;
        private Label _modalMessage;
        private Toggle _peaceful;
        private DropdownField _moonLength;
        private Button _confirmFlow;
        private Button _newGame;
        private Button _closeModal;
        private Button _settings;
        private Button _cardopedia;
        private Button _questToggle;
        private VisualElement _sidebar;
        private Button[] _speedButtons;
        private ScrollView _modalList;
        private BoardSnapshot _board;
        private string _selectedId;
        private IVisualElementScheduledItem _toastSchedule;
        private FlowRequest _currentFlow;
        private HudSnapshot _lastHud;

        private void Awake() => _document = GetComponent<UIDocument>();

        private void OnEnable()
        {
            BindVisuals();
            GameEvent.AddEventListener<BoardSnapshot>(EventDefine.StacklandsBoardChanged, OnBoardChanged);
            GameEvent.AddEventListener<HudSnapshot>(EventDefine.StacklandsHudChanged, OnHudChanged);
            GameEvent.AddEventListener<FlowRequest>(EventDefine.StacklandsFlowRequested, OnFlowRequested);
            GameEvent.AddEventListener<string>(EventDefine.StacklandsNotification, ShowToast);
        }

        private void OnDisable()
        {
            GameEvent.RemoveEventListener<BoardSnapshot>(EventDefine.StacklandsBoardChanged, OnBoardChanged);
            GameEvent.RemoveEventListener<HudSnapshot>(EventDefine.StacklandsHudChanged, OnHudChanged);
            GameEvent.RemoveEventListener<FlowRequest>(EventDefine.StacklandsFlowRequested, OnFlowRequested);
            GameEvent.RemoveEventListener<string>(EventDefine.StacklandsNotification, ShowToast);
            UnbindButtons();
        }

        private void BindVisuals()
        {
            VisualElement root = _document.rootVisualElement;
            _quests = root.Q<ScrollView>("quest-list");
            _questCount = root.Q<Label>("quest-count");
            _moon = root.Q<Label>("moon-label"); _time = root.Q<Label>("time-label");
            _food = root.Q<Label>("food-label"); _cap = root.Q<Label>("cap-label");
            _detailTitle = root.Q<Label>("detail-title"); _detailDescription = root.Q<Label>("detail-description");
            _detailStats = root.Q<Label>("detail-stats");
            _modal = root.Q("modal-overlay"); _modalTitle = root.Q<Label>("modal-title");
            _modalMessage = root.Q<Label>("modal-message"); _peaceful = root.Q<Toggle>("peaceful-toggle");
            _moonLength = root.Q<DropdownField>("moon-length");
            _modalList = root.Q<ScrollView>("modal-list");
            _moonLength.choices = new List<string> { "短（60 秒）", "普通（120 秒）", "长（180 秒）" };
            _moonLength.index = 1;
            _confirmFlow = root.Q<Button>("confirm-flow-button");
            _newGame = root.Q<Button>("new-game-button");
            _closeModal = root.Q<Button>("close-modal-button");
            _settings = root.Q<Button>("settings-button");
            _cardopedia = root.Q<Button>("cardopedia-button");
            _questToggle = root.Q<Button>("quest-toggle-button");
            _questToggle.clicked += ToggleSidebar;
            _sidebar = root.Q("left-sidebar");
            _speedButtons = new[] { root.Q<Button>("speed-0"), root.Q<Button>("speed-1"), root.Q<Button>("speed-5") };
            _speedButtons[0].clicked += Speed0; _speedButtons[1].clicked += Speed1; _speedButtons[2].clicked += Speed5;
            _newGame.clicked += NewGame;
            _confirmFlow.clicked += ConfirmFlow;
            _closeModal.clicked += CloseModal;
            _settings.clicked += OpenSettings;
            _cardopedia.clicked += OpenCardopedia;
        }

        private void UnbindButtons()
        {
            if (_speedButtons == null) return;
            _speedButtons[0].clicked -= Speed0; _speedButtons[1].clicked -= Speed1; _speedButtons[2].clicked -= Speed5;
            _newGame.clicked -= NewGame;
            _confirmFlow.clicked -= ConfirmFlow;
            _closeModal.clicked -= CloseModal;
            _settings.clicked -= OpenSettings;
            _cardopedia.clicked -= OpenCardopedia;
            _questToggle.clicked -= ToggleSidebar;
        }

        /// <summary>
        /// 折叠/展开整个任务侧栏（含任务列表与卡牌详情），只保留边缘小按钮。
        /// </summary>
        private void ToggleSidebar()
        {
            bool expand = _sidebar.ClassListContains("collapsed");
            _sidebar.EnableInClassList("collapsed", !expand);
            _questToggle.text = expand ? "«" : "»";
        }

        private void OnBoardChanged(BoardSnapshot snapshot)
        {
            _board = snapshot;
            _selectedId = snapshot.SelectedInstanceId;
            CardSnapshot selected = null;
            foreach (CardSnapshot card in snapshot.Cards)
                if (card.InstanceId == _selectedId) { selected = card; break; }
            if (selected == null)
            {
                _detailTitle.text = "选择一张卡牌"; _detailDescription.text = "拖动卡牌进行堆叠。";
                _detailStats.text = string.Empty; return;
            }
            _detailTitle.text = selected.NameZh;
            _detailDescription.text = selected.DescriptionZh;
            _detailStats.text = $"售价 {selected.SellPrice}  食物 {selected.FoodValue}" +
                                (selected.MaxHp > 0 ? $"  HP {selected.Hp}/{selected.MaxHp}" : string.Empty);
        }

        private void OnHudChanged(HudSnapshot hud)
        {
            _lastHud = hud;
            _moon.text = "Moon " + hud.Moon; _time.text = Mathf.CeilToInt(hud.MoonRemaining) + "s";
            _food.text = "食物 " + hud.Food;
            _cap.text = $"卡牌 {hud.CardCount}/{hud.CardCap}";
            _questCount.text = hud.CompletedQuestCount + "/56";
            for (int i = 0; i < _speedButtons.Length; i++) _speedButtons[i].RemoveFromClassList("speed-active");
            int active = hud.Speed <= 0f ? 0 : hud.Speed >= 5f ? 2 : 1;
            _speedButtons[active].AddToClassList("speed-active");
            RenderQuests(hud);
        }

        private void RenderQuests(HudSnapshot hud)
        {
            _quests.Clear();
            foreach (QuestSnapshot quest in hud.Quests)
            {
                var label = new Label((quest.Completed ? "✓ " : "• ") + quest.NameZh) { tooltip = quest.DescriptionZh };
                label.AddToClassList("quest-item");
                if (quest.Completed) label.AddToClassList("quest-complete");
                if (quest.IsMain) label.AddToClassList("quest-main");
                _quests.Add(label);
            }
        }

        private void OnFlowRequested(FlowRequest request)
        {
            // 主菜单流程由开始界面处理，关闭游戏界面并切换过去。
            if (request.Kind == StacklandsFlowKind.MainMenu)
            {
                SwitchToStartUI();
                return;
            }
            _currentFlow = request;
            _modalTitle.text = request.Title; _modalMessage.text = request.Message;
            _modal.RemoveFromClassList("hidden");
            _modalList.AddToClassList("hidden");
            _peaceful.style.display = DisplayStyle.None;
            _moonLength.style.display = DisplayStyle.None;
            _confirmFlow.style.display = request.Kind == StacklandsFlowKind.SummonDemon ? DisplayStyle.Flex : DisplayStyle.None;
            _newGame.style.display = request.Kind == StacklandsFlowKind.GameOver ||
                                     request.Kind == StacklandsFlowKind.Victory ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// 关闭游戏界面并打开开始界面。
        /// </summary>
        private void SwitchToStartUI() => SwitchToStartUIAsync().Forget();

        private async UniTaskVoid SwitchToStartUIAsync()
        {
            await GameModule.UIToolkit.ShowUIAsync<StacklandsStartScreenController>(UITypes.StacklandsStartScreen);
            GameModule.UIToolkit.CloseUI(UITypes.StacklandsGameScreen);
        }

        private void ShowToast(string message)
        {
            VisualElement toast = _document.rootVisualElement.Q("toast");
            toast.Q<Label>("toast-label").text = message;
            toast.RemoveFromClassList("hidden");
            _toastSchedule?.Pause();
            _toastSchedule = toast.schedule.Execute(() => toast.AddToClassList("hidden")).StartingIn(2500);
        }

        private static void Send(StacklandsCommandDto command) => GameEvent.Send(EventDefine.StacklandsCommand, command);
        private void Speed0() => Send(new StacklandsCommandDto { Kind = StacklandsCommandKind.SetSpeed, Number = 0f });
        private void Speed1() => Send(new StacklandsCommandDto { Kind = StacklandsCommandKind.SetSpeed, Number = 1f });
        private void Speed5() => Send(new StacklandsCommandDto { Kind = StacklandsCommandKind.SetSpeed, Number = 5f });
        private void NewGame() { CloseModal(); Send(StacklandsCommandDto.NewGame(_peaceful.value, _moonLength.index)); }
        private void ConfirmFlow()
        {
            if (_currentFlow?.Kind == StacklandsFlowKind.SummonDemon)
                Send(new StacklandsCommandDto
                {
                    Kind = StacklandsCommandKind.ConfirmSummon, InstanceId = _currentFlow.InstanceId, Flag = true,
                });
            CloseModal();
        }
        private void CloseModal() => _modal.AddToClassList("hidden");
        private void OpenSettings()
        {
            _currentFlow = null;
            _modalTitle.text = "游戏菜单"; _modalMessage.text = "暂停、继续或开始新游戏";
            _peaceful.style.display = DisplayStyle.Flex; _moonLength.style.display = DisplayStyle.Flex;
            _confirmFlow.style.display = DisplayStyle.None; _newGame.style.display = DisplayStyle.Flex; _modalList.AddToClassList("hidden");
            _modal.RemoveFromClassList("hidden");
        }
        private void OpenCardopedia()
        {
            _currentFlow = null; _modalTitle.text = "Cardopedia 卡牌图鉴";
            int discovered = _lastHud?.Cardopedia == null ? 0 : _lastHud.Cardopedia.Count(item => item.Discovered);
            int total = _lastHud?.Cardopedia?.Count ?? 0;
            _modalMessage.text = $"已发现 {discovered}/{total}；Idea 配方也会记录在图鉴中。";
            _peaceful.style.display = DisplayStyle.None; _moonLength.style.display = DisplayStyle.None;
            _confirmFlow.style.display = DisplayStyle.None; _newGame.style.display = DisplayStyle.None;
            _modalList.RemoveFromClassList("hidden");
            _modalList.Clear();
            if (_lastHud?.Cardopedia != null)
                foreach (CardopediaEntrySnapshot entry in _lastHud.Cardopedia)
                {
                    var label = new Label(entry.Discovered ? $"{entry.NameZh}  [{entry.Category}]" : "？？？");
                    label.AddToClassList("cardopedia-entry");
                    if (!entry.Discovered) label.AddToClassList("cardopedia-hidden");
                    _modalList.Add(label);
                }
            _modal.RemoveFromClassList("hidden");
        }
    }
}
