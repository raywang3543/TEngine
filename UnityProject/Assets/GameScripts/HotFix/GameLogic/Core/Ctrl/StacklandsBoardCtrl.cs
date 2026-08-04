using System.Collections.Generic;
using System.Linq;
using GameLogic.Core.Model;

namespace GameLogic.Core.Ctrl
{
    /// <summary>
    /// 牌桌移动、牌堆、出售和装备控制器。
    /// </summary>
    internal sealed class StacklandsBoardCtrl
    {
        private StacklandsGameModel Model => CoreSystem.Model;

        internal void Move(string instanceId, string targetId, float x, float y, bool wholeStack)
        {
            CardRunData card = Model.GetCard(instanceId);
            if (card == null) return;
            List<CardRunData> moving = wholeStack
                ? Model.Run.Cards.Where(item => item.StackId == card.StackId).OrderBy(item => item.StackOrder).ToList()
                : new List<CardRunData> { card };
            Model.CancelWorks(moving.Select(item => item.InstanceId));
            CardRunData target = Model.GetCard(targetId);
            if (target == null)
            {
                string newStack = Model.NewId("stack");
                for (int i = 0; i < moving.Count; i++)
                {
                    moving[i].StackId = newStack; moving[i].StackOrder = i; moving[i].X = x; moving[i].Y = y;
                }
            }
            else
            {
                int count = Model.Run.Cards.Count(item => item.StackId == target.StackId && !moving.Contains(item));
                if (count + moving.Count > Model.Content.WorldRules.MaxStackSize)
                {
                    CoreSystem.Notify($"牌堆最多容纳 {Model.Content.WorldRules.MaxStackSize} 张卡");
                    return;
                }
                for (int i = 0; i < moving.Count; i++)
                {
                    moving[i].StackId = target.StackId; moving[i].StackOrder = count + i;
                    moving[i].X = target.X; moving[i].Y = target.Y;
                }
            }
            Model.NormalizeStacks();
            string stackId = moving[0].StackId;
            if (!TryEquipStack(stackId) && !CoreSystem.WorkCtrl.TryStartRecipe(stackId))
                CoreSystem.WorkCtrl.TryStartAction(stackId);
            Model.Increment("EventCount:drag_card");
            Model.Changed();
        }

        internal void Sell(string instanceId)
        {
            CardRunData card = Model.GetCard(instanceId);
            if (card == null) return;
            CardDefinition definition = Model.Content.Cards.Get(card.CardId);
            if (definition.IsSellable != true || definition.SellPrice.GetValueOrDefault() <= 0) return;
            int value = definition.SellPrice.Value *
                        (card.IsFoil ? (int)Model.Content.Boosters.All.First().FoilSellMultiplier : 1);
            float x = card.X; float y = card.Y;
            Model.RemoveCard(card);
            for (int i = 0; i < value; i++) Model.AddCard("coin", x, y, false);
            Model.Increment("EventCount:sell_card");
            if (Model.Run.AwaitingCardLimit && Model.CurrentCardCount() <= Model.CurrentCardCap())
                CoreSystem.WorldCtrl.BeginNextMoon();
            CoreSystem.QuestCtrl.Evaluate();
            Model.Changed();
        }

        internal void Equip(string equipmentId, string unitId)
        {
            CardRunData equipment = Model.GetCard(equipmentId);
            CardRunData unit = Model.GetCard(unitId);
            if (equipment == null || unit == null || !Model.Content.Equipment.Contains(equipment.CardId) ||
                !Model.Content.Units.Contains(unit.CardId)) return;
            if (!string.IsNullOrEmpty(unit.EquipmentCardId))
                Model.AddCard(unit.EquipmentCardId, unit.X + 0.5f, unit.Y, false);
            unit.EquipmentCardId = equipment.CardId;
            Model.RemoveCard(equipment);
            Model.Changed();
        }

        internal void Unequip(string unitId)
        {
            CardRunData unit = Model.GetCard(unitId);
            if (unit == null || string.IsNullOrEmpty(unit.EquipmentCardId)) return;
            Model.AddCard(unit.EquipmentCardId, unit.X + 0.5f, unit.Y, false);
            unit.EquipmentCardId = null;
            Model.Changed();
        }

        internal bool TryEquipStack(string stackId)
        {
            List<CardRunData> cards = Model.StackCards(stackId);
            CardRunData equipment = cards.FirstOrDefault(card => Model.Content.Equipment.Contains(card.CardId));
            CardRunData unit = cards.FirstOrDefault(card => Model.Content.Units.Contains(card.CardId));
            if (equipment == null || unit == null || equipment == unit) return false;
            Equip(equipment.InstanceId, unit.InstanceId);
            return true;
        }
    }
}
