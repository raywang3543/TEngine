using System;
using System.Collections.Generic;
using System.Linq;

namespace GameLogic.Core.Model
{
    /// <summary>
    /// 当前局的唯一可变状态容器。规则判断由 Ctrl 执行，Model 只维护状态与基础查询。
    /// </summary>
    internal sealed class StacklandsGameModel
    {
        internal StacklandsGameModel(IStacklandsContentModel content, IStacklandsSaveStore saveStore)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            SaveStore = saveStore ?? throw new ArgumentNullException(nameof(saveStore));
            Profile = saveStore.LoadProfile() ?? new StacklandsProfileData();
            ActionsByCard = content.Actions.All.GroupBy(action => action.SourceCardId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
            MilestonesByGroup = content.Milestones.All.GroupBy(item => item.GroupId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.OrderBy(item => item.TriggerCount).ToList(),
                    StringComparer.Ordinal);
        }

        public IStacklandsContentModel Content { get; }
        internal IStacklandsSaveStore SaveStore { get; }
        internal Dictionary<string, List<CardActionDefinition>> ActionsByCard { get; }
        internal Dictionary<string, List<MilestoneDefinition>> MilestonesByGroup { get; }
        internal StacklandsProfileData Profile { get; set; }
        internal StacklandsRunData Run { get; set; }
        internal DeterministicRandom Random { get; set; }
        internal string SelectedId { get; set; }
        internal float SaveDelay { get; set; } = -1f;
        internal float MovementPublishDelay { get; set; }

        public bool HasActiveRun => Run != null;

        internal CardRunData AddCard(string cardId, float x, float y, bool created, bool foil = false)
        {
            CardDefinition definition = Content.Cards.Get(cardId);
            if (definition.IsUnique == true)
            {
                CardRunData existing = Run.Cards.FirstOrDefault(item => item.CardId == cardId);
                if (existing != null) return existing;
            }

            var card = new CardRunData
            {
                InstanceId = NewId("card"), CardId = cardId, StackId = NewId("stack"), X = x, Y = y,
                IsFoil = foil,
                Hp = Content.Units.Contains(cardId) ? Content.Units.Get(cardId).MaxHp.GetValueOrDefault(1) : 0,
            };
            Run.Cards.Add(card);
            if (!Profile.DiscoveredCards.Contains(cardId)) Profile.DiscoveredCards.Add(cardId);
            if (definition.Category == "VILLAGER") Run.HadVillager = true;
            Increment("CardObtained:" + cardId);
            if (created) Increment("CardCreated:" + cardId);
            return card;
        }

        internal void RemoveCard(CardRunData card)
        {
            if (card == null) return;
            Run.Cards.Remove(card);
            CancelWorks(new[] { card.InstanceId });
            if (SelectedId == card.InstanceId) SelectedId = null;
        }

        internal void RemoveCards(string cardId, int count)
        {
            foreach (CardRunData card in Run.Cards.Where(item => item.CardId == cardId).Take(count).ToList())
                RemoveCard(card);
        }

        internal void ConsumeFood(int points)
        {
            int consumed = 0;
            foreach (CardRunData card in Run.Cards.Where(card =>
                         Content.Cards.Get(card.CardId).FoodValue.GetValueOrDefault() > 0)
                         .OrderBy(card => Content.Cards.Get(card.CardId).FoodValue).ToList())
            {
                if (consumed >= points) break;
                consumed += Content.Cards.Get(card.CardId).FoodValue.Value;
                RemoveCard(card);
            }
        }

        internal int CountAdultVillagers() => Run.Cards.Count(card =>
            Content.Cards.Get(card.CardId).Tags.Contains("ADULT"));
        internal int CountCard(string id) => string.IsNullOrEmpty(id) ? 0 :
            Run.Cards.Count(card => card.CardId == id);
        internal int CurrentFood() => Run.Cards.Sum(card =>
            Content.Cards.Get(card.CardId).FoodValue.GetValueOrDefault());
        internal int CurrentCardCount() => Run.Cards.Sum(card => Content.Cards.Get(card.CardId).CardCapCost);
        internal int CurrentCardCap() => Content.WorldRules.RequireBaseCardCap() + Run.Cards.Sum(card =>
            Content.Structures.Contains(card.CardId) ? Content.Structures.Get(card.CardId).CardCapDelta : 0);
        internal CardRunData GetCard(string id) => string.IsNullOrEmpty(id) ? null :
            Run?.Cards.FirstOrDefault(card => card.InstanceId == id);
        internal List<CardRunData> StackCards(string stackId) => Run.Cards.Where(card => card.StackId == stackId)
            .OrderBy(card => card.StackOrder).ToList();
        internal string NewId(string prefix) => prefix + "_" + Random.NextUInt().ToString("x8") +
                                                Random.NextUInt().ToString("x8");

        internal void CancelWorks(IEnumerable<string> cardIds)
        {
            var set = new HashSet<string>(cardIds, StringComparer.Ordinal);
            Run.Works.RemoveAll(work => work.CardIds.Any(set.Contains));
        }

        internal void NormalizeStacks()
        {
            foreach (var group in Run.Cards.GroupBy(card => card.StackId))
            {
                int order = 0;
                foreach (CardRunData card in group.OrderBy(card => card.StackOrder)) card.StackOrder = order++;
            }
        }

        internal void Increment(string key, int amount = 1)
        {
            CounterRunData counter = Run?.Counters.FirstOrDefault(item => item.Key == key);
            if (counter == null && Run != null)
            {
                counter = new CounterRunData { Key = key };
                Run.Counters.Add(counter);
            }
            if (counter != null) counter.Value += amount;
        }

        internal int Counter(string key) => Run.Counters.FirstOrDefault(item => item.Key == key)?.Value ?? 0;

        internal void RemoveInvalidSaveEntries()
        {
            Run.Cards.RemoveAll(card => !Content.Cards.Contains(card.CardId));
            Run.Boosters.RemoveAll(pack => !Content.Boosters.Contains(pack.BoosterId));
            Run.Works.RemoveAll(work => work.IsRecipe
                ? !Content.Recipes.Contains(work.DefinitionId)
                : !Content.Actions.Contains(work.DefinitionId));
            NormalizeStacks();
        }

        internal bool IsHostile(CardRunData card)
        {
            if (!Content.Units.Contains(card.CardId)) return false;
            UnitFaction faction = Content.Units.Get(card.CardId).Faction;
            return faction == UnitFaction.Hostile || faction == UnitFaction.Boss;
        }

        internal void MarkDirty() => SaveDelay = 0.5f;

        internal void Changed()
        {
            Run.Revision++;
            MarkDirty();
            CoreSystem.ViewCtrl.PublishAll();
        }
    }
}
