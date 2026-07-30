using GameConfig;
using Luban;
using System;
using System.Collections.Generic;

namespace GameLogic.Core
{
    /// <summary>
    /// 从 Luban 配置表构建表演者、消耗牌与强化效果的只读内容目录。
    /// </summary>
    public sealed class CarnivalContentModel : ICarnivalContentModel
    {
        private readonly CarnivalPerformer[] _performers;
        private readonly CarnivalConsumable[] _consumables;
        private readonly Dictionary<CarnivalCardEnhancement, CarnivalCardEnhancementContent> _enhancements;

        public CarnivalContentModel()
            : this(global::ConfigSystem.Instance.Tables)
        {
        }

        private CarnivalContentModel(Tables tables)
        {
            if (tables == null)
                throw new ArgumentNullException(nameof(tables));

            _performers = BuildPerformers(tables.TbPerformer.DataList);
            _consumables = BuildConsumables(tables.TbConsumable.DataList);
            _enhancements = BuildEnhancements(tables.TbCardEnhancement.DataList);
        }

        public IReadOnlyList<CarnivalPerformer> Performers => _performers;
        public IReadOnlyList<CarnivalConsumable> Consumables => _consumables;
        public IReadOnlyDictionary<CarnivalCardEnhancement, CarnivalCardEnhancementContent> Enhancements =>
            _enhancements;

        /// <summary>
        /// 从二进制配置加载器创建内容目录，供纯逻辑测试或独立工具使用。
        /// </summary>
        /// <param name="loader">按不带扩展名的配置文件名返回字节数据。</param>
        /// <returns>卡牌内容目录。</returns>
        public static CarnivalContentModel LoadFromBytes(Func<string, byte[]> loader)
        {
            if (loader == null)
                throw new ArgumentNullException(nameof(loader));

            return new CarnivalContentModel(new Tables(file => new ByteBuf(loader(file))));
        }

        public CarnivalPerformer FindPerformer(string performerId)
        {
            foreach (CarnivalPerformer performer in _performers)
            {
                if (performer.Id == performerId)
                    return performer;
            }

            throw new InvalidOperationException($"Unknown performer: {performerId}");
        }

        public CarnivalCardEnhancementContent FindEnhancement(CarnivalCardEnhancement enhancement)
        {
            if (_enhancements.TryGetValue(enhancement, out CarnivalCardEnhancementContent content))
                return content;

            throw new InvalidOperationException($"Unknown card enhancement: {enhancement}");
        }

        internal void Release()
        {
        }

        private static CarnivalPerformer[] BuildPerformers(
            IReadOnlyList<GameConfig.carnival.PerformerConfig> configs)
        {
            var performers = new CarnivalPerformer[configs.Count];
            for (int index = 0; index < configs.Count; index++)
            {
                GameConfig.carnival.PerformerConfig config = configs[index];
                performers[index] = new CarnivalPerformer(
                    config.Id,
                    config.Name,
                    config.ShortName,
                    config.Desc,
                    config.Cost,
                    config.Rarity,
                    config.Icon,
                    config.IsStarting,
                    ParseEnum<CarnivalPerformerEffect>(config.Effect, nameof(config.Effect), config.Id),
                    config.EffectValue,
                    config.ConditionValue,
                    ParseOptionalEnum<CarnivalHandKind>(config.HandKind, nameof(config.HandKind), config.Id),
                    ParseOptionalEnum<CarnivalSuit>(config.Suit, nameof(config.Suit), config.Id),
                    config.OriginalOrder,
                    config.NameEn,
                    config.DescEn,
                    config.UnlockRequirement,
                    config.UnlockedByDefault,
                    config.Activation,
                    config.JokerType,
                    config.BlueprintCompatible,
                    config.PerishableCompatible,
                    config.EternalCompatible,
                    config.Parameters);
            }

            return performers;
        }

        private static CarnivalConsumable[] BuildConsumables(
            IReadOnlyList<GameConfig.carnival.ConsumableConfig> configs)
        {
            var consumables = new CarnivalConsumable[configs.Count];
            for (int index = 0; index < configs.Count; index++)
            {
                GameConfig.carnival.ConsumableConfig config = configs[index];
                consumables[index] = new CarnivalConsumable(
                    config.Id,
                    config.Name,
                    ParseEnum<CarnivalConsumableFamily>(config.Family, nameof(config.Family), config.Id),
                    config.Desc,
                    config.Cost,
                    ParseEnum<CarnivalConsumableAction>(config.Action, nameof(config.Action), config.Id),
                    config.MaxSelected,
                    ParseEnum<CarnivalCardEnhancement>(
                        config.Enhancement,
                        nameof(config.Enhancement),
                        config.Id),
                    ParseEnum<CarnivalCardSeal>(config.Seal, nameof(config.Seal), config.Id),
                    ParseEnum<CarnivalCardEdition>(config.Edition, nameof(config.Edition), config.Id),
                    ParseOptionalEnum<CarnivalHandKind>(config.HandKind, nameof(config.HandKind), config.Id),
                    ParseOptionalEnum<CarnivalSuit>(config.Suit, nameof(config.Suit), config.Id),
                    ParseOptionalEnum<CarnivalConsumableFamily>(
                        config.CreatedFamily,
                        nameof(config.CreatedFamily),
                        config.Id),
                    config.Rarity,
                    config.Amount,
                    config.SecondaryAmount,
                    config.BoolValue);
            }

            return consumables;
        }

        private static Dictionary<CarnivalCardEnhancement, CarnivalCardEnhancementContent> BuildEnhancements(
            IReadOnlyList<GameConfig.carnival.CardEnhancementConfig> configs)
        {
            var enhancements =
                new Dictionary<CarnivalCardEnhancement, CarnivalCardEnhancementContent>(configs.Count);
            foreach (GameConfig.carnival.CardEnhancementConfig config in configs)
            {
                if (config.Kind != "Enhancement")
                    continue;

                CarnivalCardEnhancement id =
                    ParseEnum<CarnivalCardEnhancement>(config.Id, nameof(config.Id), config.Id);
                enhancements.Add(id, new CarnivalCardEnhancementContent(
                    id,
                    config.Name,
                    config.Desc,
                    config.Chips,
                    config.AdditiveMultiplier,
                    config.MultiplierFactor,
                    config.BreakChance,
                    config.HeldMultiplierFactor,
                    config.HeldMoney,
                    config.ChanceAdditiveMultiplier,
                    config.AdditiveMultiplierChance,
                    config.ChanceMoney,
                    config.MoneyChance,
                    config.AlwaysScores,
                    config.IgnoresRankSuit));
            }

            return enhancements;
        }

        private static T ParseEnum<T>(string value, string fieldName, string recordId)
            where T : struct
        {
            if (Enum.TryParse(value, false, out T result))
                return result;

            throw new InvalidOperationException(
                $"Invalid {typeof(T).Name} value '{value}' in {fieldName} for card '{recordId}'.");
        }

        private static T? ParseOptionalEnum<T>(string value, string fieldName, string recordId)
            where T : struct
        {
            if (string.IsNullOrEmpty(value) || value == "None")
                return null;

            return ParseEnum<T>(value, fieldName, recordId);
        }
    }
}
