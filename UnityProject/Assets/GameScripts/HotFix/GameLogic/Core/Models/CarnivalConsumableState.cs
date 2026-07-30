using System;

namespace GameLogic.Core
{
    /// <summary>
    /// 单张已拥有消耗牌的局内状态。
    /// </summary>
    public sealed class CarnivalConsumableState
    {
        public CarnivalConsumableState(
            CarnivalConsumable content,
            CarnivalCardEdition edition = CarnivalCardEdition.Base,
            int? sellValue = null,
            string runtimeId = null)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Edition = edition;
            SellValue = sellValue ?? Math.Max(1, content.Cost / 2);
            RuntimeId = runtimeId ?? Guid.NewGuid().ToString("N");
        }

        public CarnivalConsumable Content { get; }
        public CarnivalCardEdition Edition { get; set; }
        public int SellValue { get; set; }
        public string RuntimeId { get; }
        public string Id => Content.Id;
        public string Name => Content.Name;
        public CarnivalConsumableFamily Family => Content.Family;
        public string Description => Content.Description;
        public bool OccupiesSlot => Edition != CarnivalCardEdition.Negative;

        public CarnivalConsumableState CreateCopy(CarnivalCardEdition edition)
        {
            return new CarnivalConsumableState(Content, edition, SellValue);
        }
    }
}
