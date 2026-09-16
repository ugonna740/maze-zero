namespace MazeZero
{
    // Identical xorshift32 implementation is used by the authoritative server.
    public sealed class MazeRandom
    {
        private uint state;
        public MazeRandom(int seed) { state = unchecked((uint)seed); if (state == 0) state = 0x6D2B79F5u; }
        private uint NextUInt() { var x = state; x ^= x << 13; x ^= x >> 17; x ^= x << 5; return state = x; }
        public int Next(int maximum) => maximum <= 1 ? 0 : (int)(NextUInt() % (uint)maximum);
        public double NextDouble() => NextUInt() / 4294967296.0;
    }
}
