namespace GameLogic.Core
{
    public sealed partial class CarnivalPokerGame
    {
        private void GenerateBlindTag()
        {
            if (CurrentBlind.Tier == CarnivalBlindTier.Boss)
            {
                _currentBlindTag = null;
                return;
            }

            switch (_random.Next(0, 5))
            {
                case 0:
                    _currentBlindTag = new CarnivalBlindTag(
                        CarnivalBlindTagKind.Economy,
                        "经济标签",
                        "资金翻倍，最多获得 $40。");
                    break;
                case 1:
                    _currentBlindTag = new CarnivalBlindTag(
                        CarnivalBlindTagKind.Handy,
                        "便利标签",
                        "本赛局每打出过 1 手牌获得 $1。");
                    break;
                case 2:
                    _currentBlindTag = new CarnivalBlindTag(
                        CarnivalBlindTagKind.Investment,
                        "投资标签",
                        "击败下一个 Boss 盲注后获得 $25。");
                    break;
                case 3:
                    _currentBlindTag = new CarnivalBlindTag(
                        CarnivalBlindTagKind.Coupon,
                        "优惠券标签",
                        "下一个商店中的卡牌和补充包免费。");
                    break;
                default:
                    _currentBlindTag = new CarnivalBlindTag(
                        CarnivalBlindTagKind.D6,
                        "D6 标签",
                        "下一个商店获得 1 次免费重掷。");
                    break;
            }
        }

        private string CollectCurrentBlindTag()
        {
            if (_currentBlindTag == null)
                return string.Empty;

            CarnivalBlindTag collected = _currentBlindTag;
            int copies = 1 + _doubleTagCount;
            _doubleTagCount = 0;
            for (int index = 0; index < copies; index++)
                ApplyBlindTag(collected);
            _tagsCollectedThisRun += copies;
            _currentBlindTag = null;

            return copies == 1
                ? $"获得「{collected.Name}」"
                : $"双倍标签触发，「{collected.Name}」生效 {copies} 次";
        }

        private void ApplyBlindTag(CarnivalBlindTag tag)
        {
            switch (tag.Kind)
            {
                case CarnivalBlindTagKind.Economy:
                    Money += System.Math.Min(System.Math.Max(0, Money), 40);
                    break;
                case CarnivalBlindTagKind.Handy:
                    int handsPlayed = 0;
                    foreach (int count in _handPlayCounts.Values)
                        handsPlayed += count;
                    Money += handsPlayed;
                    break;
                case CarnivalBlindTagKind.Investment:
                    _investmentTagCount++;
                    break;
                case CarnivalBlindTagKind.Coupon:
                    _couponShopPending = true;
                    break;
                case CarnivalBlindTagKind.D6:
                    _d6TagPending = true;
                    break;
            }
        }
    }
}
