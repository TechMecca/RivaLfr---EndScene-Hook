using Magic;
using static RivaLfr.Memory.Dx9;

namespace RivaLfr.Memory
{
    internal static class MemoryManager
    {
        public static BlackMagic Memory = new BlackMagic();
        public static Hook Hook;

        public static void Initialize(uint ProcessID)
        {
            Hook = new Hook(ProcessID);
        }

    }
}
