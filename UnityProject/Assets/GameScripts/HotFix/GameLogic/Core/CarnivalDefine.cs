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
    /// 牌局规则常量。
    /// </summary>
    public static class CarnivalDefine
    {
        public const int HandSize = 8;
        public const int MaxSelectedCards = 5;
        public const int MaxPerformers = 5;
        public const int FinalRound = 9;
        public const int MaxConsumables = 2;
    }
}
