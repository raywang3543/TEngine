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
        public string EquipmentCardId;
        public float AttackCooldown;
        public float StunRemaining;
    }

    [Serializable]
    public sealed class BoosterRunData
    {
        public string InstanceId;
        public string BoosterId;
        public float X;
        public float Y;
        public int Revealed;
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
        public int Version = 1;
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
