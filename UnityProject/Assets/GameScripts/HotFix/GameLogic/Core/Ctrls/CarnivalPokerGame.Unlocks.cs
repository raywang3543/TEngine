using System.Collections.Generic;

namespace GameLogic.Core
{
    public sealed partial class CarnivalPokerGame
    {
        /// <summary>
        /// 由存档恢复流程调用，记录“从主菜单继续牌局”的原版解锁条件。
        /// </summary>
        public void NotifyRunContinued()
        {
            _unlockModel.Statistics.ContinuedSavedRun = true;
            _unlockModel.UnlockJoker("throwback");
        }

        private void RecordHandForUnlocks(
            IReadOnlyList<CarnivalCard> playedCards,
            CarnivalScoreResult result)
        {
            CarnivalUnlockStatistics statistics = _unlockModel.Statistics;
            statistics.TotalHandsPlayed++;
            if (statistics.TotalHandsPlayed >= 200)
                _unlockModel.UnlockJoker("acrobat");

            int faceCards = 0;
            int clubSevens = 0;
            bool onlyGoldCards = playedCards.Count == 5;
            foreach (CarnivalCard card in playedCards)
            {
                if (card.IsFace)
                    faceCards++;
                if (card.Rank == 7 && card.Suit == CarnivalSuit.Clubs)
                    clubSevens++;
                if (card.Enhancement != CarnivalCardEnhancement.Gold)
                    onlyGoldCards = false;
                if (card.Suit == CarnivalSuit.Hearts)
                    _heartCardsPlayedThisRound.Add(card.Id);
            }

            statistics.TotalFaceCardsPlayed += faceCards;
            if (statistics.TotalFaceCardsPlayed >= 300)
                _unlockModel.UnlockJoker("sock_and_buskin");
            if (onlyGoldCards)
                _unlockModel.UnlockJoker("ticket");
            if (clubSevens >= 4)
                _unlockModel.UnlockJoker("seeing_double");
            if (result.Score >= 10000)
                _unlockModel.UnlockJoker("oops");
            if (result.Score >= 1000000)
                _unlockModel.UnlockJoker("idol");
            if (result.Score >= 100000000)
                _unlockModel.UnlockJoker("stuntman");

            _playedHandKindsThisRun.Add(result.Kind);
            EvaluateDeckUnlocks();
            EvaluateMoneyUnlocks();
        }

        private void RecordDiscardForUnlocks(IReadOnlyList<CarnivalCard> discardedCards)
        {
            int jackCount = 0;
            bool hasTen = false;
            bool hasJack = false;
            bool hasQueen = false;
            bool hasKing = false;
            bool hasAce = false;
            CarnivalSuit? royalSuit = null;
            bool sameSuit = true;
            foreach (CarnivalCard card in discardedCards)
            {
                if (card.Rank == 11)
                    jackCount++;
                hasTen |= card.Rank == 10;
                hasJack |= card.Rank == 11;
                hasQueen |= card.Rank == 12;
                hasKing |= card.Rank == 13;
                hasAce |= card.Rank == 14;
                if (!royalSuit.HasValue)
                    royalSuit = card.Suit;
                else if (card.Suit != royalSuit.Value)
                    sameSuit = false;
            }

            if (jackCount >= 5)
                _unlockModel.UnlockJoker("hit_the_road");
            if (discardedCards.Count == 5 &&
                sameSuit &&
                hasTen &&
                hasJack &&
                hasQueen &&
                hasKing &&
                hasAce)
            {
                _unlockModel.UnlockJoker("brainstorm");
            }
        }

        private void RecordCardSoldForUnlocks(bool joker)
        {
            CarnivalUnlockStatistics statistics = _unlockModel.Statistics;
            statistics.TotalCardsSold++;
            if (joker)
            {
                statistics.TotalJokersSold++;
                if (statistics.TotalJokersSold >= 20)
                    _unlockModel.UnlockJoker("swashbuckler");
            }

            if (statistics.TotalCardsSold >= 50)
                _unlockModel.UnlockJoker("burnt");
            EvaluateMoneyUnlocks();
        }

        private void RecordBlindDefeatedForUnlocks(CarnivalScoreResult result)
        {
            CarnivalUnlockStatistics statistics = _unlockModel.Statistics;
            if (_handsPlayedThisRound == 1)
                statistics.ConsecutiveOneHandBlindWins++;
            else
                statistics.ConsecutiveOneHandBlindWins = 0;
            if (statistics.ConsecutiveOneHandBlindWins >= 5)
                _unlockModel.UnlockJoker("troubadour");

            if (CurrentBlind.Tier == CarnivalBlindTier.Boss)
            {
                if (result.Kind == CarnivalHandKind.HighCard)
                    _unlockModel.UnlockJoker("hanging_chad");
                if (_handsPlayedThisRound == 1 && _discardsUsedThisRound == 0)
                    _unlockModel.UnlockJoker("matador");
            }

            if (AllHeartCardsWerePlayedThisRound())
                _unlockModel.UnlockJoker("shoot_the_moon");
            EvaluateMoneyUnlocks();
        }

        private void RecordRunWonForUnlocks()
        {
            _unlockModel.UnlockJoker("blueprint");
            if (Round <= 18)
                _unlockModel.UnlockJoker("wee");
            if (Round <= 12)
                _unlockModel.UnlockJoker("merry_andy");
            if (!_playedHandKindsThisRun.Contains(CarnivalHandKind.Pair))
                _unlockModel.UnlockJoker("duo");
            if (!_playedHandKindsThisRun.Contains(CarnivalHandKind.ThreeOfAKind))
                _unlockModel.UnlockJoker("trio");
            if (!_playedHandKindsThisRun.Contains(CarnivalHandKind.FourOfAKind))
                _unlockModel.UnlockJoker("family");
            if (!_playedHandKindsThisRun.Contains(CarnivalHandKind.Straight))
                _unlockModel.UnlockJoker("order");
            if (!_playedHandKindsThisRun.Contains(CarnivalHandKind.Flush))
                _unlockModel.UnlockJoker("tribe");
            if (_neverExceededFourJokers)
                _unlockModel.UnlockJoker("invisible");
        }

        private void RecordRunLostForUnlocks()
        {
            CarnivalUnlockStatistics statistics = _unlockModel.Statistics;
            statistics.RunsLost++;
            if (statistics.RunsLost >= 5)
                _unlockModel.UnlockJoker("mr_bones");
        }

        private void RecordConsumableDiscovered(CarnivalConsumable consumable)
        {
            if (consumable == null)
                return;

            CarnivalUnlockStatistics statistics = _unlockModel.Statistics;
            if (consumable.Family == CarnivalConsumableFamily.Tarot)
                statistics.DiscoveredTarotIds.Add(consumable.Id);
            else if (consumable.Family == CarnivalConsumableFamily.Planet)
                statistics.DiscoveredPlanetIds.Add(consumable.Id);

            int tarotCount = CountConsumableDefinitions(CarnivalConsumableFamily.Tarot);
            int planetCount = CountConsumableDefinitions(CarnivalConsumableFamily.Planet);
            if (statistics.DiscoveredTarotIds.Count >= tarotCount)
                _unlockModel.UnlockJoker("cartomancer");
            if (statistics.DiscoveredPlanetIds.Count >= planetCount)
                _unlockModel.UnlockJoker("astronomer");
        }

        private int CountConsumableDefinitions(CarnivalConsumableFamily family)
        {
            int count = 0;
            foreach (CarnivalConsumable consumable in _contentModel.Consumables)
            {
                if (consumable.Family == family)
                    count++;
            }

            return count;
        }

        private void EvaluateAnteUnlocks()
        {
            if (Ante >= 4)
                _unlockModel.UnlockJoker("ring_master");
            if (Ante >= 8)
                _unlockModel.UnlockJoker("flower_pot");
        }

        private void EvaluateMoneyUnlocks()
        {
            if (Money >= 400)
                _unlockModel.UnlockJoker("satellite");
        }

        private void EvaluateDeckUnlocks()
        {
            int wildCards = 0;
            int glassCards = 0;
            int enhancedCards = 0;
            int diamonds = 0;
            int hearts = 0;
            int spades = 0;
            int clubs = 0;
            bool goldWithGoldSeal = false;
            CountDeckUnlockCards(
                _deck,
                ref wildCards,
                ref glassCards,
                ref enhancedCards,
                ref diamonds,
                ref hearts,
                ref spades,
                ref clubs,
                ref goldWithGoldSeal);
            CountDeckUnlockCards(
                _hand,
                ref wildCards,
                ref glassCards,
                ref enhancedCards,
                ref diamonds,
                ref hearts,
                ref spades,
                ref clubs,
                ref goldWithGoldSeal);
            CountDeckUnlockCards(
                _discardPile,
                ref wildCards,
                ref glassCards,
                ref enhancedCards,
                ref diamonds,
                ref hearts,
                ref spades,
                ref clubs,
                ref goldWithGoldSeal);

            if (goldWithGoldSeal)
                _unlockModel.UnlockJoker("certificate");
            if (wildCards >= 3)
                _unlockModel.UnlockJoker("smeared");
            if (diamonds >= 30)
                _unlockModel.UnlockJoker("rough_gem");
            if (hearts >= 30)
                _unlockModel.UnlockJoker("bloodstone");
            if (spades >= 30)
                _unlockModel.UnlockJoker("arrowhead");
            if (clubs >= 30)
                _unlockModel.UnlockJoker("onyx_agate");
            if (glassCards >= 5)
                _unlockModel.UnlockJoker("glass");
            if (enhancedCards >= 16)
                _unlockModel.UnlockJoker("drivers_license");

            int polychromeJokers = 0;
            foreach (CarnivalJokerState state in _jokerStates.Values)
            {
                if (state.Edition == CarnivalCardEdition.Polychrome)
                    polychromeJokers++;
            }

            if (polychromeJokers >= 2)
                _unlockModel.UnlockJoker("bootstraps");
        }

        private static void CountDeckUnlockCards(
            List<CarnivalCard> cards,
            ref int wildCards,
            ref int glassCards,
            ref int enhancedCards,
            ref int diamonds,
            ref int hearts,
            ref int spades,
            ref int clubs,
            ref bool goldWithGoldSeal)
        {
            foreach (CarnivalCard card in cards)
            {
                if (card.Enhancement == CarnivalCardEnhancement.Wild)
                    wildCards++;
                if (card.Enhancement == CarnivalCardEnhancement.Glass)
                    glassCards++;
                if (card.Enhancement != CarnivalCardEnhancement.None)
                    enhancedCards++;
                diamonds += card.Suit == CarnivalSuit.Diamonds ? 1 : 0;
                hearts += card.Suit == CarnivalSuit.Hearts ? 1 : 0;
                spades += card.Suit == CarnivalSuit.Spades ? 1 : 0;
                clubs += card.Suit == CarnivalSuit.Clubs ? 1 : 0;
                goldWithGoldSeal |= card.Enhancement == CarnivalCardEnhancement.Gold &&
                                    card.Seal == CarnivalCardSeal.Gold;
            }
        }

        private bool AllHeartCardsWerePlayedThisRound()
        {
            int heartsInDeck = CountCards(card => card.Suit == CarnivalSuit.Hearts);
            return heartsInDeck > 0 && _heartCardsPlayedThisRound.Count >= heartsInDeck;
        }
    }
}
