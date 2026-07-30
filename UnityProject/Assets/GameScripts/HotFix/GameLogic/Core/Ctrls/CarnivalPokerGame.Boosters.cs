using System.Collections.Generic;

namespace GameLogic.Core
{
    public sealed partial class CarnivalPokerGame
    {
        public bool BuyBoosterPack()
        {
            if (Phase != CarnivalRunPhase.Shop ||
                IsBoosterOpen ||
                _currentBoosterPack == null)
            {
                return false;
            }

            int cost = GetBoosterPackCost();
            if (!CanAfford(cost))
            {
                StatusMessage = "金币不足，无法购买补充包。";
                return false;
            }

            Money -= cost;
            _openedBoosterPack = _currentBoosterPack;
            _currentBoosterPack = null;
            GenerateBoosterChoices(_openedBoosterPack);

            if (HasJoker("hallucination") && RollChance(2))
            {
                if (TryCreateConsumable(CarnivalConsumableFamily.Tarot))
                    StatusMessage = "幻觉触发：打开补充包时生成了 1 张塔罗牌。";
                else
                    StatusMessage = "幻觉触发，但消耗牌栏没有空位。";
            }
            else
            {
                StatusMessage = $"打开了「{_openedBoosterPack.Name}」，选择 1 张牌或跳过。";
            }

            return true;
        }

        public bool ChooseBoosterReward(string consumableId)
        {
            if (!IsBoosterOpen)
                return false;
            if (_consumables.Count >= MaxConsumables)
            {
                StatusMessage = $"消耗牌栏已满（最多 {MaxConsumables} 张）。";
                return false;
            }

            CarnivalConsumable selected = null;
            foreach (CarnivalConsumable consumable in _boosterChoices)
            {
                if (consumable.Id == consumableId)
                {
                    selected = consumable;
                    break;
                }
            }

            if (selected == null)
                return false;

            _consumables.Add(selected);
            string packName = _openedBoosterPack.Name;
            CloseBoosterPack();
            StatusMessage = $"从「{packName}」中获得了「{selected.Name}」。";
            return true;
        }

        public bool SkipBoosterPack()
        {
            if (!IsBoosterOpen)
                return false;

            CarnivalPerformer redCard = FindOwnedJoker("red_card");
            if (redCard != null)
                GetJokerState(redCard).Value += 3f;

            string packName = _openedBoosterPack.Name;
            CloseBoosterPack();
            StatusMessage = redCard == null
                ? $"跳过了「{packName}」。"
                : $"跳过了「{packName}」，红牌永久获得 +3 倍率。";
            return true;
        }

        public int GetBoosterPackCost()
        {
            if (_currentBoosterPack == null)
                return 0;
            if (_couponShopActive)
                return 0;
            if (_currentBoosterPack.Kind == CarnivalBoosterPackKind.Celestial &&
                HasJoker("astronomer"))
            {
                return 0;
            }

            return _currentBoosterPack.Cost;
        }

        private void GenerateBoosterPack()
        {
            switch (_random.Next(0, 3))
            {
                case 0:
                    _currentBoosterPack = new CarnivalBoosterPack(
                        "arcana_pack",
                        "秘术包",
                        CarnivalBoosterPackKind.Arcana,
                        CarnivalConsumableFamily.Tarot,
                        "从 3 张塔罗牌中选择 1 张。",
                        4,
                        3);
                    break;
                case 1:
                    _currentBoosterPack = new CarnivalBoosterPack(
                        "celestial_pack",
                        "天体包",
                        CarnivalBoosterPackKind.Celestial,
                        CarnivalConsumableFamily.Planet,
                        "从 3 张星球牌中选择 1 张。",
                        4,
                        3);
                    break;
                default:
                    _currentBoosterPack = new CarnivalBoosterPack(
                        "spectral_pack",
                        "幻灵包",
                        CarnivalBoosterPackKind.Spectral,
                        CarnivalConsumableFamily.Spectral,
                        "从 2 张幻灵牌中选择 1 张。",
                        4,
                        2);
                    break;
            }
        }

        private void GenerateBoosterChoices(CarnivalBoosterPack pack)
        {
            _boosterChoices.Clear();
            var candidates = new List<CarnivalConsumable>();
            foreach (CarnivalConsumable consumable in _contentModel.Consumables)
            {
                if (consumable.Family == pack.Family)
                    candidates.Add(consumable);
            }

            Shuffle(candidates);
            int count = System.Math.Min(pack.OfferCount, candidates.Count);
            for (int index = 0; index < count; index++)
                _boosterChoices.Add(candidates[index]);
        }

        private void CloseBoosterPack()
        {
            _openedBoosterPack = null;
            _boosterChoices.Clear();
        }
    }
}
