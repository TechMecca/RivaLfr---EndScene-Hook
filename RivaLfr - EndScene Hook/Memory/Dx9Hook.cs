using System;
using System.Threading;

namespace RivaLfr.Memory
{
    internal class Dx9
    {
        internal class Hook
        {
            enum Dx9 : uint
            {
                DX_DEVICE = 0xC5DF88,
                DX_DEVICE_IDX = 0x397C,
                ENDSCENE_IDX = 0xA8,
            }

            private uint injectedCode = 0;
            private uint injectionAddress = 0;
            private uint returnValueAddress = 0;
            const int SizeJumpBack = 7;

            private readonly object injectionLock = new object();
            private byte[] originalBytes;
            private uint? cachedEndScenePtr = null;

            public bool ThreadHooked { get; private set; } = false;
            private readonly uint processId;

            public Hook(uint processId)
            {
                this.processId = processId;
                Hooking();
            }

            private uint GetEndScene()
            {
                if (cachedEndScenePtr.HasValue)
                    return cachedEndScenePtr.Value;

                uint pDevice = MemoryManager.Memory.ReadUInt((uint)Dx9.DX_DEVICE);
                uint pEnd = MemoryManager.Memory.ReadUInt(pDevice + (uint)Dx9.DX_DEVICE_IDX);
                uint pScene = MemoryManager.Memory.ReadUInt(pEnd);
                uint pEndScene = MemoryManager.Memory.ReadUInt(pScene + (uint)Dx9.ENDSCENE_IDX);

                cachedEndScenePtr = pEndScene;
                return pEndScene;
            }

            private void Hooking()
            {
                

                if (!MemoryManager.Memory.IsProcessOpen)
                    MemoryManager.Memory.OpenProcessAndThread((int)processId);

                if (!MemoryManager.Memory.IsProcessOpen)
                    return;

                uint EndScenePtr = GetEndScene();
                if (MemoryManager.Memory.ReadByte(EndScenePtr) == 0xE9 && (injectedCode == 0 || injectionAddress == 0))
                    Dispose();

                if (MemoryManager.Memory.ReadByte(EndScenePtr) != 0xE9)
                {
                    try
                    {
                        ThreadHooked = false;

                        injectedCode = MemoryManager.Memory.AllocateMemory(2048);
                        injectionAddress = MemoryManager.Memory.AllocateMemory(4);
                        returnValueAddress = MemoryManager.Memory.AllocateMemory(4);

                        MemoryManager.Memory.WriteInt(injectionAddress, 0);
                        MemoryManager.Memory.WriteInt(returnValueAddress, 0);

                        string[] asmStub = new string[]
                        {
                            "pushad",
                            "pushfd",
                            string.Format("mov eax, [{0}]", injectionAddress),
                            "test eax, eax",
                            "je @out",
                            string.Format("mov eax, [{0}]", injectionAddress),
                            "call eax",
                            string.Format("mov [{0}], eax", returnValueAddress),
                            string.Format("mov edx, {0}", injectionAddress),
                            "mov ecx, 0",
                            "mov [edx], ecx",
                            "@out:",
                            "popfd",
                            "popad"
                        };

                        MemoryManager.Memory.Asm.Clear();
                        foreach (string line in asmStub)
                            MemoryManager.Memory.Asm.AddLine(line);

                        uint stubSize = (uint)MemoryManager.Memory.Asm.Assemble().Length;
                        MemoryManager.Memory.Asm.Inject(injectedCode);

                        originalBytes = MemoryManager.Memory.ReadBytes(EndScenePtr, SizeJumpBack);
                        MemoryManager.Memory.WriteBytes(injectedCode + stubSize, originalBytes);

                        MemoryManager.Memory.Asm.Clear();
                        MemoryManager.Memory.Asm.AddLine("jmp " + (EndScenePtr + SizeJumpBack));
                        MemoryManager.Memory.Asm.Inject(injectedCode + stubSize + (uint)SizeJumpBack);

                        MemoryManager.Memory.Asm.Clear();
                        MemoryManager.Memory.Asm.AddLine("jmp " + injectedCode);
                        MemoryManager.Memory.Asm.Inject(EndScenePtr);



                        Console.WriteLine("Hooked EndScene at 0x" + EndScenePtr.ToString("X"));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Hooking failed: " + ex.Message);
                        ThreadHooked = false;
                    }
                }

                ThreadHooked = true;
            }

            public void Dispose()
            {
                try
                {
                    if (!ThreadHooked || injectedCode == 0 || injectionAddress == 0 || originalBytes == null || originalBytes.Length != 7)
                        return;

                    uint endScenePtr = GetEndScene();

                    // Restore original instructions
                    MemoryManager.Memory.WriteBytes(endScenePtr, originalBytes);

                    // Free allocated memory
                    MemoryManager.Memory.FreeMemory(injectedCode);
                    MemoryManager.Memory.FreeMemory(injectionAddress);
                    MemoryManager.Memory.FreeMemory(returnValueAddress);

                    injectedCode = 0;
                    injectionAddress = 0;
                    returnValueAddress = 0;
                    cachedEndScenePtr = null;
                    ThreadHooked = false;

                    Console.WriteLine("Hook properly disposed.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error while disposing hook: " + ex.Message);
                }
            }


            public byte[] InjectAndExecute(string[] asm, int returnLength = 0)
            {
                lock (injectionLock)
                {
                    Hooking();

                    byte[] result = new byte[0];
                    MemoryManager.Memory.WriteInt(returnValueAddress, 0);

                    if (MemoryManager.Memory.IsProcessOpen && ThreadHooked)
                    {
                        MemoryManager.Memory.Asm.Clear();
                        foreach (string line in asm)
                            MemoryManager.Memory.Asm.AddLine(line);

                        uint asmLength = (uint)MemoryManager.Memory.Asm.Assemble().Length;
                        uint remoteCode = MemoryManager.Memory.AllocateMemory((int)asmLength);

                        try
                        {
                            MemoryManager.Memory.Asm.Inject(remoteCode);
                            MemoryManager.Memory.WriteInt(injectionAddress, (int)remoteCode);

                            while (MemoryManager.Memory.ReadInt(injectionAddress) > 0)
                                Thread.Sleep(1);

                            if (returnLength > 0)
                            {
                                result = MemoryManager.Memory.ReadBytes(MemoryManager.Memory.ReadUInt(returnValueAddress), returnLength);
                            }
                        }
                        catch { }
                        finally
                        {
                            MemoryManager.Memory.FreeMemory(remoteCode);
                        }
                    }

                    return result;
                }
            }
        }
    }
}
