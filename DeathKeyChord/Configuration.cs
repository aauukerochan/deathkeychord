
using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace DeathKeyChord;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public void Initialize(IDalamudPluginInterface pi) => pluginInterface = pi;
    public void Save() => pluginInterface?.SavePluginConfig(this);

    public bool IsConfigWindowMovable = false;

    public bool Enabled = true;

    public bool HoldWhileDead = true;

    public bool ModCtrl = false;
    public bool ModAlt = false;
    public bool ModShift = false;
    public bool ModWin = false;

    public ushort MainVk = 0x87; // VK_f24

    public bool UseMuteDelay = true;
    public int MuteDelayMs = 500;

    public bool DebugEnabled = false;
    public bool DebugChatEcho = false;
    public bool DebugVerboseUpdate = false;
    public int DebugUpdateIntervalMs = 1000;

}
