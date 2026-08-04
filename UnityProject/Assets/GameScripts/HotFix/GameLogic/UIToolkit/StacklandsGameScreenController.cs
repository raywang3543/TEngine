using System.Collections.Generic;
using System.Linq;
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
        private ScrollView _boosters;
        private Label _questCount;
        private Label _coin;
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
        private Button _continue;
        private Button _confirmFlow;
        private Button _newGame;
        private Button _closeModal;
        private Button _settings;
        private Button _cardopedia;
        private Button _sell;
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
            _quests = root.Q<ScrollView>("quest-list"); _boosters = root.Q<ScrollView>("booster-shelf");
            _questCount = root.Q<Label>("quest-count"); _coin = root.Q<Label>("coin-label");
            _moon = root.Q<Label>("moon-label"); _time = root.Q<Label>("time-label");
            _food = root.Q<Label>("food-label"); _cap = root.Q<Label>("cap-label");
            _detailTitle = root.Q<Label>("detail-title"); _detailDescription = root.Q<Label>("detail-description");
            _detailStats = root.Q<Label>("detail-stats"); _sell = root.Q<Button>("sell-selected");
            _modal = root.Q("modal-overlay"); _modalTitle = root.Q<Label>("modal-title");
            _modalMessage = root.Q<Label>("modal-message"); _peaceful = root.Q<Toggle>("peaceful-toggle");
            _moonLength = root.Q<DropdownField>("moon-length");
            _modalList = root.Q<ScrollView>("modal-list");
            _moonLength.choices = new List<string> { "短（60 秒）", "普通（120 秒）", "长（180 秒）" };
            _moonLength.index = 1;
            _continue = root.Q<Button>("continue-button");
            _confirmFlow = root.Q<Button>("confirm-flow-button");
            _newGame = root.Q<Button>("new-game-button");
            _closeModal = root.Q<Button>("close-modal-button");
            _settings = root.Q<Button>("settings-button");
            _cardopedia = root.Q<Button>("cardopedia-button");
            _speedButtons = new[] { root.Q<Button>("speed-0"), root.Q<Button>("speed-1"), root.Q<Button>("speed-5") };
            _speedButtons[0].clicked += Speed0; _speedButtons[1].clicked += Speed1; _speedButtons[2].clicked += Speed5;
            _newGame.clicked += NewGame;
            _continue.clicked += ContinueGame;
            _confirmFlow.clicked += ConfirmFlow;
            _closeModal.clicked += CloseModal;
            _settings.clicked += OpenSettings;
            _cardopedia.clicked += OpenCardopedia;
            _sell.clicked += SellSelected;
        }

        private void UnbindButtons()
        {
            if (_speedButtons == null) return;
            _speedButtons[0].clicked -= Speed0; _speedButtons[1].clicked -= Speed1; _speedButtons[2].clicked -= Speed5;
            _newGame.clicked -= NewGame;
            _continue.clicked -= ContinueGame;
            _confirmFlow.clicked -= ConfirmFlow;
            _closeModal.clicked -= CloseModal;
            _settings.clicked -= OpenSettings;
            _cardopedia.clicked -= OpenCardopedia;
            _sell.clicked -= SellSelected;
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
                _detailStats.text = string.Empty; _sell.SetEnabled(false); return;
            }
            _detailTitle.text = selected.NameZh;
            _detailDescription.text = selected.DescriptionZh;
            _detailStats.text = $"售价 {selected.SellPrice}  食物 {selected.FoodValue}" +
                                (selected.MaxHp > 0 ? $"  HP {selected.Hp}/{selected.MaxHp}" : string.Empty);
            _sell.SetEnabled(selected.SellPrice > 0);
        }

        private void OnHudChanged(HudSnapshot hud)
        {
            _lastHud = hud;
            _moon.text = "Moon " + hud.Moon; _time.text = Mathf.CeilToInt(hud.MoonRemaining) + "s";
            _coin.text = hud.Coins.ToString(); _food.text = "食物 " + hud.Food;
            _cap.text = $"卡牌 {hud.CardCount}/{hud.CardCap}";
            _questCount.text = hud.CompletedQuestCount + "/56";
            for (int i = 0; i < _speedButtons.Length; i++) _speedButtons[i].RemoveFromClassList("speed-active");
            int active = hud.Speed <= 0f ? 0 : hud.Speed >= 5f ? 2 : 1;
            _speedButtons[active].AddToClassList("speed-active");
            RenderQuests(hud); RenderBoosters(hud);
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

        private void RenderBoosters(HudSnapshot hud)
        {
            _boosters.Clear();
            foreach (BoosterShopSnapshot booster in hud.Boosters)
            {
                var button = new Button(() => BuyBooster(booster.Id))
                {
                    text = booster.NameZh + "\n" + (booster.Unlocked ? booster.Price + " Coin" : booster.LockText),
                    tooltip = booster.LockText,
                };
                button.AddToClassList("booster-button");
                if (!booster.Unlocked) button.AddToClassList("booster-locked");
                button.SetEnabled(booster.Unlocked && hud.Coins >= booster.Price);
                _boosters.Add(button);
            }
        }

        private void OnFlowRequested(FlowRequest request)
        {
            _currentFlow = request;
            _modalTitle.text = request.Title; _modalMessage.text = request.Message;
            _modal.RemoveFromClassList("hidden");
            bool menu = request.Kind == StacklandsFlowKind.MainMenu;
            _modalList.AddToClassList("hidden");
            _peaceful.style.display = menu ? DisplayStyle.Flex : DisplayStyle.None;
            _moonLength.style.display = menu ? DisplayStyle.Flex : DisplayStyle.None;
            _continue.style.display = menu && request.CanContinue ? DisplayStyle.Flex : DisplayStyle.None;
            _confirmFlow.style.display = request.Kind == StacklandsFlowKind.SummonDemon ? DisplayStyle.Flex : DisplayStyle.None;
            _newGame.style.display = menu || request.Kind == StacklandsFlowKind.GameOver ||
                                     request.Kind == StacklandsFlowKind.Victory ? DisplayStyle.Flex : DisplayStyle.None;
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
        private void ContinueGame() { CloseModal(); Send(new StacklandsCommandDto { Kind = StacklandsCommandKind.ContinueGame }); }
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
            _continue.style.display = DisplayStyle.None; _confirmFlow.style.display = DisplayStyle.None;
            _newGame.style.display = DisplayStyle.Flex; _modalList.AddToClassList("hidden");
            _modal.RemoveFromClassList("hidden");
        }
        private void OpenCardopedia()
        {
            _currentFlow = null; _modalTitle.text = "Cardopedia 卡牌图鉴";
            int discovered = _lastHud?.Cardopedia == null ? 0 : _lastHud.Cardopedia.Count(item => item.Discovered);
            _modalMessage.text = $"已发现 {discovered}/121；Idea 配方也会记录在图鉴中。";
            _peaceful.style.display = DisplayStyle.None; _moonLength.style.display = DisplayStyle.None;
            _continue.style.display = DisplayStyle.None; _confirmFlow.style.display = DisplayStyle.None;
            _newGame.style.display = DisplayStyle.None; _modalList.RemoveFromClassList("hidden");
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
        private void BuyBooster(string id) => Send(new StacklandsCommandDto { Kind = StacklandsCommandKind.BuyBooster, ContentId = id });
        private void SellSelected()
        {
            if (string.IsNullOrEmpty(_selectedId)) return;
            Send(new StacklandsCommandDto { Kind = StacklandsCommandKind.SellCard, InstanceId = _selectedId });
        }
    }
}
