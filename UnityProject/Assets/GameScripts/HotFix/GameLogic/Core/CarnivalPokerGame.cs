using System;
using System.Collections.Generic;

namespace GameLogic.Core
{
    /// <summary>
    /// 花色。
    /// </summary>
    public enum CarnivalSuit
    {
        Spades,
        Hearts,
        Diamonds,
        Clubs,
    }

    /// <summary>
    /// 牌型。
    /// </summary>
    public enum CarnivalHandKind
    {
        HighCard,
        Pair,
        TwoPair,
        ThreeOfAKind,
        Straight,
        Flush,
        FullHouse,
        FourOfAKind,
        StraightFlush,
    }

    /// <summary>
    /// 局内阶段。
    /// </summary>
    public enum CarnivalRunPhase
    {
        Playing,
        Shop,
        GameOver,
        Victory,
    }

    /// <summary>
    /// 一张标准扑克牌。
    /// </summary>
    public readonly struct CarnivalCard
    {
        public CarnivalCard(int id, CarnivalSuit suit, int rank)
        {
            Id = id;
            Suit = suit;
            Rank = rank;
        }

        public int Id { get; }
        public CarnivalSuit Suit { get; }
        public int Rank { get; }
        public bool IsRed => Suit == CarnivalSuit.Hearts || Suit == CarnivalSuit.Diamonds;

        public string RankText
        {
            get
            {
                switch (Rank)
                {
                    case 11:
                        return "J";
                    case 12:
                        return "Q";
                    case 13:
                        return "K";
                    case 14:
                        return "A";
                    default:
                        return Rank.ToString();
                }
            }
        }

        public string SuitText
        {
            get
            {
                switch (Suit)
                {
                    case CarnivalSuit.Spades:
                        return "♠";
                    case CarnivalSuit.Hearts:
                        return "♥";
                    case CarnivalSuit.Diamonds:
                        return "♦";
                    case CarnivalSuit.Clubs:
                        return "♣";
                    default:
                        return "?";
                }
            }
        }

        public int ChipValue => Rank == 14 ? 11 : Math.Min(Rank, 10);
    }

    /// <summary>
    /// 原创表演者卡定义。
    /// </summary>
    public sealed class CarnivalPerformer
    {
        public CarnivalPerformer(
            string id,
            string name,
            string shortName,
            string description,
            int cost,
            string rarity)
        {
            Id = id;
            Name = name;
            ShortName = shortName;
            Description = description;
            Cost = cost;
            Rarity = rarity;
        }

        public string Id { get; }
        public string Name { get; }
        public string ShortName { get; }
        public string Description { get; }
        public int Cost { get; }
        public string Rarity { get; }
    }

    /// <summary>
    /// 单次计分结果。
    /// </summary>
    public sealed class CarnivalScoreResult
    {
        public CarnivalHandKind Kind { get; set; }
        public string HandName { get; set; }
        public int Chips { get; set; }
        public float Multiplier { get; set; }
        public int Score { get; set; }
        public List<int> ScoringCardIds { get; } = new List<int>();
        public List<string> Breakdown { get; } = new List<string>();
    }

    /// <summary>
    /// 2D 卡牌构筑游戏的纯逻辑模型，不依赖 Unity 生命周期。
    /// </summary>
    public sealed class CarnivalPokerGame
    {
        private const int HandSize = 8;
        private const int MaxSelectedCards = 5;
        private const int MaxPerformers = 5;
        private const int FinalRound = 5;

        private static readonly int[] RoundTargets = { 280, 650, 1250, 2300, 4200 };

        private static readonly CarnivalPerformer[] PerformerCatalog =
        {
            new CarnivalPerformer(
                "red-ribbons",
                "红绸舞者",
                "红绸",
                "每张计分红心给予 +3 倍率。",
                4,
                "普通"),
            new CarnivalPerformer(
                "pocket-confetti",
                "口袋彩屑",
                "彩屑",
                "打出不超过 3 张牌时给予 +24 筹码。",
                4,
                "普通"),
            new CarnivalPerformer(
                "club-lantern",
                "梅花提灯",
                "提灯",
                "每张计分梅花给予 +5 倍率。",
                6,
                "稀有"),
            new CarnivalPerformer(
                "mirror-duet",
                "镜面二重奏",
                "镜面",
                "牌型包含对子时，最终倍率 ×2。",
                8,
                "稀有"),
            new CarnivalPerformer(
                "street-runner",
                "高跷跑者",
                "跑者",
                "每次打出顺子永久获得 +12 筹码。",
                7,
                "稀有"),
            new CarnivalPerformer(
                "diamond-register",
                "钻石收银机",
                "收银",
                "每张计分方块获得 $1。",
                6,
                "稀有"),
            new CarnivalPerformer(
                "late-finale",
                "压轴面具",
                "压轴",
                "本回合最后一次出牌时，最终倍率 ×2.5。",
                9,
                "史诗"),
            new CarnivalPerformer(
                "full-tent",
                "满座帐篷",
                "满座",
                "每拥有一张表演者卡给予 +4 倍率。",
                7,
                "稀有"),
            new CarnivalPerformer(
                "odd-acrobat",
                "奇数杂技团",
                "奇数",
                "每张计分 A、3、5、7、9 给予 +18 筹码。",
                6,
                "稀有"),
        };

        private readonly Random _random;
        private readonly List<CarnivalCard> _deck = new List<CarnivalCard>(52);
        private readonly List<CarnivalCard> _hand = new List<CarnivalCard>(HandSize);
        private readonly List<CarnivalCard> _discardPile = new List<CarnivalCard>(52);
        private readonly HashSet<int> _selectedCardIds = new HashSet<int>();
        private readonly List<CarnivalPerformer> _performers = new List<CarnivalPerformer>(MaxPerformers);
        private readonly List<CarnivalPerformer> _shopOffers = new List<CarnivalPerformer>(3);

        private int _runnerBonus;

        public CarnivalPokerGame(int seed = 0)
        {
            _random = seed == 0 ? new Random() : new Random(seed);
        }

        public IReadOnlyList<CarnivalCard> Hand => _hand;
        public IReadOnlyList<CarnivalPerformer> Performers => _performers;
        public IReadOnlyList<CarnivalPerformer> ShopOffers => _shopOffers;
        public CarnivalRunPhase Phase { get; private set; }
        public CarnivalScoreResult LastResult { get; private set; }
        public int Round { get; private set; }
        public int RoundScore { get; private set; }
        public int TargetScore { get; private set; }
        public int HandsRemaining { get; private set; }
        public int DiscardsRemaining { get; private set; }
        public int Money { get; private set; }
        public int CardsInDeck => _deck.Count;
        public string StatusMessage { get; private set; }

        public void StartNewRun()
        {
            Round = 1;
            Money = 6;
            _runnerBonus = 0;
            _performers.Clear();
            _performers.Add(FindPerformer("red-ribbons"));
            _performers.Add(FindPerformer("pocket-confetti"));
            StartRound();
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

            HandsRemaining--;
            LastResult = Evaluate(playedCards);
            RoundScore += LastResult.Score;
            RemoveSelectedCards();

            if (RoundScore >= TargetScore)
            {
                int reward = 5 + HandsRemaining;
                Money += reward;
                StatusMessage = $"目标达成！获得 ${reward}，进入巡演商店。";
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

        public bool BuyPerformer(string performerId)
        {
            if (Phase != CarnivalRunPhase.Shop)
                return false;

            CarnivalPerformer performer = FindOffer(performerId);
            if (performer == null)
                return false;

            if (_performers.Count >= MaxPerformers)
            {
                StatusMessage = "表演者席位已满（最多 5 张）。";
                return false;
            }

            if (Money < performer.Cost)
            {
                StatusMessage = $"金币不足，还需要 ${performer.Cost - Money}。";
                return false;
            }

            Money -= performer.Cost;
            _performers.Add(performer);
            _shopOffers.Remove(performer);
            StatusMessage = $"已邀请「{performer.Name}」加入巡演。";
            return true;
        }

        public void ContinueFromShop()
        {
            if (Phase != CarnivalRunPhase.Shop)
                return;

            Round++;
            StartRound();
        }

        public void SortHandByRank()
        {
            _hand.Sort((left, right) =>
            {
                int rankCompare = right.Rank.CompareTo(left.Rank);
                return rankCompare != 0 ? rankCompare : left.Suit.CompareTo(right.Suit);
            });
        }

        public void SortHandBySuit()
        {
            _hand.Sort((left, right) =>
            {
                int suitCompare = left.Suit.CompareTo(right.Suit);
                return suitCompare != 0 ? suitCompare : right.Rank.CompareTo(left.Rank);
            });
        }

        private void StartRound()
        {
            Phase = CarnivalRunPhase.Playing;
            RoundScore = 0;
            TargetScore = RoundTargets[Round - 1];
            HandsRemaining = 4;
            DiscardsRemaining = 3;
            LastResult = null;
            StatusMessage = $"第 {Round} 场开演。选择牌组成牌型！";
            _selectedCardIds.Clear();
            _hand.Clear();
            _discardPile.Clear();
            BuildAndShuffleDeck();
            DrawToHandSize();
            SortHandByRank();
        }

        private void BuildAndShuffleDeck()
        {
            _deck.Clear();
            int id = 0;
            foreach (CarnivalSuit suit in Enum.GetValues(typeof(CarnivalSuit)))
            {
                for (int rank = 2; rank <= 14; rank++)
                    _deck.Add(new CarnivalCard(id++, suit, rank));
            }

            Shuffle(_deck);
        }

        private void DrawToHandSize()
        {
            while (_hand.Count < HandSize)
            {
                if (_deck.Count == 0)
                {
                    if (_discardPile.Count == 0)
                        break;

                    _deck.AddRange(_discardPile);
                    _discardPile.Clear();
                    Shuffle(_deck);
                }

                int lastIndex = _deck.Count - 1;
                _hand.Add(_deck[lastIndex]);
                _deck.RemoveAt(lastIndex);
            }
        }

        private List<CarnivalCard> GetSelectedCards()
        {
            var cards = new List<CarnivalCard>(_selectedCardIds.Count);
            foreach (CarnivalCard card in _hand)
            {
                if (_selectedCardIds.Contains(card.Id))
                    cards.Add(card);
            }

            return cards;
        }

        private void RemoveSelectedCards()
        {
            for (int i = _hand.Count - 1; i >= 0; i--)
            {
                if (!_selectedCardIds.Contains(_hand[i].Id))
                    continue;

                _discardPile.Add(_hand[i]);
                _hand.RemoveAt(i);
            }

            _selectedCardIds.Clear();
        }

        private CarnivalScoreResult Evaluate(List<CarnivalCard> cards)
        {
            var rankGroups = BuildRankGroups(cards);
            bool isFlush = cards.Count == 5 && AllSameSuit(cards);
            bool isStraight = cards.Count == 5 && IsStraight(cards);
            CarnivalHandKind kind = ResolveHandKind(cards.Count, rankGroups, isFlush, isStraight);
            CarnivalScoreResult result = CreateBaseResult(kind);

            AddScoringCards(result, cards, rankGroups);
            foreach (CarnivalCard card in cards)
            {
                if (result.ScoringCardIds.Contains(card.Id))
                    result.Chips += card.ChipValue;
            }

            result.Breakdown.Add($"基础 {result.Chips} 筹码 × {result.Multiplier:0.#} 倍率");

            foreach (CarnivalPerformer performer in _performers)
                ApplyPerformer(performer, cards, result);

            result.Score = Math.Max(1, (int)Math.Round(result.Chips * result.Multiplier));
            result.Breakdown.Add($"最终得分 {result.Score:N0}");
            return result;
        }

        private void ApplyPerformer(
            CarnivalPerformer performer,
            List<CarnivalCard> playedCards,
            CarnivalScoreResult result)
        {
            int matchingCount;
            switch (performer.Id)
            {
                case "red-ribbons":
                    matchingCount = CountScoringCards(playedCards, result, card => card.Suit == CarnivalSuit.Hearts);
                    if (matchingCount > 0)
                    {
                        result.Multiplier += matchingCount * 3;
                        result.Breakdown.Add($"红绸舞者 +{matchingCount * 3} 倍率");
                    }
                    break;
                case "pocket-confetti":
                    if (playedCards.Count <= 3)
                    {
                        result.Chips += 24;
                        result.Breakdown.Add("口袋彩屑 +24 筹码");
                    }
                    break;
                case "club-lantern":
                    matchingCount = CountScoringCards(playedCards, result, card => card.Suit == CarnivalSuit.Clubs);
                    if (matchingCount > 0)
                    {
                        result.Multiplier += matchingCount * 5;
                        result.Breakdown.Add($"梅花提灯 +{matchingCount * 5} 倍率");
                    }
                    break;
                case "mirror-duet":
                    if (result.Kind >= CarnivalHandKind.Pair)
                    {
                        result.Multiplier *= 2f;
                        result.Breakdown.Add("镜面二重奏 ×2 倍率");
                    }
                    break;
                case "street-runner":
                    if (result.Kind == CarnivalHandKind.Straight ||
                        result.Kind == CarnivalHandKind.StraightFlush)
                    {
                        _runnerBonus += 12;
                    }

                    if (_runnerBonus > 0)
                    {
                        result.Chips += _runnerBonus;
                        result.Breakdown.Add($"高跷跑者 +{_runnerBonus} 筹码");
                    }
                    break;
                case "diamond-register":
                    matchingCount = CountScoringCards(
                        playedCards,
                        result,
                        card => card.Suit == CarnivalSuit.Diamonds);
                    if (matchingCount > 0)
                    {
                        Money += matchingCount;
                        result.Breakdown.Add($"钻石收银机 +${matchingCount}");
                    }
                    break;
                case "late-finale":
                    if (HandsRemaining == 0)
                    {
                        result.Multiplier *= 2.5f;
                        result.Breakdown.Add("压轴面具 ×2.5 倍率");
                    }
                    break;
                case "full-tent":
                    int bonus = _performers.Count * 4;
                    result.Multiplier += bonus;
                    result.Breakdown.Add($"满座帐篷 +{bonus} 倍率");
                    break;
                case "odd-acrobat":
                    matchingCount = CountScoringCards(
                        playedCards,
                        result,
                        card => card.Rank == 14 || card.Rank % 2 == 1);
                    if (matchingCount > 0)
                    {
                        result.Chips += matchingCount * 18;
                        result.Breakdown.Add($"奇数杂技团 +{matchingCount * 18} 筹码");
                    }
                    break;
            }
        }

        private static Dictionary<int, List<CarnivalCard>> BuildRankGroups(List<CarnivalCard> cards)
        {
            var groups = new Dictionary<int, List<CarnivalCard>>();
            foreach (CarnivalCard card in cards)
            {
                if (!groups.TryGetValue(card.Rank, out List<CarnivalCard> group))
                {
                    group = new List<CarnivalCard>();
                    groups.Add(card.Rank, group);
                }

                group.Add(card);
            }

            return groups;
        }

        private static CarnivalHandKind ResolveHandKind(
            int cardCount,
            Dictionary<int, List<CarnivalCard>> rankGroups,
            bool isFlush,
            bool isStraight)
        {
            if (isFlush && isStraight)
                return CarnivalHandKind.StraightFlush;

            bool hasFour = false;
            bool hasThree = false;
            int pairCount = 0;
            foreach (List<CarnivalCard> group in rankGroups.Values)
            {
                if (group.Count == 4)
                    hasFour = true;
                else if (group.Count == 3)
                    hasThree = true;
                else if (group.Count == 2)
                    pairCount++;
            }

            if (hasFour)
                return CarnivalHandKind.FourOfAKind;
            if (cardCount == 5 && hasThree && pairCount == 1)
                return CarnivalHandKind.FullHouse;
            if (isFlush)
                return CarnivalHandKind.Flush;
            if (isStraight)
                return CarnivalHandKind.Straight;
            if (hasThree)
                return CarnivalHandKind.ThreeOfAKind;
            if (pairCount >= 2)
                return CarnivalHandKind.TwoPair;
            if (pairCount == 1)
                return CarnivalHandKind.Pair;
            return CarnivalHandKind.HighCard;
        }

        private static CarnivalScoreResult CreateBaseResult(CarnivalHandKind kind)
        {
            var result = new CarnivalScoreResult { Kind = kind };
            switch (kind)
            {
                case CarnivalHandKind.Pair:
                    result.HandName = "对子";
                    result.Chips = 20;
                    result.Multiplier = 2f;
                    break;
                case CarnivalHandKind.TwoPair:
                    result.HandName = "两对";
                    result.Chips = 30;
                    result.Multiplier = 2f;
                    break;
                case CarnivalHandKind.ThreeOfAKind:
                    result.HandName = "三条";
                    result.Chips = 30;
                    result.Multiplier = 3f;
                    break;
                case CarnivalHandKind.Straight:
                    result.HandName = "顺子";
                    result.Chips = 35;
                    result.Multiplier = 4f;
                    break;
                case CarnivalHandKind.Flush:
                    result.HandName = "同花";
                    result.Chips = 35;
                    result.Multiplier = 4f;
                    break;
                case CarnivalHandKind.FullHouse:
                    result.HandName = "葫芦";
                    result.Chips = 40;
                    result.Multiplier = 4f;
                    break;
                case CarnivalHandKind.FourOfAKind:
                    result.HandName = "四条";
                    result.Chips = 60;
                    result.Multiplier = 7f;
                    break;
                case CarnivalHandKind.StraightFlush:
                    result.HandName = "同花顺";
                    result.Chips = 100;
                    result.Multiplier = 8f;
                    break;
                default:
                    result.HandName = "高牌";
                    result.Chips = 5;
                    result.Multiplier = 1f;
                    break;
            }

            return result;
        }

        private static void AddScoringCards(
            CarnivalScoreResult result,
            List<CarnivalCard> cards,
            Dictionary<int, List<CarnivalCard>> rankGroups)
        {
            if (result.Kind == CarnivalHandKind.Straight ||
                result.Kind == CarnivalHandKind.Flush ||
                result.Kind == CarnivalHandKind.FullHouse ||
                result.Kind == CarnivalHandKind.StraightFlush)
            {
                AddCardIds(result, cards);
                return;
            }

            int requiredCount = 1;
            if (result.Kind == CarnivalHandKind.Pair || result.Kind == CarnivalHandKind.TwoPair)
                requiredCount = 2;
            else if (result.Kind == CarnivalHandKind.ThreeOfAKind)
                requiredCount = 3;
            else if (result.Kind == CarnivalHandKind.FourOfAKind)
                requiredCount = 4;

            if (result.Kind == CarnivalHandKind.HighCard)
            {
                CarnivalCard highest = cards[0];
                foreach (CarnivalCard card in cards)
                {
                    if (card.Rank > highest.Rank)
                        highest = card;
                }

                result.ScoringCardIds.Add(highest.Id);
                return;
            }

            foreach (List<CarnivalCard> group in rankGroups.Values)
            {
                if (group.Count == requiredCount)
                    AddCardIds(result, group);
            }
        }

        private static void AddCardIds(CarnivalScoreResult result, List<CarnivalCard> cards)
        {
            foreach (CarnivalCard card in cards)
                result.ScoringCardIds.Add(card.Id);
        }

        private static bool AllSameSuit(List<CarnivalCard> cards)
        {
            CarnivalSuit suit = cards[0].Suit;
            for (int i = 1; i < cards.Count; i++)
            {
                if (cards[i].Suit != suit)
                    return false;
            }

            return true;
        }

        private static bool IsStraight(List<CarnivalCard> cards)
        {
            var ranks = new List<int>(5);
            foreach (CarnivalCard card in cards)
            {
                if (!ranks.Contains(card.Rank))
                    ranks.Add(card.Rank);
            }

            if (ranks.Count != 5)
                return false;

            ranks.Sort();
            bool normalStraight = true;
            for (int i = 1; i < ranks.Count; i++)
            {
                if (ranks[i] != ranks[0] + i)
                {
                    normalStraight = false;
                    break;
                }
            }

            if (normalStraight)
                return true;

            return ranks[0] == 2 &&
                   ranks[1] == 3 &&
                   ranks[2] == 4 &&
                   ranks[3] == 5 &&
                   ranks[4] == 14;
        }

        private static int CountScoringCards(
            List<CarnivalCard> cards,
            CarnivalScoreResult result,
            Predicate<CarnivalCard> predicate)
        {
            int count = 0;
            foreach (CarnivalCard card in cards)
            {
                if (result.ScoringCardIds.Contains(card.Id) && predicate(card))
                    count++;
            }

            return count;
        }

        private void GenerateShop()
        {
            _shopOffers.Clear();
            var candidates = new List<CarnivalPerformer>();
            foreach (CarnivalPerformer performer in PerformerCatalog)
            {
                if (!_performers.Contains(performer))
                    candidates.Add(performer);
            }

            Shuffle(candidates);
            int offerCount = Math.Min(3, candidates.Count);
            for (int i = 0; i < offerCount; i++)
                _shopOffers.Add(candidates[i]);
        }

        private CarnivalPerformer FindOffer(string performerId)
        {
            foreach (CarnivalPerformer offer in _shopOffers)
            {
                if (offer.Id == performerId)
                    return offer;
            }

            return null;
        }

        private static CarnivalPerformer FindPerformer(string performerId)
        {
            foreach (CarnivalPerformer performer in PerformerCatalog)
            {
                if (performer.Id == performerId)
                    return performer;
            }

            throw new InvalidOperationException($"Unknown performer: {performerId}");
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
