using GameLogic.Core;
using TEngine;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLogic
{
    /// <summary>
    /// 午夜马戏团主牌桌 UI。
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class CarnivalTableController : MonoBehaviour
    {
        private UIDocument _document;
        private CarnivalGameState _state;

        private VisualElement _root;
        private Label _roundLabel;
        private Label _blindLabel;
        private Label _blindRuleLabel;
        private Label _scoreLabel;
        private Label _targetLabel;
        private Label _handsLabel;
        private Label _discardsLabel;
        private Label _moneyLabel;
        private Label _deckLabel;
        private Label _handNameLabel;
        private Label _formulaLabel;
        private Label _statusLabel;
        private Label _performerCountLabel;
        private Label _consumableCountLabel;
        private VisualElement _progressFill;
        private VisualElement _performerRow;
        private VisualElement _consumableRow;
        private VisualElement _cardRow;
        private VisualElement _shopOverlay;
        private VisualElement _shopOffers;
        private VisualElement _endOverlay;
        private Label _endKicker;
        private Label _endTitle;
        private Label _endDescription;
        private Button _playButton;
        private Button _discardButton;
        private Button _nextRoundButton;
        private Button _sortRankButton;
        private Button _sortSuitButton;
        private Button _restartButton;
        private Button _skipBlindButton;
        private Button _endRestartButton;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            BindVisualTree();
            RegisterCallbacks();
            RegisterGameEvents();
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
            GameEvent.RemoveEventListener<CarnivalGameState>(
                EventDefine.CarnivalStateChanged,
                OnStateChanged);
            _state = null;
        }

        private void RegisterGameEvents()
        {
            GameEvent.AddEventListener<CarnivalGameState>(
                EventDefine.CarnivalStateChanged,
                OnStateChanged);
            GameEvent.Send(EventDefine.CarnivalRequestState);
        }

        private void OnStateChanged(CarnivalGameState state)
        {
            _state = state;
            Render();
        }

        private void BindVisualTree()
        {
            _root = _document.rootVisualElement.Q<VisualElement>(className: "carnival-root");
            _roundLabel = _root.Q<Label>("round-label");
            _blindLabel = _root.Q<Label>("blind-label");
            _blindRuleLabel = _root.Q<Label>("blind-rule-label");
            _scoreLabel = _root.Q<Label>("score-label");
            _targetLabel = _root.Q<Label>("target-label");
            _handsLabel = _root.Q<Label>("hands-label");
            _discardsLabel = _root.Q<Label>("discards-label");
            _moneyLabel = _root.Q<Label>("money-label");
            _deckLabel = _root.Q<Label>("deck-label");
            _handNameLabel = _root.Q<Label>("hand-name");
            _formulaLabel = _root.Q<Label>("formula-label");
            _statusLabel = _root.Q<Label>("status-label");
            _performerCountLabel = _root.Q<Label>("performer-count");
            _consumableCountLabel = _root.Q<Label>("consumable-count");
            _progressFill = _root.Q<VisualElement>("progress-fill");
            _performerRow = _root.Q<VisualElement>("performer-row");
            _consumableRow = _root.Q<VisualElement>("consumable-row");
            _cardRow = _root.Q<VisualElement>("card-row");
            _shopOverlay = _root.Q<VisualElement>("shop-overlay");
            _shopOffers = _root.Q<VisualElement>("shop-offers");
            _endOverlay = _root.Q<VisualElement>("end-overlay");
            _endKicker = _root.Q<Label>("end-kicker");
            _endTitle = _root.Q<Label>("end-title");
            _endDescription = _root.Q<Label>("end-description");
            _playButton = _root.Q<Button>("play-button");
            _discardButton = _root.Q<Button>("discard-button");
            _nextRoundButton = _root.Q<Button>("next-round-button");
            _sortRankButton = _root.Q<Button>("sort-rank-button");
            _sortSuitButton = _root.Q<Button>("sort-suit-button");
            _restartButton = _root.Q<Button>("restart-button");
            _skipBlindButton = _root.Q<Button>("skip-blind-button");
            _endRestartButton = _root.Q<Button>("end-restart-button");
        }

        private void RegisterCallbacks()
        {
            _playButton.clicked += PlaySelected;
            _discardButton.clicked += DiscardSelected;
            _nextRoundButton.clicked += ContinueFromShop;
            _sortRankButton.clicked += SortByRank;
            _sortSuitButton.clicked += SortBySuit;
            _restartButton.clicked += RestartRun;
            _skipBlindButton.clicked += SkipBlind;
            _endRestartButton.clicked += RestartRun;
        }

        private void UnregisterCallbacks()
        {
            if (_document == null)
                return;

            if (_playButton != null)
                _playButton.clicked -= PlaySelected;
            if (_discardButton != null)
                _discardButton.clicked -= DiscardSelected;
            if (_nextRoundButton != null)
                _nextRoundButton.clicked -= ContinueFromShop;
            if (_sortRankButton != null)
                _sortRankButton.clicked -= SortByRank;
            if (_sortSuitButton != null)
                _sortSuitButton.clicked -= SortBySuit;
            if (_restartButton != null)
                _restartButton.clicked -= RestartRun;
            if (_skipBlindButton != null)
                _skipBlindButton.clicked -= SkipBlind;
            if (_endRestartButton != null)
                _endRestartButton.clicked -= RestartRun;
        }

        private void PlaySelected()
        {
            GameEvent.Send(EventDefine.CarnivalPlaySelected);
        }

        private void DiscardSelected()
        {
            GameEvent.Send(EventDefine.CarnivalDiscardSelected);
        }

        private void ContinueFromShop()
        {
            GameEvent.Send(EventDefine.CarnivalContinueFromShop);
        }

        private void RestartRun()
        {
            GameEvent.Send(EventDefine.CarnivalStartNewRun);
        }

        private void SkipBlind()
        {
            GameEvent.Send(EventDefine.CarnivalSkipBlind);
        }

        private void SortByRank()
        {
            GameEvent.Send(EventDefine.CarnivalSortHandByRank);
        }

        private void SortBySuit()
        {
            GameEvent.Send(EventDefine.CarnivalSortHandBySuit);
        }

        private void Render()
        {
            if (_state == null)
                return;

            _roundLabel.text = $"底注 {_state.Ante} · {_state.CurrentBlind.Tier}";
            _blindLabel.text = _state.CurrentBlind.Name;
            _blindRuleLabel.text = _state.CurrentBlind.Description;
            _scoreLabel.text = _state.RoundScore.ToString("N0");
            _targetLabel.text = $"/ {_state.TargetScore:N0}";
            _handsLabel.text = _state.HandsRemaining.ToString();
            _discardsLabel.text = _state.DiscardsRemaining.ToString();
            _moneyLabel.text = $"${_state.Money}";
            _deckLabel.text = _state.CardsInDeck.ToString();
            _statusLabel.text = _state.StatusMessage;
            _performerCountLabel.text = $"{_state.Performers.Count} / 5";
            _consumableCountLabel.text = $"{_state.Consumables.Count} / 2";

            float progress = _state.TargetScore == 0
                ? 0f
                : Mathf.Clamp01((float)_state.RoundScore / _state.TargetScore);
            _progressFill.style.width = Length.Percent(progress * 100f);

            CarnivalScoreState result = _state.LastResult;
            _handNameLabel.text = result?.HandName ?? "等待出牌";
            _formulaLabel.text = result == null
                ? "选择 1–5 张牌，组成牌型"
                : $"{result.Chips} 筹码 × {result.Multiplier:0.#} = {result.Score:N0}";
            _formulaLabel.tooltip = result == null
                ? string.Empty
                : string.Join("\n", result.Breakdown);

            bool canAct = _state.Phase == CarnivalRunPhase.Playing;
            _playButton.SetEnabled(canAct);
            _discardButton.SetEnabled(canAct && _state.DiscardsRemaining > 0);
            _skipBlindButton.SetEnabled(canAct && _state.CurrentBlind.Tier != CarnivalBlindTier.Boss);

            RenderPerformers();
            RenderConsumables();
            RenderCards();
            RenderShop();
            RenderEndState();
            _root.EnableInClassList(
                "carnival-root-overlay-open",
                _state.Phase != CarnivalRunPhase.Playing);
        }

        private void RenderConsumables()
        {
            _consumableRow.Clear();
            foreach (CarnivalConsumable consumable in _state.Consumables)
            {
                CarnivalConsumable capturedConsumable = consumable;
                var button = new Button(() =>
                {
                    GameEvent.Send(EventDefine.CarnivalUseConsumable, capturedConsumable.Id);
                })
                {
                    text = $"{GetConsumableIcon(consumable.Family)}  {consumable.Name}",
                    tooltip = consumable.Description,
                };
                button.AddToClassList("consumable-button");
                button.AddToClassList($"consumable-{consumable.Family.ToString().ToLowerInvariant()}");
                _consumableRow.Add(button);
            }

            if (_state.Consumables.Count == 0)
            {
                var empty = new Label("商店中可购买秘仪、星球与幽影牌");
                empty.AddToClassList("consumable-empty");
                _consumableRow.Add(empty);
            }
        }

        private void RenderPerformers()
        {
            _performerRow.Clear();
            int index = 0;
            foreach (CarnivalPerformer performer in _state.Performers)
            {
                var card = new VisualElement();
                card.AddToClassList("performer-card");
                card.AddToClassList($"performer-tone-{index % 4}");
                card.tooltip = performer.Description;

                var number = new Label((index + 1).ToString("00"));
                number.AddToClassList("performer-number");
                card.Add(number);

                var icon = new Label(performer.Icon);
                icon.AddToClassList("performer-icon");
                card.Add(icon);

                var name = new Label(performer.ShortName);
                name.AddToClassList("performer-name");
                card.Add(name);

                _performerRow.Add(card);
                index++;
            }

            while (index < 5)
            {
                var empty = new VisualElement();
                empty.AddToClassList("performer-card");
                empty.AddToClassList("performer-empty");
                empty.Add(new Label("+"));
                _performerRow.Add(empty);
                index++;
            }
        }

        private void RenderCards()
        {
            _cardRow.Clear();
            foreach (CarnivalCard card in _state.Hand)
            {
                CarnivalCard capturedCard = card;
                var cardButton = new Button(() =>
                {
                    GameEvent.Send(EventDefine.CarnivalToggleCard, capturedCard.Id);
                });
                cardButton.name = $"card-{card.Id}";
                cardButton.AddToClassList("playing-card");
                if (card.IsRed)
                    cardButton.AddToClassList("playing-card-red");
                if (_state.IsSelected(card.Id))
                    cardButton.AddToClassList("playing-card-selected");
                if (card.Enhancement != CarnivalCardEnhancement.None)
                {
                    cardButton.AddToClassList("playing-card-enhanced");
                    cardButton.tooltip = _state.GetEnhancementDescription(card.Enhancement);
                }

                var topRank = new Label(card.RankText);
                topRank.AddToClassList("card-rank");
                cardButton.Add(topRank);

                var suit = new Label(card.SuitText);
                suit.AddToClassList("card-suit");
                cardButton.Add(suit);

                var bottomRank = new Label(card.RankText);
                bottomRank.AddToClassList("card-rank-bottom");
                cardButton.Add(bottomRank);

                _cardRow.Add(cardButton);
            }
        }

        private void RenderShop()
        {
            bool showShop = _state.Phase == CarnivalRunPhase.Shop;
            _shopOverlay.style.display = showShop ? DisplayStyle.Flex : DisplayStyle.None;
            if (!showShop)
                return;

            _shopOffers.Clear();
            foreach (CarnivalShopOffer shopOffer in _state.ShopOffers)
            {
                CarnivalShopOffer capturedOffer = shopOffer;
                var offer = new VisualElement();
                offer.AddToClassList("shop-card");

                var rarity = new Label(shopOffer.Category.ToUpperInvariant());
                rarity.AddToClassList("shop-rarity");
                offer.Add(rarity);

                string iconText = shopOffer.Kind == CarnivalShopOfferKind.Performer
                    ? shopOffer.Performer.Icon
                    : GetConsumableIcon(shopOffer.Consumable.Family);
                var icon = new Label(iconText);
                icon.AddToClassList("shop-icon");
                offer.Add(icon);

                var name = new Label(shopOffer.Name);
                name.AddToClassList("shop-name");
                offer.Add(name);

                var description = new Label(shopOffer.Description);
                description.AddToClassList("shop-description");
                offer.Add(description);

                var buyButton = new Button(() =>
                {
                    if (capturedOffer.Kind == CarnivalShopOfferKind.Performer)
                        GameEvent.Send(EventDefine.CarnivalBuyPerformer, capturedOffer.Id);
                    else
                        GameEvent.Send(EventDefine.CarnivalBuyConsumable, capturedOffer.Id);
                })
                {
                    text = $"购买  ${shopOffer.Cost}",
                };
                buyButton.AddToClassList("shop-buy-button");
                bool hasSpace = shopOffer.Kind == CarnivalShopOfferKind.Performer
                    ? _state.Performers.Count < 5
                    : _state.Consumables.Count < 2;
                buyButton.SetEnabled(_state.Money >= shopOffer.Cost && hasSpace);
                offer.Add(buyButton);
                _shopOffers.Add(offer);
            }

            _nextRoundButton.text = $"挑战下一盲注  →";
        }

        private void RenderEndState()
        {
            bool showEnd = _state.Phase == CarnivalRunPhase.GameOver ||
                           _state.Phase == CarnivalRunPhase.Victory;
            _endOverlay.style.display = showEnd ? DisplayStyle.Flex : DisplayStyle.None;
            if (!showEnd)
                return;

            bool victory = _state.Phase == CarnivalRunPhase.Victory;
            _endKicker.text = victory ? "巡演完成" : "演出中止";
            _endTitle.text = victory ? "满堂喝彩！" : "灯光熄灭";
            _endDescription.text = victory
                ? $"你以 {_state.Performers.Count} 位表演者完成了 3 个底注、全部 9 场盲注。"
                : _state.StatusMessage;
        }

        private static string GetConsumableIcon(CarnivalConsumableFamily family)
        {
            switch (family)
            {
                case CarnivalConsumableFamily.Tarot:
                    return "✦";
                case CarnivalConsumableFamily.Planet:
                    return "●";
                case CarnivalConsumableFamily.Spectral:
                    return "☾";
                default:
                    return "?";
            }
        }

    }
}
