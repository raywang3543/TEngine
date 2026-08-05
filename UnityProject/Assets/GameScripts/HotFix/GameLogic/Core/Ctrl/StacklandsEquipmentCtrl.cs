using System.Collections.Generic;
using System.Linq;
using GameLogic.Core.Model;

namespace GameLogic.Core.Ctrl
{
    /// <summary>
    /// 装备槽、佩戴、替换、卸下、存档校验和装备掉落控制器。
    /// </summary>
    internal sealed class StacklandsEquipmentCtrl
    {
        private static readonly EquipmentSlotKind[] Slots =
        {
            EquipmentSlotKind.Hand,
            EquipmentSlotKind.Head,
            EquipmentSlotKind.Body,
        };
        private static readonly IReadOnlyList<EquipmentDefinition> EmptyEquipment =
            new EquipmentDefinition[0];

        private StacklandsGameModel Model => CoreSystem.Model;

        internal bool Equip(string equipmentId, string unitId)
        {
            CardRunData equipment = Model.GetCard(equipmentId);
            CardRunData unit = Model.GetCard(unitId);
            if (equipment == null || unit == null || !Model.Content.Equipment.Contains(equipment.CardId) || !Model.Content.Units.Contains(unit.CardId)) return false;
            if (!Model.Content.Units.Get(unit.CardId).CanEquip) return false;

            EquipmentDefinition definition = Model.Content.Equipment.Get(equipment.CardId);
            if (!IsWearableSlot(definition.Slot)) return false;

            EnsureSlots(unit);
            string replacedCardId = unit.EquipmentSlots.Get(definition.Slot);
            if (!string.IsNullOrEmpty(replacedCardId))
                Model.AddCard(replacedCardId, unit.X + 0.5f, unit.Y, false);
            unit.EquipmentSlots.Set(definition.Slot, equipment.CardId);
            Model.RemoveCard(equipment);
            Model.Changed();
            return true;
        }

        internal void Unequip(string unitId, EquipmentSlotKind slot)
        {
            CardRunData unit = Model.GetCard(unitId);
            if (unit == null || !Model.Content.Units.Contains(unit.CardId)) return;
            if (!Model.Content.Units.Get(unit.CardId).CanEquip) return;
            EnsureSlots(unit);
            bool changed = false;
            IEnumerable<EquipmentSlotKind> targetSlots = IsWearableSlot(slot) ? new[] { slot } : Slots;
            foreach (EquipmentSlotKind targetSlot in targetSlots)
            {
                string equipmentCardId = unit.EquipmentSlots.Get(targetSlot);
                if (string.IsNullOrEmpty(equipmentCardId)) continue;
                Model.AddCard(equipmentCardId, unit.X + 0.5f, unit.Y, false);
                unit.EquipmentSlots.Set(targetSlot, null);
                changed = true;
            }
            if (changed) Model.Changed();
        }

        internal bool TryEquipStack(string stackId)
        {
            List<CardRunData> cards = Model.StackCards(stackId);
            CardRunData unit = cards.FirstOrDefault(card => Model.Content.Units.Contains(card.CardId) && Model.Content.Units.Get(card.CardId).CanEquip);
            if (unit == null) return false;

            bool equipped = false;
            foreach (CardRunData equipment in cards
                         .Where(card => Model.Content.Equipment.Contains(card.CardId)).ToList())
                equipped |= Equip(equipment.InstanceId, unit.InstanceId);
            return equipped;
        }

        internal IReadOnlyList<EquipmentDefinition> GetEquipment(CardRunData unit)
        {
            if (unit == null) return EmptyEquipment;
            EnsureSlots(unit);
            var result = new List<EquipmentDefinition>(Slots.Length);
            foreach (EquipmentSlotKind slot in Slots)
            {
                string cardId = unit.EquipmentSlots.Get(slot);
                if (!string.IsNullOrEmpty(cardId) && Model.Content.Equipment.Contains(cardId))
                    result.Add(Model.Content.Equipment.Get(cardId));
            }
            return result;
        }

        internal EquipmentDefinition GetAttackTypeEquipment(CardRunData unit)
        {
            IReadOnlyList<EquipmentDefinition> equipment = GetEquipment(unit);
            return equipment.FirstOrDefault(item => item.Slot == EquipmentSlotKind.Hand &&
                                                     item.AttackType != AttackKind.None) ??
                   equipment.FirstOrDefault(item => item.AttackType != AttackKind.None);
        }

        internal string GetProfessionCardId(CardRunData unit)
        {
            IReadOnlyList<EquipmentDefinition> equipment = GetEquipment(unit);
            EquipmentDefinition profession = equipment
                .FirstOrDefault(item => item.Slot == EquipmentSlotKind.Hand &&
                                        !string.IsNullOrEmpty(item.ProfessionCardId)) ??
                equipment.FirstOrDefault(item => !string.IsNullOrEmpty(item.ProfessionCardId));
            return profession?.ProfessionCardId;
        }

        internal void DropAll(CardRunData unit)
        {
            if (unit == null) return;
            EnsureSlots(unit);
            int index = 0;
            foreach (EquipmentSlotKind slot in Slots)
            {
                string cardId = unit.EquipmentSlots.Get(slot);
                if (string.IsNullOrEmpty(cardId)) continue;
                Model.AddCard(cardId, unit.X + 0.35f * index++, unit.Y, false);
                unit.EquipmentSlots.Set(slot, null);
            }
        }

        internal void ValidateRunEquipment()
        {
            if (Model.Run == null) return;
            foreach (CardRunData unit in Model.Run.Cards.Where(card => Model.Content.Units.Contains(card.CardId) && Model.Content.Units.Get(card.CardId).CanEquip).ToList())
            {
                EnsureSlots(unit);
                foreach (EquipmentSlotKind slot in Slots)
                {
                    string cardId = unit.EquipmentSlots.Get(slot);
                    if (string.IsNullOrEmpty(cardId)) continue;
                    if (!Model.Content.Equipment.Contains(cardId) || Model.Content.Equipment.Get(cardId).Slot != slot)
                        unit.EquipmentSlots.Set(slot, null);
                }
            }
        }

        private static void EnsureSlots(CardRunData unit)
        {
            if (unit.EquipmentSlots == null) unit.EquipmentSlots = new EquipmentSlotsRunData();
        }

        private static bool IsWearableSlot(EquipmentSlotKind slot)
        {
            return slot == EquipmentSlotKind.Hand || slot == EquipmentSlotKind.Head ||
                   slot == EquipmentSlotKind.Body;
        }
    }
}
