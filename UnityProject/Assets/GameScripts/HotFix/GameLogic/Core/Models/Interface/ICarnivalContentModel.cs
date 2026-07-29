using System.Collections.Generic;

namespace GameLogic.Core
{
    /// <summary>
    /// 卡牌内容目录接口。
    /// </summary>
    public interface ICarnivalContentModel
    {
        IReadOnlyList<CarnivalPerformer> Performers { get; }
        IReadOnlyList<CarnivalConsumable> Consumables { get; }

        CarnivalPerformer FindPerformer(string performerId);
    }
}
