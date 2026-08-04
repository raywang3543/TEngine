using System;
using System.Collections.Generic;
using System.Linq;
using GameLogic.Core.Model;

namespace GameLogic.Core.Ctrl
{
    /// <summary>
    /// 自动战斗、装备数值、克制、效果和死亡掉落控制器。
    /// </summary>
    internal sealed class StacklandsCombatCtrl
    {
        private StacklandsGameModel Model => CoreSystem.Model;

        internal void Tick(float delta)
        {
            foreach (CardRunData unit in Model.Run.Cards.Where(card => Model.Content.Units.Contains(card.CardId)))
                unit.StunRemaining = Math.Max(0f, unit.StunRemaining - delta);
            foreach (var group in Model.Run.Cards.Where(card => Model.Content.Units.Contains(card.CardId))
                         .GroupBy(card => card.StackId).ToList())
            {
                List<CardRunData> friendly = group.Where(card => !Model.IsHostile(card)).ToList();
                List<CardRunData> hostile = group.Where(Model.IsHostile).ToList();
                if (friendly.Count == 0 || hostile.Count == 0) continue;
                foreach (CardRunData attacker in group.ToList())
                {
                    if (attacker.StunRemaining > 0f) continue;
                    attacker.AttackCooldown -= delta;
                    if (attacker.AttackCooldown > 0f) continue;
                    List<CardRunData> targets = Model.IsHostile(attacker) ? friendly : hostile;
                    if (targets.Count == 0) break;
                    Attack(attacker, targets[Model.Random.Range(0, targets.Count)]);
                    UnitDefinition unit = Model.Content.Units.Get(attacker.CardId);
                    float interval = unit.AttackInterval.GetValueOrDefault(2f);
                    if (!string.IsNullOrEmpty(attacker.EquipmentCardId) &&
                        Model.Content.Equipment.Contains(attacker.EquipmentCardId))
                        interval *= Math.Max(0.2f,
                            1f - Model.Content.Equipment.Get(attacker.EquipmentCardId).AttackSpeedDelta / 100f);
                    attacker.AttackCooldown = Math.Max(0.2f, interval);
                    friendly.RemoveAll(item => Model.GetCard(item.InstanceId) == null);
                    hostile.RemoveAll(item => Model.GetCard(item.InstanceId) == null);
                }
            }
        }

        internal void Kill(CardRunData card)
        {
            string cardId = card.CardId; float x = card.X, y = card.Y;
            UnitDefinition unit = Model.Content.Units.Get(cardId);
            Model.RemoveCard(card);
            if (!string.IsNullOrEmpty(unit.DeathResultCardId))
                Model.AddCard(unit.DeathResultCardId, x, y, false);
            if (Model.ActionsByCard.TryGetValue(cardId, out var actions))
            {
                CardActionDefinition defeat = actions.FirstOrDefault(action => action.Type == CardActionKind.Defeat);
                if (defeat != null && !string.IsNullOrEmpty(defeat.LootPoolId))
                    foreach (string result in CoreSystem.LootCtrl.RollPool(defeat.LootPoolId))
                        Model.AddCard(result, x, y, true);
            }
            Model.Increment("CardKilled:" + cardId);
            CoreSystem.QuestCtrl.Evaluate();
            if (cardId == "demon")
            {
                CoreSystem.RunCtrl.SaveNow();
                CoreSystem.RequestFlow(new FlowRequest
                    { Kind = StacklandsFlowKind.Victory, Title = "主大陆完成", Message = "你击败了恶魔！" });
            }
            CheckGameOver();
            Model.Changed();
        }

        internal void CheckGameOver()
        {
            if (Model.Run.HadVillager && !Model.Run.Cards.Any(card =>
                    Model.Content.Cards.Get(card.CardId).Category == "VILLAGER"))
                CoreSystem.RequestFlow(new FlowRequest
                    { Kind = StacklandsFlowKind.GameOver, Title = "村庄覆灭", Message = "所有村民都已死亡" });
        }

        private void Attack(CardRunData attacker, CardRunData target)
        {
            UnitDefinition source = Model.Content.Units.Get(attacker.CardId);
            UnitDefinition destination = Model.Content.Units.Get(target.CardId);
            EquipmentDefinition sourceEquipment = !string.IsNullOrEmpty(attacker.EquipmentCardId) &&
                                                   Model.Content.Equipment.Contains(attacker.EquipmentCardId)
                ? Model.Content.Equipment.Get(attacker.EquipmentCardId) : null;
            EquipmentDefinition targetEquipment = !string.IsNullOrEmpty(target.EquipmentCardId) &&
                                                   Model.Content.Equipment.Contains(target.EquipmentCardId)
                ? Model.Content.Equipment.Get(target.EquipmentCardId) : null;
            float hitChance = source.HitChance.GetValueOrDefault(0.75f) + (sourceEquipment?.HitDelta ?? 0) / 100f;
            if (Model.Random.NextFloat() > hitChance) return;
            int min = source.DamageMin.GetValueOrDefault(1), max = source.DamageMax.GetValueOrDefault(min);
            int damage = Model.Random.Range(min, max + 1) + (sourceEquipment?.DamageDelta ?? 0);
            damage = Math.Max(1, damage - destination.Defense.GetValueOrDefault() -
                                 (targetEquipment?.DefenseDelta ?? 0));
            AttackKind sourceKind = sourceEquipment != null && sourceEquipment.AttackType != AttackKind.None
                ? sourceEquipment.AttackType : source.AttackType;
            if (HasAdvantage(sourceKind, destination.AttackType))
                damage = Math.Max(1,
                    (int)Math.Round(damage * Model.Content.WorldRules.CombatAdvantageMultiplier));
            if (Model.Random.NextFloat() < source.CritChance.GetValueOrDefault()) damage *= 2;
            target.Hp -= damage;
            ApplyEffects(attacker, target, "ON_HIT");
            if (target.Hp <= 0) Kill(target);
        }

        private void ApplyEffects(CardRunData source, CardRunData target, string trigger)
        {
            foreach (CardEffectDefinition effect in Model.Content.Effects.All.Where(item =>
                         item.SourceCardId == source.CardId &&
                         string.Equals(item.Trigger, trigger, StringComparison.OrdinalIgnoreCase)))
            {
                string counterKey = "Effect:" + effect.Id + ":" + source.InstanceId;
                if (effect.Once == OnceKind.Run && Model.Run.GrantedOnce.Contains(counterKey) ||
                    effect.Once == OnceKind.Profile && Model.Profile.GrantedOnce.Contains(counterKey) ||
                    effect.MaxTriggers > 0 && Model.Counter(counterKey) >= effect.MaxTriggers ||
                    Model.Random.NextFloat() > effect.Chance) continue;
                IEnumerable<CardRunData> targets = effect.Target == "ALL_ENEMIES"
                    ? Model.Run.Cards.Where(card => Model.Content.Units.Contains(card.CardId) &&
                                                    Model.IsHostile(card) != Model.IsHostile(source))
                    : new[] { target };
                if (effect.EffectType == "STUN")
                    foreach (CardRunData card in targets)
                        card.StunRemaining = Math.Max(card.StunRemaining, effect.Duration);
                Model.Increment(counterKey);
                if (effect.Once == OnceKind.Run) Model.Run.GrantedOnce.Add(counterKey);
                if (effect.Once == OnceKind.Profile) Model.Profile.GrantedOnce.Add(counterKey);
            }
        }

        private static bool HasAdvantage(AttackKind source, AttackKind target)
        {
            return source == AttackKind.Ranged && target == AttackKind.Melee ||
                   source == AttackKind.Melee && target == AttackKind.Magic ||
                   source == AttackKind.Magic && target == AttackKind.Ranged;
        }
    }
}
