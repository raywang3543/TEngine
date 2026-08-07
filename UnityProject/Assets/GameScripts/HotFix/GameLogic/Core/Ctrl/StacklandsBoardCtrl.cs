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
                ? Model.Run.Cards.Where(item => item.StackId == card.StackId)
                    .OrderBy(item => item.StackOrder).ToList()
                : new List<CardRunData> { card };
            CardRunData target = Model.GetCard(targetId);

            // 敌对单位不可拖动；整堆拖动只要含敌对单位（如交战中的混合牌堆）同样整体拒绝。
            if (moving.Any(Model.IsHostile))
            {
                CoreSystem.Notify(StacklandsTexts.NotifyHostileUndraggable);
                CoreSystem.ViewCtrl.PublishBoard();
                return;
            }

            // 整堆拖到空白处只是空间平移：牌堆组成不变，进行中的工作原样保留。
            if (wholeStack && target == null)
            {
                foreach (CardRunData movingCard in moving)
                {
                    movingCard.X = x;
                    movingCard.Y = y;
                }
                Model.Increment("EventCount:drag_card");
                Model.Changed();
                return;
            }

            string sourceStackId = card.StackId;
            if (!TryRestack(moving, target, x, y)) return;

            // 组成变化后，落入牌堆与源牌堆剩余卡牌（拆堆移走多余卡牌等场景）都按新组成重新评估。
            TryStartWork(moving[0].StackId);
            if (sourceStackId != moving[0].StackId && Model.StackCards(sourceStackId).Count > 0)
                TryStartWork(sourceStackId);
            Model.Increment("EventCount:drag_card");
            Model.Changed();
        }

        /// <summary>
        /// 把移动卡牌合并到目标牌堆，或拖到空白处独立成新堆。
        /// 堆叠前先按 StacklandsStackRules 判定被拖卡与目标卡是否兼容，再检查容量。
        /// 组成一旦改变，涉及这些卡牌的工作进度立即终止：允许向工作中的牌堆堆叠，代价是打断其进度。
        /// </summary>
        private bool TryRestack(List<CardRunData> moving, CardRunData target, float x, float y)
        {
            // 只允许有归并或交互关系的卡牌堆叠（同类、村民、装备、佩戴、敌我接触、配方/动作）。
            if (target != null && !StacklandsStackRules.CanStackOn(Model.Content, moving, target))
            {
                CoreSystem.Notify(StacklandsTexts.NotifyIncompatibleStack);
                CoreSystem.ViewCtrl.PublishBoard();
                return false;
            }

            List<CardRunData> targetStack = target == null
                ? new List<CardRunData>()
                : Model.Run.Cards.Where(item => item.StackId == target.StackId && !moving.Contains(item)).ToList();
            if (targetStack.Count + moving.Count > Model.Content.WorldRules.MaxStackSize)
            {
                CoreSystem.Notify(StacklandsTexts.NotifyStackCapacity(Model.Content.WorldRules.MaxStackSize));
                CoreSystem.ViewCtrl.PublishBoard();
                return false;
            }

            CoreSystem.WorkCtrl.TerminateWorksInvolving(moving.Concat(targetStack).Select(item => item.InstanceId));
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
                int count = targetStack.Count;
                for (int i = 0; i < moving.Count; i++)
                {
                    moving[i].StackId = target.StackId; moving[i].StackOrder = count + i;
                    moving[i].X = target.X; moving[i].Y = target.Y;
                }
            }
            Model.NormalizeStacks();
            return true;
        }

        private void TryStartWork(string stackId)
        {
            if (!CoreSystem.EquipmentCtrl.TryEquipStack(stackId) && !CoreSystem.WorkCtrl.TryStartRecipe(stackId))
                CoreSystem.WorkCtrl.TryStartAction(stackId);
        }

        internal void Sell(string instanceId)
        {
            CardRunData card = Model.GetCard(instanceId);
            if (card == null) return;
            CardDefinition definition = Model.Content.Cards.Get(card.CardId);
            if (definition.IsSellable != true) return;
            int value = definition.SellPrice.GetValueOrDefault() *
                        (card.IsFoil ? (int)Model.Content.Boosters.All.First().FoilSellMultiplier : 1);
            float x = card.X; float y = card.Y;
            Model.RemoveCard(card);
            for (int i = 0; i < value; i++) Model.AddCard(StacklandsGameModel.CurrencyCardId, x, y, false);
            Model.Increment("EventCount:sell_card");
            if (Model.Run.AwaitingCardLimit && Model.CurrentCardCount() <= Model.CurrentCardCap())
                CoreSystem.WorldCtrl.BeginNextMoon();
            CoreSystem.QuestCtrl.Evaluate();
            Model.Changed();
        }

    }
}
