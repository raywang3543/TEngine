using System;
using System.Collections.Generic;

namespace GameLogic.Core
{
    /// <summary>
    /// 表演者与消耗牌的只读内容目录。
    /// </summary>
    public sealed class CarnivalContentModel : ICarnivalContentModel
    {
        private static readonly CarnivalPerformer[] StartingPerformerCatalog =
        {
            new CarnivalPerformer(
                "red-ribbons",
                "红绸舞者",
                "红绸",
                "每张计分红心给予 +3 倍率。",
                4,
                "普通"),
            new CarnivalPerformer(
                "pocket-confetti",
                "口袋彩屑",
                "彩屑",
                "打出不超过 3 张牌时给予 +24 筹码。",
                4,
                "普通"),
            new CarnivalPerformer(
                "club-lantern",
                "梅花提灯",
                "提灯",
                "每张计分梅花给予 +5 倍率。",
                6,
                "稀有"),
            new CarnivalPerformer(
                "mirror-duet",
                "镜面二重奏",
                "镜面",
                "牌型包含对子时，最终倍率 ×2。",
                8,
                "稀有"),
            new CarnivalPerformer(
                "street-runner",
                "高跷跑者",
                "跑者",
                "每次打出顺子永久获得 +12 筹码。",
                7,
                "稀有"),
            new CarnivalPerformer(
                "diamond-register",
                "钻石收银机",
                "收银",
                "每张计分方块获得 $1。",
                6,
                "稀有"),
            new CarnivalPerformer(
                "late-finale",
                "压轴面具",
                "压轴",
                "本回合最后一次出牌时，最终倍率 ×2.5。",
                9,
                "史诗"),
            new CarnivalPerformer(
                "full-tent",
                "满座帐篷",
                "满座",
                "每拥有一张表演者卡给予 +4 倍率。",
                7,
                "稀有"),
            new CarnivalPerformer(
                "odd-acrobat",
                "奇数杂技团",
                "奇数",
                "每张计分 A、3、5、7、9 给予 +18 筹码。",
                6,
                "稀有"),
        };

        private static readonly CarnivalConsumable[] StartingConsumableCatalog =
        {
            new CarnivalConsumable(
                "tarot-forge",
                "锻金秘仪",
                CarnivalConsumableFamily.Tarot,
                "将最多 2 张所选牌强化为奖励牌（计分时 +30 筹码）。",
                3),
            new CarnivalConsumable(
                "tarot-mask",
                "百相秘仪",
                CarnivalConsumableFamily.Tarot,
                "将最多 3 张所选牌强化为万能花色。",
                3),
            new CarnivalConsumable(
                "tarot-rise",
                "升格秘仪",
                CarnivalConsumableFamily.Tarot,
                "将最多 2 张所选牌的点数提高 1（A 回到 2）。",
                3),
            new CarnivalConsumable(
                "planet-pair",
                "双星",
                CarnivalConsumableFamily.Planet,
                "永久提升「对子」的基础筹码与倍率。",
                3,
                CarnivalHandKind.Pair),
            new CarnivalConsumable(
                "planet-straight",
                "长轨星",
                CarnivalConsumableFamily.Planet,
                "永久提升「顺子」的基础筹码与倍率。",
                3,
                CarnivalHandKind.Straight),
            new CarnivalConsumable(
                "planet-flush",
                "潮汐星",
                CarnivalConsumableFamily.Planet,
                "永久提升「同花」的基础筹码与倍率。",
                3,
                CarnivalHandKind.Flush),
            new CarnivalConsumable(
                "spectral-glass",
                "棱镜幽影",
                CarnivalConsumableFamily.Spectral,
                "将 1 张所选牌变为玻璃牌（计分倍率 ×2，随后有概率碎裂）。",
                4),
            new CarnivalConsumable(
                "spectral-echo",
                "群星回声",
                CarnivalConsumableFamily.Spectral,
                "随机升级 2 种牌型，但失去 $3。",
                4),
            new CarnivalConsumable(
                "spectral-void",
                "虚空契约",
                CarnivalConsumableFamily.Spectral,
                "将整手牌变成同一点数，但本盲注少 1 次出牌。",
                5),
        };

        private static readonly string[] PerformerTitles =
        {
            "月影", "星火", "绯幕", "银铃", "黑羽",
            "流光", "迷雾", "金线", "夜航", "晨露",
            "幻彩", "深海", "雷鸣", "霜花", "余烬",
        };

        private static readonly string[] PerformerRoles =
        {
            "魔术师", "驯兽师", "空中飞人", "默剧演员", "提线师",
            "占卜师", "鼓手", "火舞者", "逃脱师", "报幕人",
        };

        private static readonly CarnivalPerformer[] PerformerCatalog = BuildPerformerCatalog();
        private static readonly CarnivalConsumable[] ConsumableCatalog = BuildConsumableCatalog();

        private static CarnivalPerformer[] BuildPerformerCatalog()
        {
            const int targetCount = 150;
            var catalog = new List<CarnivalPerformer>(targetCount);
            for (int index = 0; index < StartingPerformerCatalog.Length; index++)
            {
                CarnivalPerformer performer = StartingPerformerCatalog[index];
                catalog.Add(new CarnivalPerformer(
                    performer.Id,
                    performer.Name,
                    performer.ShortName,
                    performer.Description,
                    performer.Cost,
                    ResolveRarity(index),
                    performer.Effect,
                    performer.EffectValue,
                    performer.HandKind,
                    performer.Suit));
            }

            for (int index = catalog.Count; index < targetCount; index++)
                catalog.Add(CreatePerformer(index));

            return catalog.ToArray();
        }

        private static CarnivalPerformer CreatePerformer(int index)
        {
            string title = PerformerTitles[index / PerformerRoles.Length];
            string role = PerformerRoles[index % PerformerRoles.Length];
            string name = title + role;
            string rarity = ResolveRarity(index);
            int cost = ResolveCost(rarity, index);
            int profile = index % 10;
            int value = 8 + (index % 5) * 4;
            CarnivalHandKind handKind = (CarnivalHandKind)(index % 12);
            CarnivalSuit suit = (CarnivalSuit)(index % 4);

            switch (profile)
            {
                case 0:
                    return NewPerformer(index, name, role, $"+{value * 3} 筹码。", cost, rarity,
                        CarnivalPerformerEffect.FlatChips, value * 3);
                case 1:
                    return NewPerformer(index, name, role, $"+{value / 2} 倍率。", cost, rarity,
                        CarnivalPerformerEffect.FlatMultiplier, value / 2);
                case 2:
                    float multiplier = 1.25f + index % 4 * 0.25f;
                    return NewPerformer(index, name, role, $"最终倍率 ×{multiplier:0.##}。", cost, rarity,
                        CarnivalPerformerEffect.MultiplyMultiplier, multiplier);
                case 3:
                    return NewPerformer(index, name, role, $"{HandName(handKind)}牌型 +{value * 4} 筹码。", cost,
                        rarity, CarnivalPerformerEffect.HandChips, value * 4, handKind);
                case 4:
                    return NewPerformer(index, name, role, $"{HandName(handKind)}牌型 +{value / 2} 倍率。", cost,
                        rarity, CarnivalPerformerEffect.HandMultiplier, value / 2, handKind);
                case 5:
                    return NewPerformer(index, name, role,
                        $"每张计分{SuitName(suit)}给予 +{value} 筹码。", cost, rarity,
                        CarnivalPerformerEffect.SuitChipsPerCard, value, null, suit);
                case 6:
                    return NewPerformer(index, name, role,
                        $"每张计分{SuitName(suit)}给予 +{Math.Max(2, value / 4)} 倍率。", cost, rarity,
                        CarnivalPerformerEffect.SuitMultiplierPerCard, Math.Max(2, value / 4), null, suit);
                case 7:
                    return NewPerformer(index, name, role,
                        $"每张计分奇数牌给予 +{value} 筹码。", cost, rarity,
                        CarnivalPerformerEffect.OddRankChipsPerCard, value);
                case 8:
                    return NewPerformer(index, name, role,
                        $"每张计分人头牌给予 +{Math.Max(2, value / 4)} 倍率。", cost, rarity,
                        CarnivalPerformerEffect.FaceMultiplierPerCard, Math.Max(2, value / 4));
                default:
                    return NewPerformer(index, name, role,
                        "每张计分牌获得 $1。", cost, rarity,
                        CarnivalPerformerEffect.MoneyPerScoringCard, 1);
            }
        }

        private static CarnivalPerformer NewPerformer(
            int index,
            string name,
            string shortName,
            string description,
            int cost,
            string rarity,
            CarnivalPerformerEffect effect,
            float value,
            CarnivalHandKind? handKind = null,
            CarnivalSuit? suit = null)
        {
            return new CarnivalPerformer(
                $"performer-{index + 1:000}",
                name,
                shortName,
                description,
                cost,
                rarity,
                effect,
                value,
                handKind,
                suit);
        }

        private static string ResolveRarity(int index)
        {
            if (index < 61)
                return "普通";
            if (index < 125)
                return "罕见";
            if (index < 145)
                return "稀有";
            return "传说";
        }

        private static int ResolveCost(string rarity, int index)
        {
            int variance = index % 3;
            switch (rarity)
            {
                case "传说":
                    return 12 + variance;
                case "稀有":
                    return 8 + variance;
                case "罕见":
                    return 5 + variance;
                default:
                    return 3 + variance;
            }
        }

        private static CarnivalConsumable[] BuildConsumableCatalog()
        {
            var catalog = new List<CarnivalConsumable>(52);
            catalog.AddRange(StartingConsumableCatalog);

            AddTarotCards(catalog);
            AddPlanetCards(catalog);
            AddSpectralCards(catalog);
            return catalog.ToArray();
        }

        private static void AddTarotCards(List<CarnivalConsumable> catalog)
        {
            string[] names =
            {
                "焰纹秘仪", "鎏金秘仪", "钢骨秘仪", "幸运秘仪", "净化秘仪",
                "丰收秘仪", "星图秘仪", "红幕秘仪", "黑幕秘仪", "方晶秘仪",
                "花冠秘仪", "镜像秘仪", "裁断秘仪", "命运秘仪", "聚光秘仪",
                "谢幕秘仪", "新生秘仪", "巡游秘仪", "世界秘仪",
            };
            string[] descriptions =
            {
                "将最多 2 张所选牌强化为倍率牌。",
                "将 1 张所选牌强化为黄金牌。",
                "将 1 张所选牌强化为钢铁牌。",
                "将 1 张所选牌强化为幸运牌。",
                "移除最多 2 张所选牌的强化。",
                "立即获得 $8。",
                "随机提升 2 种牌型。",
                "将最多 3 张所选牌转化为红心。",
                "将最多 3 张所选牌转化为黑桃。",
                "将最多 3 张所选牌转化为方块。",
                "将最多 3 张所选牌转化为梅花。",
                "复制 1 张所选牌的点数与花色到另一张牌。",
                "摧毁最多 2 张所选牌。",
                "随机改变所选牌的点数。",
                "使最多 2 张所选牌降低 1 点。",
                "出售换取 $5。",
                "随机生成 1 张消耗牌。",
                "本盲注恢复 1 次弃牌。",
                "本盲注恢复 1 次出牌。",
            };

            for (int i = 0; i < names.Length; i++)
            {
                catalog.Add(new CarnivalConsumable(
                    $"tarot-{i + 4:00}",
                    names[i],
                    CarnivalConsumableFamily.Tarot,
                    descriptions[i],
                    3));
            }
        }

        private static void AddPlanetCards(List<CarnivalConsumable> catalog)
        {
            CarnivalHandKind[] kinds =
            {
                CarnivalHandKind.HighCard,
                CarnivalHandKind.TwoPair,
                CarnivalHandKind.ThreeOfAKind,
                CarnivalHandKind.FullHouse,
                CarnivalHandKind.FourOfAKind,
                CarnivalHandKind.StraightFlush,
                CarnivalHandKind.FiveOfAKind,
                CarnivalHandKind.FlushHouse,
                CarnivalHandKind.FlushFive,
            };
            string[] names =
            {
                "独曜星", "双环星", "三冠星", "满月星", "四极星",
                "虹桥星", "五芒星", "合潮星", "完美星",
            };

            for (int i = 0; i < kinds.Length; i++)
            {
                catalog.Add(new CarnivalConsumable(
                    $"planet-{i + 4:00}",
                    names[i],
                    CarnivalConsumableFamily.Planet,
                    $"永久提升「{HandName(kinds[i])}」的基础筹码与倍率。",
                    3,
                    kinds[i]));
            }
        }

        private static void AddSpectralCards(List<CarnivalConsumable> catalog)
        {
            string[] names =
            {
                "赤焰幻灵", "玄冰幻灵", "鎏金幻灵", "钢心幻灵", "幸运幻灵",
                "荒石幻灵", "血月幻灵", "双生幻灵", "断舍幻灵", "丰饶幻灵",
                "逆位幻灵", "王冠幻灵", "深渊幻灵", "灵魂幻灵", "黑洞幻灵",
            };
            string[] descriptions =
            {
                "强化所选牌，并随机提升一种牌型。",
                "强化所选牌，但失去 $2。",
                "将 1 张所选牌强化为黄金牌。",
                "将 1 张所选牌强化为钢铁牌。",
                "将 1 张所选牌强化为幸运牌。",
                "将 1 张所选牌化为高筹码石牌。",
                "将整手牌转为红色花色，但失去 $3。",
                "复制 1 张所选牌的点数。",
                "摧毁最多 3 张所选牌并提升一种牌型。",
                "获得 $10，但本盲注少 1 次弃牌。",
                "随机改变整手牌的点数。",
                "将所有人头牌强化为倍率牌。",
                "随机升级 3 种牌型，但失去全部金币。",
                "随机获得一位传说表演者。",
                "所有牌型各提升 1 级。",
            };

            for (int i = 0; i < names.Length; i++)
            {
                catalog.Add(new CarnivalConsumable(
                    $"spectral-{i + 4:00}",
                    names[i],
                    CarnivalConsumableFamily.Spectral,
                    descriptions[i],
                    4 + i % 2));
            }
        }

        private static string HandName(CarnivalHandKind kind)
        {
            switch (kind)
            {
                case CarnivalHandKind.Pair:
                    return "对子";
                case CarnivalHandKind.TwoPair:
                    return "两对";
                case CarnivalHandKind.ThreeOfAKind:
                    return "三条";
                case CarnivalHandKind.Straight:
                    return "顺子";
                case CarnivalHandKind.Flush:
                    return "同花";
                case CarnivalHandKind.FullHouse:
                    return "葫芦";
                case CarnivalHandKind.FourOfAKind:
                    return "四条";
                case CarnivalHandKind.StraightFlush:
                    return "同花顺";
                case CarnivalHandKind.FiveOfAKind:
                    return "五条";
                case CarnivalHandKind.FlushHouse:
                    return "同花葫芦";
                case CarnivalHandKind.FlushFive:
                    return "同花五条";
                default:
                    return "高牌";
            }
        }

        private static string SuitName(CarnivalSuit suit)
        {
            switch (suit)
            {
                case CarnivalSuit.Hearts:
                    return "红心";
                case CarnivalSuit.Diamonds:
                    return "方块";
                case CarnivalSuit.Clubs:
                    return "梅花";
                default:
                    return "黑桃";
            }
        }

        public IReadOnlyList<CarnivalPerformer> Performers => PerformerCatalog;
        public IReadOnlyList<CarnivalConsumable> Consumables => ConsumableCatalog;

        public CarnivalPerformer FindPerformer(string performerId)
        {
            foreach (CarnivalPerformer performer in PerformerCatalog)
            {
                if (performer.Id == performerId)
                    return performer;
            }

            throw new InvalidOperationException($"Unknown performer: {performerId}");
        }

        internal void Release()
        {
        }
    }
}
