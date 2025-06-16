# WoW Lua Injection via EndScene Hook (Windows 11 Compatible)

This project demonstrates how to inject and execute Lua commands in **World of Warcraft** by hooking the **EndScene** function in DirectX 9 using inline shellcode.

The method is a modernized version of the original concept from [OwnedCore](https://www.ownedcore.com/forums/world-of-warcraft/world-of-warcraft-bots-programs/wow-memory-editing/305473-sample-code-endscene-hook-asm-blackmagic.html), updated to:

* Work cleanly on **Windows 10/11**
* Avoid freezing or crashing WoW
* Support clean stack return and hook restoration
* Use inline shellcode without relying on hardware breakpoints

---

## ✅ Features

* 🔗 Hooks `EndScene` via inline jump and executes custom shellcode
* 🧠 Injects and runs `Lua_DoString` inside WoW to cast spells or run any Lua command
* 💡 Preserves original EndScene bytes and restores them after execution
* 🧵 Runs in its own thread with retry-safe logic
* 🧼 Includes memory cleanup after shellcode executes

---

## 💻 Requirements

* .NET Framework 4.6 or later
* A working [BlackMagic]([https://github.com/drewsonne/BlackMagic](https://github.com/acidburn974/Blackmagic)) DLL
* 32-bit WoW client (for x86 shellcode injection)
* Admin privileges to access external processes

---

## 🚀 How It Works

1. **Scan and locate** the DirectX9 `EndScene` function pointer
2. **Allocate memory** for shellcode that calls `Lua_DoString("...")`
3. **Inject a jump** to the shellcode from the beginning of `EndScene`
4. **Shellcode executes**, then jumps back to original `EndScene + 7`
5. **Memory is freed**, and original bytes are restored

---

## 📜 Credits

* 💻 **Original implementation by \[RivaLfr]** (OwnedCore / Private source)
* 🔧 Code modernization, bug fixes, and Windows 11 compatibility by **\[TechMecca]**

