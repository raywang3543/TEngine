namespace GameLogic.Core
{
    /// <summary>
    /// 商店中的补充包定义。
    /// </summary>
    public sealed class CarnivalBoosterPack
    {
        public CarnivalBoosterPack(
            string id,
            string name,
            CarnivalBoosterPackKind kind,
            CarnivalConsumableFamily family,
            string description,
            int cost,
            int offerCount)
        {
            Id = id;
            Name = name;
            Kind = kind;
            Family = family;
            Description = description;
            Cost = cost;
            OfferCount = offerCount;
        }

        public string Id { get; }
        public string Name { get; }
        public CarnivalBoosterPackKind Kind { get; }
        public CarnivalConsumableFamily Family { get; }
        public string Description { get; }
        public int Cost { get; }
        public int OfferCount { get; }
    }
}
