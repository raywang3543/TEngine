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

        private static readonly int[] AnteBaseTargets =
        {
            300,
            800,
            2000,
            5000,
            11000,
            20000,
            35000,
            50000,
        };

        private readonly Random _random;
        private readonly ICarnivalContentModel _contentModel;
        private readonly ICarnivalUnlockModel _unlockModel;
        private readonly List<CarnivalCard> _deck = new List<CarnivalCard>(52);
        private readonly List<CarnivalCard> _hand = new List<CarnivalCard>(HandSize);
        private readonly List<CarnivalCard> _discardPile = new List<CarnivalCard>(52);
        private readonly HashSet<int> _selectedCardIds = new HashSet<int>();
        private readonly List<CarnivalPerformer> _performers = new List<CarnivalPerformer>(MaxPerformers);
        private readonly List<CarnivalShopOffer> _shopOffers = new List<CarnivalShopOffer>(4);
        private readonly List<CarnivalConsumableState> _consumables =
            new List<CarnivalConsumableState>(MaxConsumables);
        private readonly List<CarnivalConsumable> _boosterChoices = new List<CarnivalConsumable>(3);
        private readonly Dictionary<CarnivalPerformer, CarnivalJokerState> _jokerStates =
            new Dictionary<CarnivalPerformer, CarnivalJokerState>();
        private readonly Dictionary<CarnivalHandKind, CarnivalHandLevel> _handLevels =
            new Dictionary<CarnivalHandKind, CarnivalHandLevel>();
        private readonly Dictionary<CarnivalHandKind, int> _handPlayCounts =
            new Dictionary<CarnivalHandKind, int>();
        private readonly Dictionary<CarnivalHandKind, int> _roundHandPlayCounts =
            new Dictionary<CarnivalHandKind, int>();
        private readonly HashSet<CarnivalHandKind> _usedPlanetKinds = new HashSet<CarnivalHandKind>();
        private readonly HashSet<CarnivalHandKind> _playedHandKindsThisRun = new HashSet<CarnivalHandKind>();
        private readonly HashSet<int> _heartCardsPlayedThisRound = new HashSet<int>();

        private int _runnerBonus;
        private int _nextCardId;
        private int _startingDeckSize;
        private int _handsPlayedThisRound;
        private int _discardsUsedThisRound;
        private int _cardsDiscardedThisRun;
        private int _blindsSkippedThisRun;
        private int _shopRerollsThisRun;
        private int _tarotCardsUsedThisRun;
        private int _freeRerolls;
        private int _doubleTagCount;
        private int _handSizeModifier;
        private int _tagsCollectedThisRun;
        private int _investmentTagCount;
        private bool _firstDiscardUsedThisRound;
        private bool _bossBlindDisabled;
        private bool _grosMichelExtinct;
        private bool _couponShopPending;
        private bool _couponShopActive;
        private bool _d6TagPending;
        private bool _currentHandWasPlayedThisRound;
        private bool _neverExceededFourJokers;
        private CarnivalHandKind _currentEvaluatedHand;
        private CarnivalBoosterPack _currentBoosterPack;
        private CarnivalBoosterPack _openedBoosterPack;
        private CarnivalBlindTag _currentBlindTag;
        private CarnivalConsumable _lastUsedConsumable;

        public CarnivalPokerGame(int seed = 0)
            : this(new CarnivalContentModel(), seed)
        {
        }

        public CarnivalPokerGame(ICarnivalContentModel contentModel, int seed = 0)
            : this(contentModel, new CarnivalUnlockModel(), seed)
        {
        }

        public CarnivalPokerGame(
            ICarnivalContentModel contentModel,
            ICarnivalUnlockModel unlockModel,
            int seed = 0)
        {
            _contentModel = contentModel ?? throw new ArgumentNullException(nameof(contentModel));
            _unlockModel = unlockModel ?? throw new ArgumentNullException(nameof(unlockModel));
            _random = seed == 0 ? new Random() : new Random(seed);
        }

        public IReadOnlyList<CarnivalCard> Hand => _hand;
        public IReadOnlyList<CarnivalPerformer> Performers => _performers;
        public IReadOnlyList<CarnivalShopOffer> ShopOffers => _shopOffers;
        public IReadOnlyList<CarnivalConsumableState> Consumables => _consumables;
        public IReadOnlyList<CarnivalConsumable> BoosterChoices => _boosterChoices;
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
        public int PerformerSlotLimit => MaxPerformerSlots;
        public int RerollCost => _freeRerolls > 0 ? 0 : 5;
        public CarnivalBoosterPack CurrentBoosterPack => _currentBoosterPack;
        public CarnivalBoosterPack OpenedBoosterPack => _openedBoosterPack;
        public CarnivalBlindTag CurrentBlindTag => _currentBlindTag;
        public bool IsBoosterOpen => _openedBoosterPack != null;
        public int DoubleTagCount => _doubleTagCount;
        public int TagsCollectedThisRun => _tagsCollectedThisRun;
        public string StatusMessage { get; private set; }

        public string GetEnhancementDescription(CarnivalCardEnhancement enhancement)
        {
            CarnivalCardEnhancementContent content = _contentModel.FindEnhancement(enhancement);
            return $"{content.Name}：{content.Description}";
        }

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

            if (IsBossRuleActive(CarnivalBossRule.FiveCardOnly) && playedCards.Count != 5)
            {
                StatusMessage = "Boss 规则：必须恰好打出 5 张牌。";
                return null;
            }

            HandsRemaining--;
            LastResult = Evaluate(playedCards);
            RoundScore += LastResult.Score;
            ApplyAfterHandPlayedJokers(playedCards, LastResult);
            ResolveBreakingCards(playedCards, LastResult);
            RemoveSelectedCards();

            if (RoundScore >= TargetScore)
            {
                RecordBlindDefeatedForUnlocks(LastResult);
                ApplyEndOfRoundJokers();
                int reward = CurrentBlind.Reward + HandsRemaining;
                Money += reward;
                EvaluateMoneyUnlocks();
                StatusMessage = $"{CurrentBlind.Name}击破！获得 ${reward}，进入巡演商店。";
                Phase = Round == FinalRound ? CarnivalRunPhase.Victory : CarnivalRunPhase.Shop;
                if (Phase == CarnivalRunPhase.Victory)
                    RecordRunWonForUnlocks();
                if (Phase == CarnivalRunPhase.Shop)
                    GenerateShop();
                return LastResult;
            }

            if (HandsRemaining <= 0)
            {
                CarnivalPerformer mrBones = FindOwnedJoker("mr_bones");
                if (mrBones != null && RoundScore >= Math.Ceiling(TargetScore * 0.25f))
                {
                    RemoveOwnedPerformer(mrBones);
                    RoundScore = TargetScore;
                    RecordBlindDefeatedForUnlocks(LastResult);
                    ApplyEndOfRoundJokers();
                    int reward = CurrentBlind.Reward;
                    Money += reward;
                    EvaluateMoneyUnlocks();
                    StatusMessage = $"骷髅先生避免了失败；进入商店并获得 ${reward}。";
                    Phase = Round == FinalRound ? CarnivalRunPhase.Victory : CarnivalRunPhase.Shop;
                    if (Phase == CarnivalRunPhase.Victory)
                        RecordRunWonForUnlocks();
                    if (Phase == CarnivalRunPhase.Shop)
                        GenerateShop();
                    return LastResult;
                }

                Phase = CarnivalRunPhase.GameOver;
                RecordRunLostForUnlocks();
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
            List<CarnivalCard> discardedCards = GetSelectedCards();
            DiscardsRemaining--;
            RecordDiscardForUnlocks(discardedCards);
            ApplyDiscardJokers(discardedCards);
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
            _boosterChoices.Clear();
            _jokerStates.Clear();
            _handLevels.Clear();
            LastResult = null;
            CurrentBlind = null;
            _currentBoosterPack = null;
            _openedBoosterPack = null;
            _currentBlindTag = null;
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
