using System;
using System.Collections.Generic;

namespace GameLogic.Core.Model
{
    [Serializable]
    public sealed class StacklandsProfileData
    {
        public int Version = 1;
        public List<string> DiscoveredCards = new List<string>();
        public List<string> CompletedQuests = new List<string>();
        public List<string> GrantedOnce = new List<string>();
        public int PurchasedPacks;
        public bool PauseWhileDragging;
    }

    [Serializable]
    public sealed class EquipmentSlotsRunData
    {
        public string Hand;
        public string Head;
        public string Body;

        public string Get(EquipmentSlotKind slot)
        {
            switch (slot)
            {
                case EquipmentSlotKind.Hand: return Hand;
                case EquipmentSlotKind.Head: return Head;
                case EquipmentSlotKind.Body: return Body;
                default: return null;
            }
        }

        public void Set(EquipmentSlotKind slot, string cardId)
        {
            switch (slot)
            {
                case EquipmentSlotKind.Hand: Hand = cardId; break;
                case EquipmentSlotKind.Head: Head = cardId; break;
                case EquipmentSlotKind.Body: Body = cardId; break;
            }
        }
    }

    [Serializable]
    public sealed class CardRunData
    {
        public string InstanceId;
        public string CardId;
        public string StackId;
        public int StackOrder;
        public float X;
        public float Y;
        public bool IsFoil;
        public int Hp;
        public int Uses;
        public EquipmentSlotsRunData EquipmentSlots = new EquipmentSlotsRunData();
        public float AttackCooldown;
        public float StunRemaining;
        // 最近一次创建/移动时的 Run.Revision，布局解算器据此判定重叠时谁让路（旧存档默认为 0）。
        public int LastActiveRevision;
    }

    [Serializable]
    public sealed class BoosterRunData
    {
        public string InstanceId;
        public string BoosterId;
        public string DisplayNameZh;
        public float X;
        public float Y;
        public int Revealed;
        // 最近一次创建/移动时的 Run.Revision，布局解算器据此判定重叠时谁让路（旧存档默认为 0）。
        public int LastActiveRevision;
        public List<string> Results = new List<string>();
        public List<bool> Foils = new List<bool>();
    }

    [Serializable]
    public sealed class WorkRunData
    {
        public string Id;
        public string DefinitionId;
        public bool IsRecipe;
        public string StackId;
        public float Remaining;
        public float Duration;
        public List<string> CardIds = new List<string>();
    }

    [Serializable]
    public sealed class CounterRunData
    {
        public string Key;
        public int Value;
    }

    [Serializable]
    public sealed class StacklandsRunData
    {
        public int Version = 2;
        public uint RandomState;
        public int Moon = 1;
        public float MoonRemaining;
        public float MoonDuration;
        public float Speed = 1f;
        public bool Peaceful;
        public bool AwaitingCardLimit;
        public bool HadVillager;
        public int Revision;
        public List<CardRunData> Cards = new List<CardRunData>();
        public List<BoosterRunData> Boosters = new List<BoosterRunData>();
        public List<WorkRunData> Works = new List<WorkRunData>();
        public List<CounterRunData> Counters = new List<CounterRunData>();
        public List<string> GrantedOnce = new List<string>();
    }
}
