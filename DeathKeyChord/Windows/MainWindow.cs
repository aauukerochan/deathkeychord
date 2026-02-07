using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace DeathKeyChord.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    public MainWindow(Plugin plugin)
        : base("DeathKeyChord##Main", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(330, 150),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.TextUnformatted("DeathKeyChord");
        ImGui.Separator();

        ImGui.Text($"Enabled: {(plugin.Configuration.Enabled ? "Yes" : "No")}");
        ImGui.Text($"Mode: {(plugin.Configuration.HoldWhileDead ? "Hold while dead" : "Tap on death/rez")}");
        ImGui.Text($"Chord: {plugin.GetChordLabel()}");

        ImGui.Spacing();

        if (ImGui.Button("Open Settings"))
            plugin.ToggleConfigUi();

        ImGui.SameLine();

        var hp = plugin.DebugHp;
        ImGui.Text($"HP: {(hp.HasValue ? hp.Value.ToString() : "n/a")}");
        ImGui.Text($"Detected dead: {plugin.DebugWasDead}");
        ImGui.Text($"Chord held: {plugin.DebugChordHeld}");
    }
}

