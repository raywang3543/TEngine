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
        FiveOfAKind,
        FlushHouse,
        FlushFive,
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
    /// 扑克牌强化类型。
    /// </summary>
    public enum CarnivalCardEnhancement
    {
        None,
        Bonus,
        Mult,
        Wild,
        Glass,
        Steel,
        Gold,
        Lucky,
        Stone,
    }

    /// <summary>
    /// 扑克牌封蜡。
    /// </summary>
    public enum CarnivalCardSeal
    {
        None,
        Gold,
        Red,
        Blue,
        Purple,
    }

    /// <summary>
    /// 卡牌版本。
    /// </summary>
    public enum CarnivalCardEdition
    {
        Base,
        Foil,
        Holographic,
        Polychrome,
        Negative,
    }

    /// <summary>
    /// 盲注阶段。
    /// </summary>
    public enum CarnivalBlindTier
    {
        Small,
        Big,
        Boss,
    }

    /// <summary>
    /// Boss 盲注规则。
    /// </summary>
    public enum CarnivalBossRule
    {
        None,
        FiveCardOnly,
        DebuffFaceCards,
        HalveBaseScore,
        LoseDiscard,
    }

    /// <summary>
    /// 消耗牌家族。
    /// </summary>
    public enum CarnivalConsumableFamily
    {
        Tarot,
        Planet,
        Spectral,
    }

    /// <summary>
    /// 数据驱动的表演者计分效果。
    /// </summary>
    public enum CarnivalPerformerEffect
    {
        Custom,
        BalatroOriginal,
        FlatChips,
        FlatMultiplier,
        MultiplyMultiplier,
        HandChips,
        HandMultiplier,
        SuitChipsPerCard,
        SuitMultiplierPerCard,
        OddRankChipsPerCard,
        FaceMultiplierPerCard,
        MoneyPerScoringCard,
        MaxPlayedCardsChips,
        PairFamilyMultiplyMultiplier,
        StraightPermanentChips,
        SuitMoneyPerCard,
        LastHandMultiplyMultiplier,
        PerformerCountMultiplier,
    }

    /// <summary>
    /// 消耗牌行为执行器。
    /// </summary>
    public enum CarnivalConsumableAction
    {
        EnhanceSelected,
        ShiftSelectedRanks,
        UpgradeHand,
        UpgradeRandomHands,
        UnifyHandRank,
        ChangeSelectedSuit,
        AddMoney,
        CopySelectedCard,
        DestroySelected,
        RandomizeSelectedRanks,
        CreateRandomConsumable,
        AddDiscards,
        AddHands,
        EnhanceAndUpgradeRandomHand,
        EnhanceAndMoney,
        ChangeWholeHandSuitAndMoney,
        DestroyAndUpgradeRandomHand,
        AddMoneyAndDiscards,
        RandomizeWholeHandRanks,
        EnhanceFaceCards,
        UpgradeRandomHandsAndClearMoney,
        AddRandomLegendaryPerformer,
        UpgradeAllHands,
    }

    /// <summary>
    /// 商店商品类型。
    /// </summary>
    public enum CarnivalShopOfferKind
    {
        Performer,
        Consumable,
    }

    /// <summary>
    /// 补充包类型。
    /// </summary>
    public enum CarnivalBoosterPackKind
    {
        Arcana,
        Celestial,
        Spectral,
    }

    /// <summary>
    /// 跳过盲注时可以获得的标签。
    /// </summary>
    public enum CarnivalBlindTagKind
    {
        Economy,
        Handy,
        Investment,
        Coupon,
        D6,
    }

    /// <summary>
    /// 牌局规则常量。
    /// </summary>
    public static class CarnivalDefine
    {
        public const int HandSize = 8;
        public const int MaxSelectedCards = 5;
        public const int MaxPerformers = 5;
        public const int FinalRound = 24;
        public const int MaxConsumables = 2;
    }
}
