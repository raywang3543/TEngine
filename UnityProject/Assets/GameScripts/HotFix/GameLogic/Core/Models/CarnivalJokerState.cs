using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GameLogic.Core
{
    /// <summary>
    /// 单张已拥有小丑牌在本局游戏中的可变状态。
    /// </summary>
    public sealed class CarnivalJokerState
    {
        public CarnivalJokerState(CarnivalPerformer performer)
        {
            if (performer == null)
                throw new ArgumentNullException(nameof(performer));

            SellValue = Math.Max(1, performer.Cost / 2);
            Value = ResolveInitialValue(performer.Id);
            Counter = ResolveInitialCounter(performer.Id);
        }

        public float Value { get; set; }
        public int Counter { get; set; }
        public int SecondaryCounter { get; set; }
        public int SellValue { get; set; }
        public int Rank { get; set; }
        public CarnivalSuit Suit { get; set; }
        public bool Active { get; set; }
        public CarnivalCardEdition Edition { get; set; }
        public bool Eternal { get; set; }
        public int PerishableRounds { get; set; }
        public bool Rental { get; set; }

        private static float ResolveInitialValue(string performerId)
        {
            switch (performerId)
            {
                case "ice_cream":
                    return 100f;
                case "popcorn":
                    return 20f;
                case "ramen":
                    return 2f;
                case "constellation":
                case "madness":
                case "vampire":
                case "hologram":
                case "lucky_cat":
                case "campfire":
                case "glass":
                case "hit_the_road":
                case "caino":
                case "yorick":
                    return 1f;
                case "cavendish":
                    return 3f;
                default:
                    return 0f;
            }
        }

        private static int ResolveInitialCounter(string performerId)
        {
            switch (performerId)
            {
                case "loyalty_card":
                    return 5;
                case "selzer":
                    return 10;
                case "turtle_bean":
                    return 5;
                default:
                    return 0;
            }
        }
    }

    /// <summary>
    /// 读取 Excel 中保存的原版 game.lua 配置参数。
    /// </summary>
    internal static class CarnivalJokerParameters
    {
        public static float GetFloat(CarnivalPerformer performer, string key, float defaultValue = 0f)
        {
            if (performer == null || string.IsNullOrEmpty(performer.Parameters))
                return defaultValue;

            Match match = Regex.Match(
                performer.Parameters,
                $@"\b{Regex.Escape(key)}\s*=\s*(-?\d+(?:\.\d+)?)",
                RegexOptions.CultureInvariant);
            if (!match.Success)
                return defaultValue;

            return float.TryParse(
                match.Groups[1].Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float value)
                ? value
                : defaultValue;
        }

        public static string GetString(CarnivalPerformer performer, string key, string defaultValue = "")
        {
            if (performer == null || string.IsNullOrEmpty(performer.Parameters))
                return defaultValue;

            Match match = Regex.Match(
                performer.Parameters,
                $@"\b{Regex.Escape(key)}\s*=\s*""([^""]*)""",
                RegexOptions.CultureInvariant);
            return match.Success ? match.Groups[1].Value : defaultValue;
        }
    }
}
