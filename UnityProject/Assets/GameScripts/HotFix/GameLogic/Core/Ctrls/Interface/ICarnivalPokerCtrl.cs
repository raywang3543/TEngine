using System.Collections.Generic;

namespace GameLogic.Core
{
    /// <summary>
    /// 牌局控制器对外接口。
    /// </summary>
    public interface ICarnivalPokerCtrl
    {
        IReadOnlyList<CarnivalCard> Hand { get; }
        IReadOnlyList<CarnivalPerformer> Performers { get; }
        IReadOnlyList<CarnivalShopOffer> ShopOffers { get; }
        IReadOnlyList<CarnivalConsumable> Consumables { get; }
        IReadOnlyDictionary<CarnivalHandKind, CarnivalHandLevel> HandLevels { get; }
        CarnivalBlind CurrentBlind { get; }
        CarnivalRunPhase Phase { get; }
        CarnivalScoreResult LastResult { get; }
        int Round { get; }
        int Ante { get; }
        int RoundScore { get; }
        int TargetScore { get; }
        int HandsRemaining { get; }
        int DiscardsRemaining { get; }
        int Money { get; }
        int CardsInDeck { get; }
        string StatusMessage { get; }

        void StartNewRun();
        bool IsSelected(int cardId);
        bool ToggleCard(int cardId);
        CarnivalScoreResult PlaySelected();
        bool DiscardSelected();
        bool BuyPerformer(string performerId);
        bool BuyConsumable(string consumableId);
        bool UseConsumable(string consumableId);
        void ContinueFromShop();
        bool SkipBlind();
        void SortHandByRank();
        void SortHandBySuit();
    }
}
