using System.Collections.Generic;
using GameLogic.Core.Model;

namespace GameLogic.Core
{
    public enum StacklandsCommandKind
    {
        NewGame, ContinueGame, SetSpeed, MoveCard, MoveStack, SelectCard, BuyBooster, OpenBooster, MoveBooster,
        SellCard, Equip, Unequip, ConfirmSummon, SaveGame,
    }

    public enum StacklandsFlowKind { None, MainMenu, CardLimit, SummonDemon, Victory, GameOver, SaveError }

    public sealed class StacklandsCommandDto
    {
        public StacklandsCommandKind Kind;
        public string InstanceId;
        public string TargetInstanceId;
        public string ContentId;
        public float X;
        public float Y;
        public float Number;
        public bool Flag;
        public EquipmentSlotKind EquipmentSlot;

        public static StacklandsCommandDto NewGame(bool peaceful, int moonLengthIndex)
        {
            return new StacklandsCommandDto
            {
                Kind = StacklandsCommandKind.NewGame,
                Flag = peaceful,
                Number = moonLengthIndex,
            };
        }
    }

    public sealed class CardSnapshot
    {
        public string InstanceId { get; internal set; }
        public string CardId { get; internal set; }
        public string NameZh { get; internal set; }
        public string DescriptionZh { get; internal set; }
        public string Color { get; internal set; }
        public string Category { get; internal set; }
        public string StackId { get; internal set; }
        public int StackOrder { get; internal set; }
        public float X { get; internal set; }
        public float Y { get; internal set; }
        public int SellPrice { get; internal set; }
        public bool CanSell { get; internal set; }
        public int FoodValue { get; internal set; }
        public int Hp { get; internal set; }
        public int MaxHp { get; internal set; }
        public float Progress { get; internal set; }
        public bool IsFoil { get; internal set; }
        public bool IsLocked { get; internal set; }
        public string Status { get; internal set; }
    }

    public sealed class BoosterSnapshot
    {
        public string InstanceId { get; internal set; }
        public string BoosterId { get; internal set; }
        public string NameZh { get; internal set; }
        public float X { get; internal set; }
        public float Y { get; internal set; }
        public int Remaining { get; internal set; }
    }

    public sealed class BoardSnapshot
    {
        public int Revision { get; internal set; }
        public IReadOnlyList<CardSnapshot> Cards { get; internal set; }
        public IReadOnlyList<BoosterSnapshot> Boosters { get; internal set; }
        public string SelectedInstanceId { get; internal set; }
    }

    /// <summary>
    /// 单张卡牌的实时工作进度增量，避免计时期间反复复制完整牌桌快照。
    /// </summary>
    public sealed class CardProgressSnapshot
    {
        public string InstanceId { get; internal set; }
        public float Progress { get; internal set; }
    }

    /// <summary>
    /// 当前帧所有工作中卡牌的进度增量。
    /// </summary>
    public sealed class CardProgressBatch
    {
        public IReadOnlyList<CardProgressSnapshot> Cards { get; internal set; }
    }

    public sealed class QuestSnapshot
    {
        public string Id { get; internal set; }
        public string NameZh { get; internal set; }
        public string DescriptionZh { get; internal set; }
        public bool Completed { get; internal set; }
        public bool IsMain { get; internal set; }
    }

    public sealed class BoosterShopSnapshot
    {
        public string Id { get; internal set; }
        public string NameZh { get; internal set; }
        public int Price { get; internal set; }
        public bool Unlocked { get; internal set; }
        public string LockText { get; internal set; }
    }

    public sealed class CardopediaEntrySnapshot
    {
        public string CardId { get; internal set; }
        public string NameZh { get; internal set; }
        public string Category { get; internal set; }
        public bool Discovered { get; internal set; }
    }

    public sealed class HudSnapshot
    {
        public int Moon { get; internal set; }
        public float MoonRemaining { get; internal set; }
        public float MoonDuration { get; internal set; }
        public float Speed { get; internal set; }
        public int Coins { get; internal set; }
        public int Food { get; internal set; }
        public int CardCount { get; internal set; }
        public int CardCap { get; internal set; }
        public int CompletedQuestCount { get; internal set; }
        public bool Peaceful { get; internal set; }
        public IReadOnlyList<QuestSnapshot> Quests { get; internal set; }
        public IReadOnlyList<BoosterShopSnapshot> Boosters { get; internal set; }
        public IReadOnlyList<CardopediaEntrySnapshot> Cardopedia { get; internal set; }
    }

    public sealed class FlowRequest
    {
        public StacklandsFlowKind Kind { get; internal set; }
        public string Title { get; internal set; }
        public string Message { get; internal set; }
        public string InstanceId { get; internal set; }
        public bool CanContinue { get; internal set; }
    }
}
