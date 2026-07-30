using System.Collections.Generic;

namespace GameLogic.Core
{
    /// <summary>
    /// 局外小丑牌解锁状态。
    /// </summary>
    public interface ICarnivalUnlockModel
    {
        CarnivalUnlockStatistics Statistics { get; }
        bool IsJokerUnlocked(string jokerId);
        void UnlockJoker(string jokerId);
    }

    /// <summary>
    /// 跨牌局累计的原版解锁统计。
    /// </summary>
    public sealed class CarnivalUnlockStatistics
    {
        public int RunsLost { get; set; }
        public int TotalHandsPlayed { get; set; }
        public int TotalFaceCardsPlayed { get; set; }
        public int TotalJokersSold { get; set; }
        public int TotalCardsSold { get; set; }
        public int ConsecutiveOneHandBlindWins { get; set; }
        public bool ContinuedSavedRun { get; set; }
        public HashSet<string> DiscoveredTarotIds { get; } = new HashSet<string>();
        public HashSet<string> DiscoveredPlanetIds { get; } = new HashSet<string>();
    }

    /// <summary>
    /// 默认的内存解锁状态；存档层可注入持久化实现。
    /// </summary>
    public sealed class CarnivalUnlockModel : ICarnivalUnlockModel
    {
        private readonly HashSet<string> _unlockedJokerIds = new HashSet<string>();

        public CarnivalUnlockStatistics Statistics { get; } = new CarnivalUnlockStatistics();

        public bool IsJokerUnlocked(string jokerId)
        {
            return !string.IsNullOrEmpty(jokerId) && _unlockedJokerIds.Contains(jokerId);
        }

        public void UnlockJoker(string jokerId)
        {
            if (!string.IsNullOrEmpty(jokerId))
                _unlockedJokerIds.Add(jokerId);
        }
    }
}
