namespace GameLogic.Core
{
    /// <summary>
    /// 跳过盲注时获得的标签。
    /// </summary>
    public sealed class CarnivalBlindTag
    {
        public CarnivalBlindTag(
            CarnivalBlindTagKind kind,
            string name,
            string description)
        {
            Kind = kind;
            Name = name;
            Description = description;
        }

        public CarnivalBlindTagKind Kind { get; }
        public string Name { get; }
        public string Description { get; }
    }
}
