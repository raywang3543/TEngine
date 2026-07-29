using GameLogic.Core;
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
        private CarnivalPokerGame _game;

        private VisualElement _root;
        private Label _roundLabel;
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
        private VisualElement _progressFill;
        private VisualElement _performerRow;
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
        private Button _endRestartButton;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            _game = new CarnivalPokerGame();
        }

        private void OnEnable()
        {
            BindVisualTree();
            RegisterCallbacks();
            _game.StartNewRun();
            Render();
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
        }

        private void BindVisualTree()
        {
            _root = _document.rootVisualElement.Q<VisualElement>(className: "carnival-root");
            _roundLabel = _root.Q<Label>("round-label");
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
            _progressFill = _root.Q<VisualElement>("progress-fill");
            _performerRow = _root.Q<VisualElement>("performer-row");
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
            if (_endRestartButton != null)
                _endRestartButton.clicked -= RestartRun;
        }

        private void PlaySelected()
        {
            _game.PlaySelected();
            Render();
        }

        private void DiscardSelected()
        {
            _game.DiscardSelected();
            Render();
        }

        private void ContinueFromShop()
        {
            _game.ContinueFromShop();
            Render();
        }

        private void RestartRun()
        {
            _game.StartNewRun();
            Render();
        }

        private void SortByRank()
        {
            _game.SortHandByRank();
            RenderCards();
        }

        private void SortBySuit()
        {
            _game.SortHandBySuit();
            RenderCards();
        }

        private void Render()
        {
            _roundLabel.text = $"巡演 {_game.Round} / 5";
            _scoreLabel.text = _game.RoundScore.ToString("N0");
            _targetLabel.text = $"/ {_game.TargetScore:N0}";
            _handsLabel.text = _game.HandsRemaining.ToString();
            _discardsLabel.text = _game.DiscardsRemaining.ToString();
            _moneyLabel.text = $"${_game.Money}";
            _deckLabel.text = _game.CardsInDeck.ToString();
            _statusLabel.text = _game.StatusMessage;
            _performerCountLabel.text = $"{_game.Performers.Count} / 5";

            float progress = _game.TargetScore == 0
                ? 0f
                : Mathf.Clamp01((float)_game.RoundScore / _game.TargetScore);
            _progressFill.style.width = Length.Percent(progress * 100f);

            CarnivalScoreResult result = _game.LastResult;
            _handNameLabel.text = result?.HandName ?? "等待出牌";
            _formulaLabel.text = result == null
                ? "选择 1–5 张牌，组成牌型"
                : $"{result.Chips} 筹码 × {result.Multiplier:0.#} = {result.Score:N0}";
            _formulaLabel.tooltip = result == null
                ? string.Empty
                : string.Join("\n", result.Breakdown);

            bool canAct = _game.Phase == CarnivalRunPhase.Playing;
            _playButton.SetEnabled(canAct);
            _discardButton.SetEnabled(canAct && _game.DiscardsRemaining > 0);

            RenderPerformers();
            RenderCards();
            RenderShop();
            RenderEndState();
            _root.EnableInClassList(
                "carnival-root-overlay-open",
                _game.Phase != CarnivalRunPhase.Playing);
        }

        private void RenderPerformers()
        {
            _performerRow.Clear();
            int index = 0;
            foreach (CarnivalPerformer performer in _game.Performers)
            {
                var card = new VisualElement();
                card.AddToClassList("performer-card");
                card.AddToClassList($"performer-tone-{index % 4}");
                card.tooltip = performer.Description;

                var number = new Label((index + 1).ToString("00"));
                number.AddToClassList("performer-number");
                card.Add(number);

                var icon = new Label(GetPerformerIcon(performer.Id));
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
            foreach (CarnivalCard card in _game.Hand)
            {
                CarnivalCard capturedCard = card;
                var cardButton = new Button(() =>
                {
                    _game.ToggleCard(capturedCard.Id);
                    Render();
                });
                cardButton.name = $"card-{card.Id}";
                cardButton.AddToClassList("playing-card");
                if (card.IsRed)
                    cardButton.AddToClassList("playing-card-red");
                if (_game.IsSelected(card.Id))
                    cardButton.AddToClassList("playing-card-selected");

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
            bool showShop = _game.Phase == CarnivalRunPhase.Shop;
            _shopOverlay.style.display = showShop ? DisplayStyle.Flex : DisplayStyle.None;
            if (!showShop)
                return;

            _shopOffers.Clear();
            foreach (CarnivalPerformer performer in _game.ShopOffers)
            {
                CarnivalPerformer capturedPerformer = performer;
                var offer = new VisualElement();
                offer.AddToClassList("shop-card");

                var rarity = new Label(performer.Rarity.ToUpperInvariant());
                rarity.AddToClassList("shop-rarity");
                offer.Add(rarity);

                var icon = new Label(GetPerformerIcon(performer.Id));
                icon.AddToClassList("shop-icon");
                offer.Add(icon);

                var name = new Label(performer.Name);
                name.AddToClassList("shop-name");
                offer.Add(name);

                var description = new Label(performer.Description);
                description.AddToClassList("shop-description");
                offer.Add(description);

                var buyButton = new Button(() =>
                {
                    _game.BuyPerformer(capturedPerformer.Id);
                    Render();
                })
                {
                    text = $"邀请  ${performer.Cost}",
                };
                buyButton.AddToClassList("shop-buy-button");
                buyButton.SetEnabled(_game.Money >= performer.Cost && _game.Performers.Count < 5);
                offer.Add(buyButton);
                _shopOffers.Add(offer);
            }

            _nextRoundButton.text = $"前往巡演 {_game.Round + 1}  →";
        }

        private void RenderEndState()
        {
            bool showEnd = _game.Phase == CarnivalRunPhase.GameOver ||
                           _game.Phase == CarnivalRunPhase.Victory;
            _endOverlay.style.display = showEnd ? DisplayStyle.Flex : DisplayStyle.None;
            if (!showEnd)
                return;

            bool victory = _game.Phase == CarnivalRunPhase.Victory;
            _endKicker.text = victory ? "巡演完成" : "演出中止";
            _endTitle.text = victory ? "满堂喝彩！" : "灯光熄灭";
            _endDescription.text = victory
                ? $"你以 {_game.Performers.Count} 位表演者完成了全部 5 场巡演。"
                : _game.StatusMessage;
        }

        private static string GetPerformerIcon(string performerId)
        {
            switch (performerId)
            {
                case "red-ribbons":
                    return "♥";
                case "pocket-confetti":
                    return "✦";
                case "club-lantern":
                    return "♣";
                case "mirror-duet":
                    return "◈";
                case "street-runner":
                    return "↟";
                case "diamond-register":
                    return "♦";
                case "late-finale":
                    return "☾";
                case "full-tent":
                    return "♜";
                case "odd-acrobat":
                    return "★";
                default:
                    return "?";
            }
        }
    }
}
