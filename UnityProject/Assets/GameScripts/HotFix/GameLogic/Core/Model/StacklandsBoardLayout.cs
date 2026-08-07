using System;
using System.Collections.Generic;
using System.Linq;

namespace GameLogic.Core.Model
{
    /// <summary>
    /// 牌桌布局解算器：以牌堆与卡包为实体，按牌面 AABB 解算重叠。
    /// 任何来源造成的重叠（拖放被拒、开包/月亮事件生成、读档等）都在 Tick 末尾统一顶开，
    /// 各业务路径不再分别处理。刚创建或刚移动的实体（LastActiveRevision 较高）给静止实体让路；
    /// 交战中的敌我单位牌堆豁免，以免打断敌对单位的追击合堆。
    /// 视图侧由 CardView 的平滑跟随把解算位移呈现为滑开动画。
    /// </summary>
    internal static class StacklandsBoardLayout
    {
        // 牌面世界尺寸（图集 150×200px、PPU 100）与分离间隙。
        internal const float CardWidth = 1.5f;
        internal const float CardHeight = 2.0f;
        private const float SeparationMargin = 0.1f;
        private const int MaxIterations = 4;

        private sealed class Entity
        {
            internal List<CardRunData> Cards;
            internal BoosterRunData Booster;
            internal float X;
            internal float Y;
            internal int Revision;
            internal int Order;
        }

        /// <summary>解算一次当前局的重叠；返回是否有实体被移动。</summary>
        internal static bool ResolveOverlaps(StacklandsGameModel model)
        {
            StacklandsRunData run = model.Run;
            if (run == null) return false;
            List<Entity> entities = BuildEntities(run);
            bool moved = false;
            for (int iteration = 0; iteration < MaxIterations; iteration++)
            {
                bool passMoved = false;
                for (int i = 0; i < entities.Count; i++)
                for (int j = i + 1; j < entities.Count; j++)
                    passMoved |= Separate(model, entities[i], entities[j]);
                moved |= passMoved;
                if (!passMoved) break;
            }
            if (!moved) return false;
            foreach (Entity entity in entities) Apply(entity);
            return true;
        }

        private static List<Entity> BuildEntities(StacklandsRunData run)
        {
            var entities = new List<Entity>();
            int order = 0;
            foreach (IGrouping<string, CardRunData> group in run.Cards.GroupBy(card => card.StackId))
            {
                List<CardRunData> cards = group.ToList();
                entities.Add(new Entity
                {
                    Cards = cards, X = cards[0].X, Y = cards[0].Y,
                    Revision = cards.Max(card => card.LastActiveRevision), Order = order++,
                });
            }
            foreach (BoosterRunData booster in run.Boosters)
            {
                entities.Add(new Entity
                {
                    Booster = booster, X = booster.X, Y = booster.Y,
                    Revision = booster.LastActiveRevision, Order = order++,
                });
            }
            return entities;
        }

        /// <summary>沿穿透较浅的轴把让路实体推出重叠区并留间隙；返回是否发生移动。</summary>
        private static bool Separate(StacklandsGameModel model, Entity a, Entity b)
        {
            float overlapX = CardWidth - Math.Abs(a.X - b.X);
            float overlapY = CardHeight - Math.Abs(a.Y - b.Y);
            if (overlapX <= 0f || overlapY <= 0f) return false;
            if (IsCombatPair(model, a, b)) return false;

            Entity moving = a.Revision != b.Revision
                ? (a.Revision > b.Revision ? a : b)
                : (a.Order > b.Order ? a : b);
            Entity stationary = ReferenceEquals(moving, a) ? b : a;
            if (overlapX <= overlapY)
            {
                float direction = moving.X == stationary.X
                    ? ((moving.Order & 1) == 0 ? 1f : -1f)
                    : Math.Sign(moving.X - stationary.X);
                moving.X += direction * (overlapX + SeparationMargin);
            }
            else
            {
                float direction = moving.Y == stationary.Y
                    ? ((moving.Order & 1) == 0 ? 1f : -1f)
                    : Math.Sign(moving.Y - stationary.Y);
                moving.Y += direction * (overlapY + SeparationMargin);
            }
            return true;
        }

        /// <summary>敌我单位分属两堆时视为交战追击：不解算，交由 WorldCtrl 合堆触发战斗。</summary>
        private static bool IsCombatPair(StacklandsGameModel model, Entity a, Entity b)
        {
            if (a.Cards == null || b.Cards == null) return false;
            bool hostileA = a.Cards.Any(card => model.IsHostile(card));
            bool hostileB = b.Cards.Any(card => model.IsHostile(card));
            if (hostileA == hostileB) return false;
            bool unitA = a.Cards.Any(card => model.Content.Units.Contains(card.CardId));
            bool unitB = b.Cards.Any(card => model.Content.Units.Contains(card.CardId));
            return unitA && unitB;
        }

        private static void Apply(Entity entity)
        {
            if (entity.Cards != null)
            {
                foreach (CardRunData card in entity.Cards)
                {
                    card.X = entity.X;
                    card.Y = entity.Y;
                }
            }
            else
            {
                entity.Booster.X = entity.X;
                entity.Booster.Y = entity.Y;
            }
        }
    }
}
