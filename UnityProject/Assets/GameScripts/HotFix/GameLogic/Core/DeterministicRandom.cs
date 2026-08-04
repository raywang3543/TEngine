namespace GameLogic.Core
{
    internal sealed class DeterministicRandom
    {
        private uint _state;
        public DeterministicRandom(uint seed) { _state = seed == 0 ? 0x6D2B79F5u : seed; }
        public uint State { get => _state; set => _state = value == 0 ? 0x6D2B79F5u : value; }
        public uint NextUInt()
        {
            uint value = _state;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            _state = value;
            return value;
        }
        public float NextFloat() => (NextUInt() & 0x00FFFFFFu) / 16777216f;
        public int Range(int min, int max) => max <= min ? min : min + (int)(NextFloat() * (max - min));
    }
}
