using RivaLfr;
using RivaLfr.Memory;
using System;
using System.Diagnostics;
using System.Threading;

internal class Program
{
    static void Main(string[] args)
    {
        // Setup graceful exit handler
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true; // Cancel immediate termination
            OnProcessExit(sender, e);
            Environment.Exit(0);
        };

        // Look for a process named "Wow"
        Process wowProcess = null;

        foreach (var proc in Process.GetProcesses())
        {
            if (proc.ProcessName.Equals("Wow", StringComparison.OrdinalIgnoreCase))
            {
                wowProcess = proc;
                break;
            }
        }

        if (wowProcess == null)
        {
            Console.WriteLine("Wow.exe is not running.");
        }
        else
        {
            MemoryManager.Initialize((uint)wowProcess.Id);
            Console.WriteLine("Injection hook initialized.");
            Lua.LuaDoString("CastSpellByName('Healing Wave')");
        }

        Console.WriteLine("Press Ctrl+C to exit.");
        while (true)
            Thread.Sleep(100); // Keep the app running
    }

    static void OnProcessExit(object sender, EventArgs e)
    {
        if (MemoryManager.Hook != null)
        {
            MemoryManager.Hook.Dispose();
        }
    }
}
