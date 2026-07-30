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
        IReadOnlyList<CarnivalConsumableState> Consumables { get; }
        IReadOnlyList<CarnivalConsumable> BoosterChoices { get; }
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
        int PerformerSlotLimit { get; }
        int RerollCost { get; }
        CarnivalBoosterPack CurrentBoosterPack { get; }
        CarnivalBoosterPack OpenedBoosterPack { get; }
        CarnivalBlindTag CurrentBlindTag { get; }
        bool IsBoosterOpen { get; }
        int DoubleTagCount { get; }
        int TagsCollectedThisRun { get; }
        string StatusMessage { get; }

        void StartNewRun();
        bool IsSelected(int cardId);
        string GetEnhancementDescription(CarnivalCardEnhancement enhancement);
        bool ToggleCard(int cardId);
        CarnivalScoreResult PlaySelected();
        bool DiscardSelected();
        bool BuyPerformer(string performerId);
        bool BuyConsumable(string consumableId);
        bool BuyBoosterPack();
        bool ChooseBoosterReward(string consumableId);
        bool SkipBoosterPack();
        int GetBoosterPackCost();
        bool RerollShop();
        bool SellPerformer(int performerIndex);
        bool SellConsumable(string consumableId);
        bool MovePerformer(int performerIndex, int direction);
        int GetPerformerSellValue(int performerIndex);
        int GetOfferCost(string offerId);
        bool UseConsumable(string consumableId);
        void ContinueFromShop();
        bool SkipBlind();
        void SortHandByRank();
        void SortHandBySuit();
    }
}
