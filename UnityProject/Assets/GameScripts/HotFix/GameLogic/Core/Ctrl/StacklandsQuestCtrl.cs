using System.Linq;
using GameLogic.Core.Model;

namespace GameLogic.Core.Ctrl
{
    /// <summary>
    /// 任务指标、跨局完成状态和卡包解锁控制器。
    /// </summary>
    internal sealed class StacklandsQuestCtrl
    {
        private StacklandsGameModel Model => CoreSystem.Model;

        internal void Evaluate()
        {
            foreach (QuestDefinition quest in Model.Content.Quests.All.OrderBy(item => item.Order))
            {
                if (Model.Profile.CompletedQuests.Contains(quest.Id)) continue;
                bool complete = quest.Mode == ConditionModeKind.All
                    ? quest.Conditions.All(ConditionMet) : quest.Conditions.Any(ConditionMet);
                if (!complete) continue;
                Model.Profile.CompletedQuests.Add(quest.Id);
                CoreSystem.Notify(StacklandsTexts.NotifyQuestCompleted(quest.NameZh));
            }
        }

        private bool ConditionMet(QuestConditionDefinition condition)
        {
            int value;
            switch (condition.Metric)
            {
                case QuestMetricKind.CardCount: value = Model.CountCard(condition.CardId); break;
                case QuestMetricKind.TotalFood: value = Model.CurrentFood(); break;
                case QuestMetricKind.CoinCount: value = Model.CountCard(StacklandsGameModel.CurrencyCardId); break;
                case QuestMetricKind.IdeaDiscoveredCount:
                    value = Model.Profile.DiscoveredCards.Count(id => Model.Content.Cards.Contains(id) &&
                        Model.Content.Cards.Get(id).Category == "IDEA"); break;
                case QuestMetricKind.ReachMoon: value = Model.Run.Moon; break;
                case QuestMetricKind.PackUnlocked:
                    value = Model.Content.Boosters.Contains(condition.PackId) &&
                            Model.Profile.CompletedQuests.Count >=
                            Model.Content.Boosters.Get(condition.PackId).UnlockQuestCount ? 1 : 0; break;
                case QuestMetricKind.PackOpened: value = Model.Counter("PackOpened:" + condition.PackId); break;
                case QuestMetricKind.PackPurchased: value = Model.Counter("PackPurchased:" + condition.PackId); break;
                case QuestMetricKind.CardObtained: value = Model.Counter("CardObtained:" + condition.CardId); break;
                case QuestMetricKind.CardCreated: value = Model.Counter("CardCreated:" + condition.CardId); break;
                case QuestMetricKind.CardKilled: value = Model.Counter("CardKilled:" + condition.CardId); break;
                case QuestMetricKind.LocationExplored:
                    value = Model.Counter("LocationExplored:" + condition.CardId); break;
                case QuestMetricKind.StateCheck: value = StateCheck(condition.TargetId) ? 1 : 0; break;
                default: value = Model.Counter("EventCount:" + condition.TargetId); break;
            }
            return condition.Compare == CompareKind.Equal ? value == condition.Threshold :
                value >= condition.Threshold;
        }

        private bool StateCheck(string id)
        {
            if (id == "all_packs_unlocked") return Model.Content.Boosters.All.All(pack =>
                pack.AcquireMode != "PURCHASE" ||
                Model.Profile.CompletedQuests.Count >= pack.UnlockQuestCount);
            return Model.Counter("StateCheck:" + id) > 0;
        }
    }
}
