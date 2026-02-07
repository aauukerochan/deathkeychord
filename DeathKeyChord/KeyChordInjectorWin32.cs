using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Dalamud.Plugin.Services;

internal sealed class KeyChordInjectorWin32
{
    private readonly IPluginLog log;
    public KeyChordInjectorWin32(IPluginLog log) => this.log = log;

    private const uint INPUT_KEYBOARD = 1;

    private const uint KEYEVENTF_KEYUP = 0x0002;

    private const ushort VK_LCONTROL = 0xA2;
    private const ushort VK_LMENU    = 0xA4; // Left Alt
    private const ushort VK_LSHIFT   = 0xA0;
    private const ushort VK_LWIN     = 0x5B;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public nuint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public nuint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    private static INPUT KeyDownVk(ushort vk) => new()
    {
        type = INPUT_KEYBOARD,
        U = new InputUnion { ki = new KEYBDINPUT { wVk = vk, wScan = 0, dwFlags = 0 } }
    };

    private static INPUT KeyUpVk(ushort vk) => new()
    {
        type = INPUT_KEYBOARD,
        U = new InputUnion { ki = new KEYBDINPUT { wVk = vk, wScan = 0, dwFlags = KEYEVENTF_KEYUP } }
    };

    private static List<ushort> GetMods(bool ctrl, bool alt, bool shift, bool win)
    {
        var mods = new List<ushort>(4);
        if (ctrl) mods.Add(VK_LCONTROL);
        if (alt) mods.Add(VK_LMENU);
        if (shift) mods.Add(VK_LSHIFT);
        if (win) mods.Add(VK_LWIN);
        return mods;
    }

    private void Send(string label, List<INPUT> inputs)
    {
        var arr = inputs.ToArray();
        var sent = SendInput((uint)arr.Length, arr, Marshal.SizeOf<INPUT>());

        if (sent != arr.Length)
        {
            int err = Marshal.GetLastWin32Error();
            log.Warning($"SendInput({label}) sent {sent}/{arr.Length}. Win32Error={err} ({new Win32Exception(err).Message}). INPUT size={Marshal.SizeOf<INPUT>()}");
        }
        else
        {
            log.Information($"SendInput({label}) OK ({sent} events). INPUT size={Marshal.SizeOf<INPUT>()}");
        }
    }

    public void HoldChordDown(bool ctrl, bool alt, bool shift, bool win, ushort mainVk)
    {
        var mods = GetMods(ctrl, alt, shift, win);
        var inputs = new List<INPUT>(mods.Count + 1);

        foreach (var m in mods) inputs.Add(KeyDownVk(m));
        inputs.Add(KeyDownVk(mainVk));

        Send("HoldDown", inputs);
    }

    public void HoldChordUp(bool ctrl, bool alt, bool shift, bool win, ushort mainVk)
    {
        var mods = GetMods(ctrl, alt, shift, win);
        var inputs = new List<INPUT>(mods.Count + 1);

        inputs.Add(KeyUpVk(mainVk));
        for (int i = mods.Count - 1; i >= 0; i--) inputs.Add(KeyUpVk(mods[i]));

        Send("HoldUp", inputs);
    }

    public void TapChord(bool ctrl, bool alt, bool shift, bool win, ushort mainVk)
    {
        var mods = GetMods(ctrl, alt, shift, win);
        var inputs = new List<INPUT>(mods.Count * 2 + 2);

        foreach (var m in mods) inputs.Add(KeyDownVk(m));
        inputs.Add(KeyDownVk(mainVk));
        inputs.Add(KeyUpVk(mainVk));
        for (int i = mods.Count - 1; i >= 0; i--) inputs.Add(KeyUpVk(mods[i]));

        Send("Tap", inputs);
    }
}
