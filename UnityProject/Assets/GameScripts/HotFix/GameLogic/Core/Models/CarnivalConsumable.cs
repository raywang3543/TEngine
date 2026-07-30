namespace GameLogic.Core
{
    /// <summary>
    /// 消耗牌定义。
    /// </summary>
    public sealed class CarnivalConsumable
    {
        public CarnivalConsumable(
            string id,
            string name,
            CarnivalConsumableFamily family,
            string description,
            int cost,
            CarnivalConsumableAction action,
            int maxSelected,
            CarnivalCardEnhancement enhancement,
            CarnivalHandKind? handKind,
            CarnivalSuit? suit,
            int amount,
            int secondaryAmount,
            bool boolValue)
        {
            Id = id;
            Name = name;
            Family = family;
            Description = description;
            Cost = cost;
            Action = action;
            MaxSelected = maxSelected;
            Enhancement = enhancement;
            HandKind = handKind;
            Suit = suit;
            Amount = amount;
            SecondaryAmount = secondaryAmount;
            BoolValue = boolValue;
        }

        public string Id { get; }
        public string Name { get; }
        public CarnivalConsumableFamily Family { get; }
        public string Description { get; }
        public int Cost { get; }
        public CarnivalConsumableAction Action { get; }
        public int MaxSelected { get; }
        public CarnivalCardEnhancement Enhancement { get; }
        public CarnivalHandKind? HandKind { get; }
        public CarnivalSuit? Suit { get; }
        public int Amount { get; }
        public int SecondaryAmount { get; }
        public bool BoolValue { get; }
    }
}
