namespace GameLogic.Core
{
    /// <summary>
    /// 商店中的统一商品。
    /// </summary>
    public sealed class CarnivalShopOffer
    {
        public CarnivalShopOffer(CarnivalPerformer performer)
        {
            Kind = CarnivalShopOfferKind.Performer;
            Performer = performer;
        }

        public CarnivalShopOffer(CarnivalConsumable consumable)
        {
            Kind = CarnivalShopOfferKind.Consumable;
            Consumable = consumable;
        }

        public CarnivalShopOfferKind Kind { get; }
        public CarnivalPerformer Performer { get; }
        public CarnivalConsumable Consumable { get; }
        public string Id => Kind == CarnivalShopOfferKind.Performer ? Performer.Id : Consumable.Id;
        public string Name => Kind == CarnivalShopOfferKind.Performer ? Performer.Name : Consumable.Name;
        public string Description =>
            Kind == CarnivalShopOfferKind.Performer ? Performer.Description : Consumable.Description;
        public int Cost => Kind == CarnivalShopOfferKind.Performer ? Performer.Cost : Consumable.Cost;
        public string Category => Kind == CarnivalShopOfferKind.Performer
            ? Performer.Rarity
            : Consumable.Family.ToString();
    }
}
