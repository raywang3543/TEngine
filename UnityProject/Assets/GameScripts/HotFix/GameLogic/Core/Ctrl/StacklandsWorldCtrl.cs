using System;
using System.Collections.Generic;
using System.Linq;
using GameLogic.Core.Model;

namespace GameLogic.Core.Ctrl
{
    /// <summary>
    /// Moon、喂食、卡牌上限、移动、传送门和商车事件控制器。
    /// </summary>
    internal sealed class StacklandsWorldCtrl
    {
        private StacklandsGameModel Model => CoreSystem.Model;

        internal void TickMovement(float delta)
        {
            bool changed = false;
            List<CardRunData> friends = Model.Run.Cards.Where(card =>
                Model.Content.Units.Contains(card.CardId) && !Model.IsHostile(card)).ToList();
            if (friends.Count == 0) return;
            foreach (CardRunData hostile in Model.Run.Cards.Where(card =>
                         Model.Content.Units.Contains(card.CardId) && Model.IsHostile(card)).ToList())
            {
                if (Model.StackCards(hostile.StackId).Any(card =>
                        !Model.IsHostile(card) && Model.Content.Units.Contains(card.CardId))) continue;
                CardRunData target = friends.OrderBy(card => DistanceSquared(card, hostile)).First();
                float dx = target.X - hostile.X, dy = target.Y - hostile.Y;
                float length = (float)Math.Sqrt(dx * dx + dy * dy);
                if (length < 1.2f)
                {
                    hostile.StackId = target.StackId;
                    hostile.StackOrder = Model.StackCards(target.StackId).Count;
                    hostile.X = target.X; hostile.Y = target.Y; changed = true;
                }
                else if (length > 0.001f)
                {
                    float step = Math.Min(length, 0.45f * delta);
                    hostile.X += dx / length * step; hostile.Y += dy / length * step; changed = true;
                }
            }
            if (!changed) return;
            Model.MovementPublishDelay -= delta;
            if (Model.MovementPublishDelay <= 0f)
            {
                Model.MovementPublishDelay = 0.1f;
                CoreSystem.ViewCtrl.PublishBoard();
            }
        }

        internal void EndMoon()
        {
            List<CardRunData> units = Model.Run.Cards.Where(card => Model.Content.Units.Contains(card.CardId))
                .OrderBy(FeedPriority).ToList();
            int foodAvailable = Model.CurrentFood();
            int spent = 0;
            foreach (CardRunData unitCard in units)
            {
                int need = Model.Content.Units.Get(unitCard.CardId).FoodPerMoon.GetValueOrDefault();
                if (foodAvailable - spent >= need) spent += need;
                else CoreSystem.CombatCtrl.Kill(unitCard);
            }
            Model.ConsumeFood(spent);
            if (Model.CurrentCardCount() > Model.CurrentCardCap())
            {
                Model.Run.AwaitingCardLimit = true; Model.Run.Speed = 0f;
                CoreSystem.RequestFlow(new FlowRequest
                    { Kind = StacklandsFlowKind.CardLimit, Title = StacklandsTexts.CardLimitTitle, Message = StacklandsTexts.CardLimitMessage });
            }
            else BeginNextMoon();
            CoreSystem.RunCtrl.SaveNow();
            CoreSystem.ViewCtrl.PublishAll();
        }

        internal void BeginNextMoon()
        {
            Model.Run.AwaitingCardLimit = false; Model.Run.Moon++;
            Model.Run.MoonRemaining = Model.Run.MoonDuration; Model.Run.Speed = 1f;
            Model.Increment("ReachMoon:moon");
            SpawnMoonEvents();
            CoreSystem.QuestCtrl.Evaluate();
            CoreSystem.CombatCtrl.CheckGameOver();
            Model.Changed();
        }

        internal List<string> RollPortalThreat(string poolId, bool rare)
        {
            int cappedMoon = Math.Min(Model.Run.Moon, Model.Content.WorldRules.ThreatCapMoon);
            int intervals = Math.Max(0, (cappedMoon - Model.Content.WorldRules.PortalStartMoon) /
                                        Math.Max(1, Model.Content.WorldRules.PortalInterval));
            int budget = Model.Content.WorldRules.PortalBaseThreat +
                         intervals * Model.Content.WorldRules.PortalThreatPerInterval;
            if (rare) budget = (int)Math.Ceiling(budget * Model.Content.WorldRules.RarePortalMultiplier);
            var results = new List<string>();
            int spent = 0;
            for (int guard = 0; guard < 32 && spent < budget; guard++)
            {
                string cardId = CoreSystem.LootCtrl.RollPool(poolId).FirstOrDefault();
                if (string.IsNullOrEmpty(cardId)) break;
                results.Add(cardId);
                spent += Model.Content.Units.Contains(cardId)
                    ? Math.Max(1, Model.Content.Units.Get(cardId).CombatLevel.GetValueOrDefault(1)) : 1;
            }
            return results;
        }

        private void SpawnMoonEvents()
        {
            WorldRules rules = Model.Content.WorldRules;
            if (!Model.Run.Peaceful && Model.Run.Moon >= rules.PortalStartMoon &&
                (Model.Run.Moon - rules.PortalStartMoon) % rules.PortalInterval == 0)
            {
                int index = (Model.Run.Moon - rules.PortalStartMoon) / rules.PortalInterval + 1;
                CardRunData portal = Model.AddCard(
                    index % rules.RarePortalFrequency == 0 ? "rare_portal" : "strange_portal", 5f, 2f, false);
                CoreSystem.WorkCtrl.TryStartAction(portal.StackId);
            }
            if (Model.Run.Moon >= rules.CartStartMoon && (Model.Run.Moon == rules.CartGuaranteeMoon ||
                (Model.Run.Moon % 2 == 1 && Model.Random.NextFloat() < rules.CartChance)) &&
                Model.CountCard("travelling_cart") == 0)
            {
                CardRunData cart = Model.AddCard("travelling_cart", 4f, -2f, false);
                CoreSystem.WorkCtrl.TryStartAction(cart.StackId);
            }
        }

        private static float DistanceSquared(CardRunData a, CardRunData b)
        {
            float x = a.X - b.X, y = a.Y - b.Y;
            return x * x + y * y;
        }

        private static int FeedPriority(CardRunData card) => card.CardId == "baby" ? 0 :
            card.CardId == "dog" ? 1 : 2;
    }
}
