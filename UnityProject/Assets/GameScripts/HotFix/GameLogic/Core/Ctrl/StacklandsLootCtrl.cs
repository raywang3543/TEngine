using System;
using System.Collections.Generic;
using System.Linq;
using GameLogic.Core.Model;

namespace GameLogic.Core.Ctrl
{
    /// <summary>
    /// 权重池、卡包预抽、保底和一次性奖励控制器。
    /// </summary>
    internal sealed class StacklandsLootCtrl
    {
        private StacklandsGameModel Model => CoreSystem.Model;

        internal List<string> RollPool(string poolId, int depth = 0)
        {
            if (depth > 8) throw new InvalidOperationException("掉落池后备链形成循环：" + poolId);
            LootPoolDefinition pool = Model.Content.LootPools.Get(poolId);
            int count = Model.Random.Range(pool.DrawMin, pool.DrawMax + 1);
            var results = new List<string>();
            var available = pool.Entries.Where(Eligible).ToList();
            for (int draw = 0; draw < count; draw++)
            {
                if (available.Count == 0)
                {
                    if (!string.IsNullOrEmpty(pool.FallbackPoolId))
                        results.AddRange(RollPool(pool.FallbackPoolId, depth + 1));
                    break;
                }
                float total = available.Sum(entry => entry.RequireWeight());
                float roll = Model.Random.NextFloat() * (pool.NormalizeWeights ? total : 100f);
                if (!pool.NormalizeWeights && roll >= total)
                {
                    if (!string.IsNullOrEmpty(pool.FallbackPoolId))
                        results.AddRange(RollPool(pool.FallbackPoolId, depth + 1));
                    continue;
                }
                float cursor = 0f;
                LootEntryDefinition selected = available[available.Count - 1];
                foreach (LootEntryDefinition entry in available)
                {
                    cursor += entry.RequireWeight();
                    if (roll < cursor) { selected = entry; break; }
                }
                int amount = Model.Random.Range(selected.MinCount, selected.MaxCount + 1);
                for (int i = 0; i < amount; i++) results.Add(selected.ResultCardId);
                MarkOnce(selected);
                if (pool.WithoutReplacement) available.Remove(selected);
            }
            return results;
        }

        internal void CreateBooster(string boosterId, float x, float y, bool purchased)
        {
            BoosterDefinition definition = Model.Content.Boosters.Get(boosterId);
            var booster = new BoosterRunData
                { InstanceId = Model.NewId("pack"), BoosterId = boosterId, X = x, Y = y };
            foreach (BoosterSlotDefinition slot in definition.Slots)
            {
                string cardId;
                if (!string.IsNullOrEmpty(slot.GuaranteeCardId)) cardId = slot.GuaranteeCardId;
                else
                {
                    string poolId = Model.Run.Peaceful && !string.IsNullOrEmpty(slot.PeacefulPoolId)
                        ? slot.PeacefulPoolId : SelectSlotPool(slot);
                    cardId = RollPool(poolId).First();
                }
                booster.Results.Add(cardId);
                booster.Foils.Add(Model.Content.Cards.Get(cardId).IsFoilEligible == true &&
                                  Model.Random.NextFloat() < definition.FoilChance);
            }
            if (purchased)
            {
                Model.Profile.PurchasedPacks++;
                if (Model.Profile.PurchasedPacks == Model.Content.WorldRules.SecondVillagerGuaranteePack)
                    booster.Results[0] = "villager";
                else if (Model.CountAdultVillagers() == 1 &&
                         Model.Random.NextFloat() < Model.Content.WorldRules.SingleVillagerPackChance)
                    booster.Results[0] = "villager";
            }
            Model.Run.Boosters.Add(booster);
        }

        internal void BuyBooster(string boosterId)
        {
            if (Model.Run == null || !Model.Content.Boosters.Contains(boosterId)) return;
            BoosterDefinition pack = Model.Content.Boosters.Get(boosterId);
            if (Model.Profile.CompletedQuests.Count < pack.UnlockQuestCount || pack.AcquireMode != "PURCHASE")
            {
                CoreSystem.Notify("卡包尚未解锁"); return;
            }
            if (Model.CountCard(pack.PriceCardId) < pack.PriceAmount)
            {
                CoreSystem.Notify("金币不足"); return;
            }
            Model.RemoveCards(pack.PriceCardId, pack.PriceAmount);
            CreateBooster(boosterId, 0f, 3f, true);
            Model.Increment("PackPurchased:" + boosterId);
            CoreSystem.QuestCtrl.Evaluate();
            Model.Changed();
        }

        internal void OpenBooster(string instanceId)
        {
            BoosterRunData booster = Model.Run?.Boosters.FirstOrDefault(item => item.InstanceId == instanceId);
            if (booster == null || booster.Revealed >= booster.Results.Count) return;
            int index = booster.Revealed++;
            CardRunData card = Model.AddCard(booster.Results[index], booster.X + index * 0.45f,
                booster.Y - 1f, false, booster.Foils[index]);
            CoreSystem.WorkCtrl.TryStartAction(card.StackId);
            if (booster.Revealed >= booster.Results.Count)
            {
                Model.Run.Boosters.Remove(booster);
                Model.Increment("PackOpened:" + booster.BoosterId);
                TryGrantNewWeaponry();
            }
            CoreSystem.QuestCtrl.Evaluate();
            Model.Changed();
        }

        private bool Eligible(LootEntryDefinition entry)
        {
            if (entry.OnceScope == "PROFILE" && Model.Profile.GrantedOnce.Contains(entry.Id)) return false;
            if (entry.OnceScope == "RUN" && Model.Run.GrantedOnce.Contains(entry.Id)) return false;
            switch (entry.ConditionType)
            {
                case "IDEA_UNDISCOVERED": return !Model.Profile.DiscoveredCards.Contains(entry.ResultCardId);
                case "PEACEFUL_MODE": return Model.Run.Peaceful;
                case "MOON_AT_LEAST": return int.TryParse(entry.ConditionArg, out int moon) && Model.Run.Moon >= moon;
                case "PACK_PURCHASE_COUNT": return int.TryParse(entry.ConditionArg, out int packs) &&
                                                    Model.Profile.PurchasedPacks >= packs;
                case "OWNED_COUNT_LESS_THAN":
                    string[] parts = (entry.ConditionArg ?? string.Empty).Split(':');
                    return parts.Length == 2 && int.TryParse(parts[1], out int limit) &&
                           Model.CountCard(parts[0]) < limit;
                default: return true;
            }
        }

        private void MarkOnce(LootEntryDefinition entry)
        {
            if (entry.OnceScope == "PROFILE" && !Model.Profile.GrantedOnce.Contains(entry.Id))
                Model.Profile.GrantedOnce.Add(entry.Id);
            if (entry.OnceScope == "RUN" && !Model.Run.GrantedOnce.Contains(entry.Id))
                Model.Run.GrantedOnce.Add(entry.Id);
        }

        private string SelectSlotPool(BoosterSlotDefinition slot)
        {
            if (!string.IsNullOrEmpty(slot.IdeaPoolId))
            {
                LootPoolDefinition ideas = Model.Content.LootPools.Get(slot.IdeaPoolId);
                if (ideas.Entries.Any(Eligible)) return slot.IdeaPoolId;
            }
            return slot.NormalPoolId;
        }

        private void TryGrantNewWeaponry()
        {
            if (Model.Profile.PurchasedPacks < 10 || Model.Run.GrantedOnce.Contains("new_weaponry")) return;
            Model.Run.GrantedOnce.Add("new_weaponry");
            CreateBooster("new_weaponry", 1f, 3f, false);
        }
    }
}
