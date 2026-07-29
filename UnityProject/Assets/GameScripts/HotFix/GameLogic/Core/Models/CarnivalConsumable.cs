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
            CarnivalHandKind? handKind = null)
        {
            Id = id;
            Name = name;
            Family = family;
            Description = description;
            Cost = cost;
            HandKind = handKind;
        }

        public string Id { get; }
        public string Name { get; }
        public CarnivalConsumableFamily Family { get; }
        public string Description { get; }
        public int Cost { get; }
        public CarnivalHandKind? HandKind { get; }
    }
}
