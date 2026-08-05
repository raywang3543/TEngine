namespace GameLogic.Core
{
    /// <summary>
    /// Core 内所有面向玩家的显示文本，统一在此维护；含动态内容的用静态方法拼接。
    /// 配置校验、异常等开发向文本不收拢在此。
    /// </summary>
    internal static class StacklandsTexts
    {
        // 通用
        public const string Unnamed = "未命名";

        // 卡牌
        public const string WholeStackBadge = "整堆";
        public const string StatusCrafting = "制作中";
        public const string StatusWorking = "工作中";
        public const string FoilPrefix = "闪  ";

        public static string CardHp(int hp, int maxHp) => $"HP {hp}/{maxHp}";
        public static string CardFoodFooter(int foodValue, int sellPrice) => $"食 {foodValue}  售 {sellPrice}";
        public static string CardSellFooter(int sellPrice) => $"售 {sellPrice}";

        // 卡包与卡槽
        public const string TemporaryEquipmentPackName = "装备测试包";

        public static string BoosterText(string name, int remaining) => name + "\n剩余 " + remaining;
        public static string ShopLockedByQuests(int questCount) => $"完成 {questCount} 项任务";
        public static string ShopPrice(int price) => price + " 金币";
        public static string SellSlot(int coins) => "出售\n金币 " + coins;

        // 短提示（StacklandsNotification）
        public const string NotifyBoosterLocked = "卡包尚未解锁";
        public const string NotifyDragCoinsToSlot = "请将金币堆拖到卡槽购买";
        public const string NotifyNotEnoughCoins = "金币不足";

        public static string NotifyQuestCompleted(string questName) => "任务完成：" + questName;
        public static string NotifyStackCapacity(int maxStackSize) => $"牌堆最多容纳 {maxStackSize} 张卡";

        // 流程弹窗（FlowRequest）
        public const string GameTitle = "堆叠大陆";
        public const string MainMenuContinueMessage = "继续当前冒险，或保留跨局进度开始新游戏";
        public const string MainMenuNewGameMessage = "开始新的 Original 主大陆冒险";
        public const string NoSaveTitle = "没有存档";
        public const string NoSaveMessage = "请开始新游戏";
        public const string SaveErrorTitle = "存档失败";
        public const string CardLimitTitle = "卡牌超出上限";
        public const string CardLimitMessage = "出售多余卡牌后继续";
        public const string SummonDemonTitle = "召唤恶魔？";
        public const string SummonDemonMessage = "仪式开始后将消耗 Golden Goblet（金杯）。";
        public const string VictoryTitle = "主大陆完成";
        public const string VictoryMessage = "你击败了恶魔！";
        public const string GameOverTitle = "村庄覆灭";
        public const string GameOverMessage = "所有村民都已死亡";
    }
}
