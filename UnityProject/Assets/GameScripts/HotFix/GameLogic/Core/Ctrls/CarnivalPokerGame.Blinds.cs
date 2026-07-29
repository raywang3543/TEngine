namespace GameLogic.Core
{
    public sealed partial class CarnivalPokerGame
    {
        public void StartNewRun()
        {
            Round = 1;
            Money = 6;
            _runnerBonus = 0;
            _performers.Clear();
            _consumables.Clear();
            ResetHandLevels();
            _performers.Add(_contentModel.FindPerformer("red-ribbons"));
            _performers.Add(_contentModel.FindPerformer("pocket-confetti"));
            StartRound();
        }

        public bool SkipBlind()
        {
            if (Phase != CarnivalRunPhase.Playing || CurrentBlind.Tier == CarnivalBlindTier.Boss)
            {
                StatusMessage = "Boss 盲注不能跳过。";
                return false;
            }

            Money += 2;
            Round++;
            StartRound();
            StatusMessage = $"跳过盲注获得 $2。现在挑战「{CurrentBlind.Name}」。";
            return true;
        }

        private void ResetHandLevels()
        {
            _handLevels.Clear();
            _handLevels.Add(CarnivalHandKind.HighCard, new CarnivalHandLevel(5, 1f, 10, 1f));
            _handLevels.Add(CarnivalHandKind.Pair, new CarnivalHandLevel(20, 2f, 15, 1f));
            _handLevels.Add(CarnivalHandKind.TwoPair, new CarnivalHandLevel(30, 2f, 20, 1f));
            _handLevels.Add(CarnivalHandKind.ThreeOfAKind, new CarnivalHandLevel(30, 3f, 20, 2f));
            _handLevels.Add(CarnivalHandKind.Straight, new CarnivalHandLevel(35, 4f, 30, 3f));
            _handLevels.Add(CarnivalHandKind.Flush, new CarnivalHandLevel(35, 4f, 15, 2f));
            _handLevels.Add(CarnivalHandKind.FullHouse, new CarnivalHandLevel(40, 4f, 25, 2f));
            _handLevels.Add(CarnivalHandKind.FourOfAKind, new CarnivalHandLevel(60, 7f, 30, 3f));
            _handLevels.Add(CarnivalHandKind.StraightFlush, new CarnivalHandLevel(100, 8f, 40, 4f));
            _handLevels.Add(CarnivalHandKind.FiveOfAKind, new CarnivalHandLevel(120, 12f, 35, 3f));
            _handLevels.Add(CarnivalHandKind.FlushHouse, new CarnivalHandLevel(140, 14f, 40, 4f));
            _handLevels.Add(CarnivalHandKind.FlushFive, new CarnivalHandLevel(160, 16f, 50, 3f));
        }

        private CarnivalBlind CreateBlind(int round)
        {
            CarnivalBlindTier tier = (CarnivalBlindTier)((round - 1) % 3);
            if (tier == CarnivalBlindTier.Small)
                return new CarnivalBlind("小盲注", tier, CarnivalBossRule.None, 1f, 3, "基础目标，奖励较少。");
            if (tier == CarnivalBlindTier.Big)
                return new CarnivalBlind("大盲注", tier, CarnivalBossRule.None, 1.5f, 4, "更高的目标分数。");

            CarnivalBossRule[] rules =
            {
                CarnivalBossRule.FiveCardOnly,
                CarnivalBossRule.DebuffFaceCards,
                CarnivalBossRule.HalveBaseScore,
                CarnivalBossRule.LoseDiscard,
            };
            CarnivalBossRule rule = rules[(Ante - 1) % rules.Length];
            switch (rule)
            {
                case CarnivalBossRule.FiveCardOnly:
                    return new CarnivalBlind("Boss · 五幕", tier, rule, 2f, 6, "每手必须恰好打出 5 张牌。");
                case CarnivalBossRule.DebuffFaceCards:
                    return new CarnivalBlind("Boss · 无面王", tier, rule, 2f, 6, "J、Q、K 不提供筹码或强化效果。");
                case CarnivalBossRule.HalveBaseScore:
                    return new CarnivalBlind("Boss · 燧石幕", tier, rule, 2f, 6, "每手牌型的基础筹码与倍率减半。");
                default:
                    return new CarnivalBlind("Boss · 断弦", tier, rule, 2f, 6, "本盲注少 1 次弃牌机会。");
            }
        }
    }
}
