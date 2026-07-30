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
            CarnivalCardSeal seal,
            CarnivalCardEdition edition,
            CarnivalHandKind? handKind,
            CarnivalSuit? suit,
            CarnivalConsumableFamily? createdFamily,
            string rarity,
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
            Seal = seal;
            Edition = edition;
            HandKind = handKind;
            Suit = suit;
            CreatedFamily = createdFamily;
            Rarity = rarity;
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
        public CarnivalCardSeal Seal { get; }
        public CarnivalCardEdition Edition { get; }
        public CarnivalHandKind? HandKind { get; }
        public CarnivalSuit? Suit { get; }
        public CarnivalConsumableFamily? CreatedFamily { get; }
        public string Rarity { get; }
        public int Amount { get; }
        public int SecondaryAmount { get; }
        public bool BoolValue { get; }
    }
}
