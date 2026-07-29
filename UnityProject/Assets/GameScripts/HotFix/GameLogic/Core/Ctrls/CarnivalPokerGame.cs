using System;
using System.Collections.Generic;

namespace GameLogic.Core
{
    /// <summary>
    /// 2D 卡牌构筑游戏的纯逻辑控制器，不依赖 Unity 生命周期。
    /// </summary>
    public sealed partial class CarnivalPokerGame : ICarnivalPokerCtrl
    {
        private const int HandSize = CarnivalDefine.HandSize;
        private const int MaxSelectedCards = CarnivalDefine.MaxSelectedCards;
        private const int MaxPerformers = CarnivalDefine.MaxPerformers;
        private const int FinalRound = CarnivalDefine.FinalRound;
        private const int MaxConsumables = CarnivalDefine.MaxConsumables;

        private static readonly int[] AnteBaseTargets = { 300, 900, 2600 };

        private readonly Random _random;
        private readonly ICarnivalContentModel _contentModel;
        private readonly List<CarnivalCard> _deck = new List<CarnivalCard>(52);
        private readonly List<CarnivalCard> _hand = new List<CarnivalCard>(HandSize);
        private readonly List<CarnivalCard> _discardPile = new List<CarnivalCard>(52);
        private readonly HashSet<int> _selectedCardIds = new HashSet<int>();
        private readonly List<CarnivalPerformer> _performers = new List<CarnivalPerformer>(MaxPerformers);
        private readonly List<CarnivalShopOffer> _shopOffers = new List<CarnivalShopOffer>(4);
        private readonly List<CarnivalConsumable> _consumables = new List<CarnivalConsumable>(MaxConsumables);
        private readonly Dictionary<CarnivalHandKind, CarnivalHandLevel> _handLevels =
            new Dictionary<CarnivalHandKind, CarnivalHandLevel>();

        private int _runnerBonus;

        public CarnivalPokerGame(int seed = 0)
            : this(new CarnivalContentModel(), seed)
        {
        }

        internal CarnivalPokerGame(ICarnivalContentModel contentModel, int seed = 0)
        {
            _contentModel = contentModel ?? throw new ArgumentNullException(nameof(contentModel));
            _random = seed == 0 ? new Random() : new Random(seed);
        }

        public IReadOnlyList<CarnivalCard> Hand => _hand;
        public IReadOnlyList<CarnivalPerformer> Performers => _performers;
        public IReadOnlyList<CarnivalShopOffer> ShopOffers => _shopOffers;
        public IReadOnlyList<CarnivalConsumable> Consumables => _consumables;
        public IReadOnlyDictionary<CarnivalHandKind, CarnivalHandLevel> HandLevels => _handLevels;
        public CarnivalBlind CurrentBlind { get; private set; }
        public CarnivalRunPhase Phase { get; private set; }
        public CarnivalScoreResult LastResult { get; private set; }
        public int Round { get; private set; }
        public int Ante => (Round - 1) / 3 + 1;
        public int RoundScore { get; private set; }
        public int TargetScore { get; private set; }
        public int HandsRemaining { get; private set; }
        public int DiscardsRemaining { get; private set; }
        public int Money { get; private set; }
        public int CardsInDeck => _deck.Count;
        public string StatusMessage { get; private set; }

        public bool IsSelected(int cardId)
        {
            return _selectedCardIds.Contains(cardId);
        }

        public bool ToggleCard(int cardId)
        {
            if (Phase != CarnivalRunPhase.Playing)
                return false;

            if (_selectedCardIds.Remove(cardId))
                return true;

            if (_selectedCardIds.Count >= MaxSelectedCards)
            {
                StatusMessage = "一次最多选择 5 张牌。";
                return false;
            }

            _selectedCardIds.Add(cardId);
            StatusMessage = $"已选择 {_selectedCardIds.Count} 张牌。";
            return true;
        }

        public CarnivalScoreResult PlaySelected()
        {
            if (Phase != CarnivalRunPhase.Playing)
                return null;

            List<CarnivalCard> playedCards = GetSelectedCards();
            if (playedCards.Count == 0)
            {
                StatusMessage = "先选择 1–5 张牌再出牌。";
                return null;
            }

            if (CurrentBlind.BossRule == CarnivalBossRule.FiveCardOnly && playedCards.Count != 5)
            {
                StatusMessage = "Boss 规则：必须恰好打出 5 张牌。";
                return null;
            }

            HandsRemaining--;
            LastResult = Evaluate(playedCards);
            RoundScore += LastResult.Score;
            ResolveGlassCards(playedCards, LastResult);
            RemoveSelectedCards();

            if (RoundScore >= TargetScore)
            {
                int reward = CurrentBlind.Reward + HandsRemaining;
                Money += reward;
                StatusMessage = $"{CurrentBlind.Name}击破！获得 ${reward}，进入巡演商店。";
                Phase = Round == FinalRound ? CarnivalRunPhase.Victory : CarnivalRunPhase.Shop;
                if (Phase == CarnivalRunPhase.Shop)
                    GenerateShop();
                return LastResult;
            }

            if (HandsRemaining <= 0)
            {
                Phase = CarnivalRunPhase.GameOver;
                StatusMessage = $"演出散场：还差 {TargetScore - RoundScore} 分。";
                return LastResult;
            }

            DrawToHandSize();
            StatusMessage = $"{LastResult.HandName} 得到 {LastResult.Score:N0} 分。";
            return LastResult;
        }

        public bool DiscardSelected()
        {
            if (Phase != CarnivalRunPhase.Playing)
                return false;

            if (_selectedCardIds.Count == 0)
            {
                StatusMessage = "请选择要弃掉的牌。";
                return false;
            }

            if (DiscardsRemaining <= 0)
            {
                StatusMessage = "本回合已没有弃牌次数。";
                return false;
            }

            int count = _selectedCardIds.Count;
            DiscardsRemaining--;
            RemoveSelectedCards();
            DrawToHandSize();
            StatusMessage = $"弃掉 {count} 张牌，重新整理手牌。";
            return true;
        }

        internal void Release()
        {
            _deck.Clear();
            _hand.Clear();
            _discardPile.Clear();
            _selectedCardIds.Clear();
            _performers.Clear();
            _shopOffers.Clear();
            _consumables.Clear();
            _handLevels.Clear();
            LastResult = null;
            CurrentBlind = null;
            StatusMessage = string.Empty;
        }

        private void Shuffle<T>(List<T> items)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int swapIndex = _random.Next(i + 1);
                T value = items[i];
                items[i] = items[swapIndex];
                items[swapIndex] = value;
            }
        }
    }
}
