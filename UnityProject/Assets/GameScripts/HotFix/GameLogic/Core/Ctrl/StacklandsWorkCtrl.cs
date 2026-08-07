using System;
using System.Collections.Generic;
using System.Linq;
using GameLogic.Core.Model;

namespace GameLogic.Core.Ctrl
{
    /// <summary>
    /// 配方匹配、卡牌动作、工作终止、消耗方式与里程碑控制器。
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
                        Kind = StacklandsFlowKind.SummonDemon, Title = StacklandsTexts.SummonDemonTitle,
                        Message = StacklandsTexts.SummonDemonMessage, InstanceId = source.InstanceId,
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

        /// <summary>
        /// 终止涉及指定卡牌的全部工作进度。牌堆组成改变（堆叠、拆堆）时由 BoardCtrl 调用。
        /// </summary>
        internal void TerminateWorksInvolving(IEnumerable<string> cardIds)
        {
            var set = new HashSet<string>(cardIds, StringComparer.Ordinal);
            Model.Run.Works.RemoveAll(work => work.CardIds.Any(set.Contains));
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
                foreach (EquipmentDefinition equipment in CoreSystem.EquipmentCtrl.GetEquipment(worker))
                    speed *= equipment.WorkSpeedMultiplier;
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
            List<CardRunData> others = cards.Where(card => card != source).ToList();
            int required = 0;
            foreach (CardRequirementDefinition requirement in action.Requirements)
            {
                int count = others.Count(card => RequirementMatches(card, requirement));
                if (count < requirement.Count) return false;
                required += requirement.Count;
            }
            bool needsWorker = action.Worker != WorkerKind.None;
            if (needsWorker && !others.Any(card => WorkerMatches(card, action.Worker))) return false;
            // 牌堆中存在组合之外的多余卡牌时不触发动作：除原料外仅允许一张工人卡；
            // 工人卡本身已计入原料（如 Old Tome 研究由村民亲自研究）时不重复计数。
            bool workerCountsAsRequirement = needsWorker && action.Requirements.Any(requirement =>
                others.Any(card => WorkerMatches(card, action.Worker) && RequirementMatches(card, requirement)));
            return others.Count == required + (needsWorker && !workerCountsAsRequirement ? 1 : 0);
        }

        private bool RequirementMatches(CardRunData card, CardRequirementDefinition requirement)
            => StacklandsStackRules.RequirementMatches(Model.Content, card, requirement);

        private bool WorkerMatches(CardRunData card, WorkerKind kind)
            => StacklandsStackRules.WorkerMatches(Model.Content, card, kind);

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
