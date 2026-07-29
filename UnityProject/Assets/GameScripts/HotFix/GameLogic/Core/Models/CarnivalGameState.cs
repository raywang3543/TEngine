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
            Consumables = Copy(source.Consumables);
            CurrentBlind = source.CurrentBlind;
            Phase = source.Phase;
            LastResult = source.LastResult == null ? null : new CarnivalScoreState(source.LastResult);
            Ante = source.Ante;
            RoundScore = source.RoundScore;
            TargetScore = source.TargetScore;
            HandsRemaining = source.HandsRemaining;
            DiscardsRemaining = source.DiscardsRemaining;
            Money = source.Money;
            CardsInDeck = source.CardsInDeck;
            StatusMessage = source.StatusMessage;

            var selectedCardIds = new HashSet<int>();
            foreach (CarnivalCard card in source.Hand)
            {
                if (source.IsSelected(card.Id))
                    selectedCardIds.Add(card.Id);
            }

            SelectedCardIds = selectedCardIds;
        }

        public IReadOnlyList<CarnivalCard> Hand { get; }
        public IReadOnlyList<CarnivalPerformer> Performers { get; }
        public IReadOnlyList<CarnivalShopOffer> ShopOffers { get; }
        public IReadOnlyList<CarnivalConsumable> Consumables { get; }
        public CarnivalBlind CurrentBlind { get; }
        public CarnivalRunPhase Phase { get; }
        public CarnivalScoreState LastResult { get; }
        public int Ante { get; }
        public int RoundScore { get; }
        public int TargetScore { get; }
        public int HandsRemaining { get; }
        public int DiscardsRemaining { get; }
        public int Money { get; }
        public int CardsInDeck { get; }
        public string StatusMessage { get; }
        private HashSet<int> SelectedCardIds { get; }

        public bool IsSelected(int cardId)
        {
            return SelectedCardIds.Contains(cardId);
        }

        private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> source)
        {
            var copy = new List<T>(source.Count);
            for (int i = 0; i < source.Count; i++)
                copy.Add(source[i]);
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
