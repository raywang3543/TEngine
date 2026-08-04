using System;
using System.Collections.Generic;
using System.Linq;
using GameLogic.Core.Model;

namespace GameLogic.Core.Ctrl
{
    /// <summary>
    /// 配方匹配、卡牌动作、消耗方式与里程碑控制器。
    /// </summary>
    internal sealed class StacklandsWorkCtrl
    {
        private StacklandsGameModel Model => CoreSystem.Model;

        internal bool TryStartRecipe(string stackId)
        {
            if (string.IsNullOrEmpty(stackId) || Model.Run.Works.Any(work => work.StackId == stackId)) return false;
            List<CardRunData> cards = Model.StackCards(stackId);
            RecipeDefinition recipe = Model.Content.Recipes.All.OrderByDescending(item => item.Priority)
                .FirstOrDefault(item => Matches(cards, item.Requirements, item.AllowExtraCards));
            if (recipe == null) return false;
            float duration = recipe.DurationSeconds ?? 0f;
            Model.Run.Works.Add(new WorkRunData
            {
                Id = Model.NewId("work"), DefinitionId = recipe.Id, IsRecipe = true, StackId = stackId,
                Remaining = duration, Duration = Math.Max(duration, 0.01f),
                CardIds = cards.Select(item => item.InstanceId).ToList(),
            });
            Model.Changed();
            return true;
        }

        internal bool TryStartAction(string stackId)
        {
            if (string.IsNullOrEmpty(stackId) || Model.Run.Works.Any(work => work.StackId == stackId)) return false;
            List<CardRunData> cards = Model.StackCards(stackId);
            foreach (CardRunData source in cards)
            {
                if (!Model.ActionsByCard.TryGetValue(source.CardId, out var actions)) continue;
                CardActionDefinition action = actions.FirstOrDefault(item => item.Type != CardActionKind.Defeat &&
                    MatchesAction(cards, source, item));
                if (action == null) continue;
                if (action.Type == CardActionKind.Summon)
                {
                    CoreSystem.RequestFlow(new FlowRequest
                    {
                        Kind = StacklandsFlowKind.SummonDemon, Title = "召唤恶魔？",
                        Message = "仪式开始后将消耗 Golden Goblet（金杯）。", InstanceId = source.InstanceId,
                    });
                    return true;
                }
                StartAction(action, cards, stackId);
                return true;
            }
            return false;
        }

        internal void StartSummonAction(string sourceInstanceId)
        {
            CardRunData source = Model.GetCard(sourceInstanceId);
            if (source == null || !Model.ActionsByCard.TryGetValue(source.CardId, out var actions)) return;
            List<CardRunData> cards = Model.StackCards(source.StackId);
            CardActionDefinition action = actions.FirstOrDefault(item => item.Type == CardActionKind.Summon &&
                MatchesAction(cards, source, item));
            if (action != null) StartAction(action, cards, source.StackId);
        }

        internal void Tick(float delta)
        {
            for (int i = Model.Run.Works.Count - 1; i >= 0; i--)
            {
                WorkRunData work = Model.Run.Works[i];
                if (work.CardIds.Any(id => Model.GetCard(id) == null))
                {
                    Model.Run.Works.RemoveAt(i); continue;
                }
                work.Remaining -= delta;
                if (work.Remaining > 0f) continue;
                Model.Run.Works.RemoveAt(i);
                if (work.IsRecipe) CompleteRecipe(work); else CompleteAction(work);
            }
        }

        private void StartAction(CardActionDefinition action, List<CardRunData> cards, string stackId)
        {
            float duration = action.Duration ?? action.RepeatInterval ?? 0f;
            CardRunData source = cards.FirstOrDefault(item => item.CardId == action.SourceCardId);
            if (source != null && (source.CardId == "strange_portal" || source.CardId == "rare_portal"))
                duration = Model.Content.WorldRules.PortalDelay;
            CardRunData worker = cards.FirstOrDefault(item => item != source && Model.Content.Units.Contains(item.CardId));
            if (worker != null)
            {
                float speed = Model.Content.Units.Get(worker.CardId).WorkSpeed.GetValueOrDefault(1f);
                if (!string.IsNullOrEmpty(worker.EquipmentCardId) && Model.Content.Equipment.Contains(worker.EquipmentCardId))
                    speed *= Model.Content.Equipment.Get(worker.EquipmentCardId).WorkSpeedMultiplier;
                duration /= Math.Max(0.1f, speed);
            }
            Model.Run.Works.Add(new WorkRunData
            {
                Id = Model.NewId("work"), DefinitionId = action.Id, IsRecipe = false, StackId = stackId,
                Remaining = duration, Duration = Math.Max(duration, 0.01f),
                CardIds = cards.Select(item => item.InstanceId).ToList(),
            });
            Model.Changed();
        }

        private void CompleteRecipe(WorkRunData work)
        {
            RecipeDefinition recipe = Model.Content.Recipes.Get(work.DefinitionId);
            List<CardRunData> cards = work.CardIds.Select(Model.GetCard).Where(item => item != null).ToList();
            if (!Matches(cards, recipe.Requirements, recipe.AllowExtraCards)) return;
            CardRunData anchor = cards[0];
            ConsumeRequirements(cards, recipe.Requirements);
            foreach (CardAmountDefinition result in recipe.Results)
                for (int i = 0; i < result.Count; i++)
                {
                    CardRunData card = Model.AddCard(result.CardId, anchor.X, anchor.Y, true);
                    TryStartAction(card.StackId);
                }
            Model.Increment("EventCount:recipe:" + recipe.Id);
            CoreSystem.QuestCtrl.Evaluate();
            Model.Changed();
        }

        private void CompleteAction(WorkRunData work)
        {
            CardActionDefinition action = Model.Content.Actions.Get(work.DefinitionId);
            CardRunData source = work.CardIds.Select(Model.GetCard)
                .FirstOrDefault(item => item?.CardId == action.SourceCardId);
            if (source == null) return;
            List<CardRunData> cards = work.CardIds.Select(Model.GetCard).Where(item => item != null).ToList();
            ConsumeRequirements(cards.Where(item => item != source).ToList(), action.Requirements);
            List<string> results = string.IsNullOrEmpty(action.LootPoolId) ? new List<string>() :
                source.CardId == "strange_portal" || source.CardId == "rare_portal"
                    ? CoreSystem.WorldCtrl.RollPortalThreat(action.LootPoolId, source.CardId == "rare_portal")
                    : CoreSystem.LootCtrl.RollPool(action.LootPoolId);
            source.Uses++;
            ApplyMilestone(action, source, results);
            foreach (string result in results)
            {
                CardRunData card = Model.AddCard(result, source.X, source.Y - 0.6f, true);
                TryStartAction(card.StackId);
            }
            if (action.Type == CardActionKind.Explore) Model.Increment("LocationExplored:" + source.CardId);
            Model.Increment("EventCount:action:" + action.Id);
            if (action.DestroySource && (!action.MaxUses.HasValue || source.Uses >= action.MaxUses.Value))
                Model.RemoveCard(source);
            CoreSystem.QuestCtrl.Evaluate();
            if (Model.GetCard(source.InstanceId) != null) TryStartAction(source.StackId);
            Model.Changed();
        }

        private bool Matches(List<CardRunData> cards, IReadOnlyList<CardRequirementDefinition> requirements,
            bool allowExtra)
        {
            int required = 0;
            foreach (CardRequirementDefinition requirement in requirements)
            {
                int count = cards.Count(card => RequirementMatches(card, requirement));
                if (count < requirement.Count) return false;
                required += requirement.Count;
            }
            return allowExtra || cards.Count == required;
        }

        private bool MatchesAction(List<CardRunData> cards, CardRunData source, CardActionDefinition action)
        {
            if (!Matches(cards.Where(card => card != source).ToList(), action.Requirements, true)) return false;
            if (action.Worker == WorkerKind.None) return true;
            return cards.Any(card => card != source && WorkerMatches(card, action.Worker));
        }

        private bool RequirementMatches(CardRunData card, CardRequirementDefinition requirement)
        {
            if (requirement.Matcher == "CARD") return card.CardId == requirement.CardId;
            CardDefinition definition = Model.Content.Cards.Get(card.CardId);
            return definition.Tags.Any(tag => string.Equals(tag, requirement.Tag, StringComparison.OrdinalIgnoreCase));
        }

        private bool WorkerMatches(CardRunData card, WorkerKind kind)
        {
            CardDefinition definition = Model.Content.Cards.Get(card.CardId);
            string tag = kind.ToString().ToUpperInvariant();
            return definition.Tags.Contains(tag) || kind == WorkerKind.Worker && definition.Tags.Contains("WORKER");
        }

        private void ConsumeRequirements(List<CardRunData> cards,
            IReadOnlyList<CardRequirementDefinition> requirements)
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (CardRequirementDefinition requirement in requirements)
            {
                int remaining = requirement.Count;
                foreach (CardRunData card in cards.Where(item => !used.Contains(item.InstanceId) &&
                             RequirementMatches(item, requirement)).ToList())
                {
                    used.Add(card.InstanceId);
                    if (requirement.ConsumeMode == "CONSUME" || requirement.ConsumeMode == "TRANSFORM")
                        Model.RemoveCard(card);
                    if (--remaining == 0) break;
                }
            }
        }

        private void ApplyMilestone(CardActionDefinition action, CardRunData source, List<string> results)
        {
            if (string.IsNullOrEmpty(action.MilestoneGroupId) ||
                !Model.MilestonesByGroup.TryGetValue(action.MilestoneGroupId, out var milestones)) return;
            MilestoneDefinition milestone = milestones.FirstOrDefault(item => item.TriggerCount == source.Uses &&
                !(item.Once == OnceKind.Run && Model.Run.GrantedOnce.Contains(item.Id)) &&
                !(item.Once == OnceKind.Profile && Model.Profile.GrantedOnce.Contains(item.Id)));
            if (milestone == null) return;
            if (milestone.ReplaceRandomResult) results.Clear();
            for (int i = 0; i < milestone.OutputCount; i++) results.Add(milestone.OutputCardId);
            if (milestone.Once == OnceKind.Run) Model.Run.GrantedOnce.Add(milestone.Id);
            if (milestone.Once == OnceKind.Profile) Model.Profile.GrantedOnce.Add(milestone.Id);
            if (milestone.DestroySource) Model.RemoveCard(source);
        }
    }
}
