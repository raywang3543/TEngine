using System;

namespace GameLogic.Core
{
    /// <summary>
    /// 一张标准扑克牌。
    /// </summary>
    public readonly struct CarnivalCard
    {
        public CarnivalCard(
            int id,
            CarnivalSuit suit,
            int rank,
            CarnivalCardEnhancement enhancement = CarnivalCardEnhancement.None,
            CarnivalCardSeal seal = CarnivalCardSeal.None,
            CarnivalCardEdition edition = CarnivalCardEdition.Base,
            int permanentChips = 0)
        {
            Id = id;
            Suit = suit;
            Rank = rank;
            Enhancement = enhancement;
            Seal = seal;
            Edition = edition;
            PermanentChips = permanentChips;
        }

        public int Id { get; }
        public CarnivalSuit Suit { get; }
        public int Rank { get; }
        public CarnivalCardEnhancement Enhancement { get; }
        public CarnivalCardSeal Seal { get; }
        public CarnivalCardEdition Edition { get; }
        public int PermanentChips { get; }
        public bool IsRed => Suit == CarnivalSuit.Hearts || Suit == CarnivalSuit.Diamonds;
        public bool IsFace => Rank >= 11 && Rank <= 13;
        public int ChipValue => (Rank == 14 ? 11 : Math.Min(Rank, 10)) + PermanentChips;

        public string RankText
        {
            get
            {
                switch (Rank)
                {
                    case 11:
                        return "J";
                    case 12:
                        return "Q";
                    case 13:
                        return "K";
                    case 14:
                        return "A";
                    default:
                        return Rank.ToString();
                }
            }
        }

        public string SuitText
        {
            get
            {
                switch (Suit)
                {
                    case CarnivalSuit.Spades:
                        return "♠";
                    case CarnivalSuit.Hearts:
                        return "♥";
                    case CarnivalSuit.Diamonds:
                        return "♦";
                    case CarnivalSuit.Clubs:
                        return "♣";
                    default:
                        return "?";
                }
            }
        }

        public CarnivalCard WithRank(int rank)
        {
            return new CarnivalCard(Id, Suit, rank, Enhancement, Seal, Edition, PermanentChips);
        }

        public CarnivalCard WithEnhancement(CarnivalCardEnhancement enhancement)
        {
            return new CarnivalCard(Id, Suit, Rank, enhancement, Seal, Edition, PermanentChips);
        }

        public CarnivalCard WithSuit(CarnivalSuit suit)
        {
            return new CarnivalCard(Id, suit, Rank, Enhancement, Seal, Edition, PermanentChips);
        }

        public CarnivalCard WithSeal(CarnivalCardSeal seal)
        {
            return new CarnivalCard(Id, Suit, Rank, Enhancement, seal, Edition, PermanentChips);
        }

        public CarnivalCard WithEdition(CarnivalCardEdition edition)
        {
            return new CarnivalCard(Id, Suit, Rank, Enhancement, Seal, edition, PermanentChips);
        }

        public CarnivalCard WithPermanentChips(int permanentChips)
        {
            return new CarnivalCard(Id, Suit, Rank, Enhancement, Seal, Edition, permanentChips);
        }
    }
}
