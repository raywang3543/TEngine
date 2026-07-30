using System.Collections.Generic;

namespace GameLogic.Core
{
    /// <summary>
    /// Core 对外发布的只读牌局快照。
    /// </summary>
    public sealed class CarnivalGameState
    {
        public CarnivalGameState(ICarnivalPokerCtrl source)
        {
            Hand = Copy(source.Hand);
            Performers = Copy(source.Performers);
            ShopOffers = Copy(source.ShopOffers);
            Consumables = CopyConsumables(source.Consumables);
            BoosterChoices = Copy(source.BoosterChoices);
            CurrentBlind = source.CurrentBlind;
            CurrentBoosterPack = source.CurrentBoosterPack;
            OpenedBoosterPack = source.OpenedBoosterPack;
            CurrentBlindTag = source.CurrentBlindTag;
            IsBoosterOpen = source.IsBoosterOpen;
            Phase = source.Phase;
            LastResult = source.LastResult == null ? null : new CarnivalScoreState(source.LastResult);
            Ante = source.Ante;
            RoundScore = source.RoundScore;
            TargetScore = source.TargetScore;
            HandsRemaining = source.HandsRemaining;
            DiscardsRemaining = source.DiscardsRemaining;
            Money = source.Money;
            CardsInDeck = source.CardsInDeck;
            PerformerSlotLimit = source.PerformerSlotLimit;
            RerollCost = source.RerollCost;
            BoosterPackCost = source.GetBoosterPackCost();
            DoubleTagCount = source.DoubleTagCount;
            TagsCollectedThisRun = source.TagsCollectedThisRun;
            StatusMessage = source.StatusMessage;

            var performerSellValues = new List<int>(source.Performers.Count);
            for (int index = 0; index < source.Performers.Count; index++)
                performerSellValues.Add(source.GetPerformerSellValue(index));
            PerformerSellValues = performerSellValues.AsReadOnly();

            var offerCosts = new Dictionary<string, int>();
            foreach (CarnivalShopOffer offer in source.ShopOffers)
                offerCosts[offer.Id] = source.GetOfferCost(offer.Id);
            OfferCosts = offerCosts;

            var selectedCardIds = new HashSet<int>();
            foreach (CarnivalCard card in source.Hand)
            {
                if (source.IsSelected(card.Id))
                    selectedCardIds.Add(card.Id);
            }

            SelectedCardIds = selectedCardIds;

            var enhancementDescriptions = new Dictionary<CarnivalCardEnhancement, string>();
            foreach (CarnivalCard card in source.Hand)
            {
                if (card.Enhancement == CarnivalCardEnhancement.None ||
                    enhancementDescriptions.ContainsKey(card.Enhancement))
                {
                    continue;
                }

                enhancementDescriptions.Add(
                    card.Enhancement,
                    source.GetEnhancementDescription(card.Enhancement));
            }

            EnhancementDescriptions = enhancementDescriptions;
        }

        public IReadOnlyList<CarnivalCard> Hand { get; }
        public IReadOnlyList<CarnivalPerformer> Performers { get; }
        public IReadOnlyList<CarnivalShopOffer> ShopOffers { get; }
        public IReadOnlyList<CarnivalConsumableState> Consumables { get; }
        public IReadOnlyList<CarnivalConsumable> BoosterChoices { get; }
        public CarnivalBlind CurrentBlind { get; }
        public CarnivalBoosterPack CurrentBoosterPack { get; }
        public CarnivalBoosterPack OpenedBoosterPack { get; }
        public CarnivalBlindTag CurrentBlindTag { get; }
        public bool IsBoosterOpen { get; }
        public CarnivalRunPhase Phase { get; }
        public CarnivalScoreState LastResult { get; }
        public int Ante { get; }
        public int RoundScore { get; }
        public int TargetScore { get; }
        public int HandsRemaining { get; }
        public int DiscardsRemaining { get; }
        public int Money { get; }
        public int CardsInDeck { get; }
        public int PerformerSlotLimit { get; }
        public int RerollCost { get; }
        public int BoosterPackCost { get; }
        public int DoubleTagCount { get; }
        public int TagsCollectedThisRun { get; }
        public IReadOnlyList<int> PerformerSellValues { get; }
        public string StatusMessage { get; }
        private HashSet<int> SelectedCardIds { get; }
        private Dictionary<CarnivalCardEnhancement, string> EnhancementDescriptions { get; }
        private Dictionary<string, int> OfferCosts { get; }

        public int GetOfferCost(string offerId)
        {
            return OfferCosts.TryGetValue(offerId, out int cost) ? cost : 0;
        }

        public bool IsSelected(int cardId)
        {
            return SelectedCardIds.Contains(cardId);
        }

        public string GetEnhancementDescription(CarnivalCardEnhancement enhancement)
        {
            return EnhancementDescriptions.TryGetValue(enhancement, out string description)
                ? description
                : enhancement.ToString();
        }

        private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> source)
        {
            var copy = new List<T>(source.Count);
            for (int i = 0; i < source.Count; i++)
                copy.Add(source[i]);
            return copy.AsReadOnly();
        }

        private static IReadOnlyList<CarnivalConsumableState> CopyConsumables(
            IReadOnlyList<CarnivalConsumableState> source)
        {
            var copy = new List<CarnivalConsumableState>(source.Count);
            foreach (CarnivalConsumableState consumable in source)
            {
                copy.Add(new CarnivalConsumableState(
                    consumable.Content,
                    consumable.Edition,
                    consumable.SellValue,
                    consumable.RuntimeId));
            }

            return copy.AsReadOnly();
        }
    }

    /// <summary>
    /// 单次计分结果的只读快照。
    /// </summary>
    public sealed class CarnivalScoreState
    {
        public CarnivalScoreState(CarnivalScoreResult source)
        {
            Kind = source.Kind;
            HandName = source.HandName;
            Chips = source.Chips;
            Multiplier = source.Multiplier;
            Score = source.Score;
            Breakdown = new List<string>(source.Breakdown).AsReadOnly();
        }

        public CarnivalHandKind Kind { get; }
        public string HandName { get; }
        public int Chips { get; }
        public float Multiplier { get; }
        public int Score { get; }
        public IReadOnlyList<string> Breakdown { get; }
    }
}
