using RivaLfr.Memory;
using System.Text;

namespace RivaLfr
{
    public static class Lua
    {
        private const uint LuaDoStringAddress = 0x819210;
        private const uint GetLocalizedTextAddress = 0x7225E0;
        private const uint PrepareTextAddress = 0x4038F0;

        public static void LuaDoString(string command)
        {
            if (string.IsNullOrEmpty(command))
                return;

            byte[] bytes = Encoding.UTF8.GetBytes(command + "\0");
            uint buffer = MemoryManager.Memory.AllocateMemory(bytes.Length);
            MemoryManager.Memory.WriteBytes(buffer, bytes);

            string[] asm = new string[]
            {
                $"mov eax, {buffer}",
                "push 0",
                "push eax",
                "push eax",
                $"mov eax, {LuaDoStringAddress}",
                "call eax",
                "add esp, 0xC",
                "retn"
            };

            MemoryManager.Hook.InjectAndExecute(asm);
            MemoryManager.Memory.FreeMemory(buffer);
        }

        public static string GetLocalizedText(string luaCommand)
        {
            if (string.IsNullOrEmpty(luaCommand))
                return string.Empty;

            byte[] bytes = Encoding.UTF8.GetBytes(luaCommand + "\0");
            uint buffer = MemoryManager.Memory.AllocateMemory(bytes.Length);
            MemoryManager.Memory.WriteBytes(buffer, bytes);

            string[] asm = new string[]
            {
                $"call {PrepareTextAddress}",
                "mov ecx, eax",
                "push -1",
                $"mov edx, {buffer}",
                "push edx",
                $"call {GetLocalizedTextAddress}",
                "retn"
            };

            byte[] resultBytes = MemoryManager.Hook.InjectAndExecute(asm);
            MemoryManager.Memory.FreeMemory(buffer);

            return Encoding.ASCII.GetString(resultBytes).TrimEnd('\0');
        }
    }
}
