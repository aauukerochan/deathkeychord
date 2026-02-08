using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace DeathKeyChord.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private readonly Plugin plugin;

    private static readonly int[] DelayOptionsMs =
    {
        0,
        250,
        500,
        750,
        1000,
        1500,
        2000,
        3000,
        4000,
        5000
    };

    public ConfigWindow(Plugin plugin) : base("DeathKeyChord Settings###DeathKeyChordConfig")
    {
        Flags = ImGuiWindowFlags.NoCollapse & ~ImGuiWindowFlags.NoMove;

        Size = new Vector2(520, 360);
        SizeCondition = ImGuiCond.FirstUseEver;

        this.plugin = plugin;
        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void PreDraw() { }

    public override void Draw()
    {
        if (!ImGui.IsWindowAppearing() && plugin.IsTestHoldActive && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            plugin.EndTestHold();
        }

        if (!plugin.CanInject())
        {
            ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f),
                "Key injection is only supported on Windows-like runtimes (Windows / Wine / Proton).");
            ImGui.Separator();
        }

        DrawEnabledCheckbox();

        DrawHoldCheckbox();

        ImGui.Separator();
        DrawChordConfig();

        ImGui.Separator();

        DrawMuteDelayConfig();

        ImGui.Separator();

        DrawDebugConfig();
    }

    private void DrawDebugConfig()
    {
        ImGui.Text("Debug");

        var dbg = configuration.DebugEnabled;
        if (ImGui.Checkbox("Enable debug logging", ref dbg))
        {
            configuration.DebugEnabled = dbg;
            configuration.Save();
        }

        var chat = configuration.DebugChatEcho;
        if (ImGui.Checkbox("Also print debug to chat", ref chat))
        {
            configuration.DebugChatEcho = chat;
            configuration.Save();
        }

        var verbose = configuration.DebugVerboseUpdate;
        if (ImGui.Checkbox("Verbose periodic state logs", ref verbose))
        {
            configuration.DebugVerboseUpdate = verbose;
            configuration.Save();
        }
    }

    private void DrawMuteDelayConfig()
    {
        ImGui.Text("Mute delay");
        var current = configuration.MuteDelayMs;
        var delayIdx = Array.IndexOf(DelayOptionsMs, current);

        if (delayIdx < 0)
        {
            delayIdx = Array.FindIndex(DelayOptionsMs, v => v >= current);
            if (delayIdx < 0)
                delayIdx = DelayOptionsMs.Length - 1;
        }

        if (ImGui.BeginCombo("Delay before muting", FormatDelay(DelayOptionsMs[delayIdx])))
        {
            for (var i = 0; i < DelayOptionsMs.Length; i++)
            {
                var selected = i == delayIdx;

                if (ImGui.Selectable(FormatDelay(DelayOptionsMs[i]), selected))
                {
                    configuration.MuteDelayMs = DelayOptionsMs[i];
                    configuration.Save();
                }

                if (selected)
                    ImGui.SetItemDefaultFocus();
            }

            ImGui.EndCombo();
        }
        ImGui.TextWrapped("The mute will only activate if you remain dead for at least this long.");
    }

    private void DrawEnabledCheckbox()
    {
        var enabled = configuration.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
        {
            configuration.Enabled = enabled;
            configuration.Save();
        }
    }

    private void DrawHoldCheckbox()
    {
        var hold = configuration.HoldWhileDead;
        if (ImGui.Checkbox("Hold chord while dead (recommended)", ref hold))
        {
            configuration.HoldWhileDead = hold;
            configuration.Save();
        }
    }

    private void DrawChordConfig()
    {
        ImGui.Text("Chord");

        var ctrl = configuration.ModCtrl;
        var alt = configuration.ModAlt;
        var shift = configuration.ModShift;
        var win = configuration.ModWin;

        if (ImGui.Checkbox("Ctrl", ref ctrl))
        {
            configuration.ModCtrl = ctrl;
            configuration.Save();
        }
        ImGui.SameLine();
        if (ImGui.Checkbox("Alt", ref alt))
        {
            configuration.ModAlt = alt;
            configuration.Save();
        }
        ImGui.SameLine();
        if (ImGui.Checkbox("Shift", ref shift))
        {
            configuration.ModShift = shift;
            configuration.Save();
        }
        ImGui.SameLine();
        if (ImGui.Checkbox("Win", ref win))
        {
            configuration.ModWin = win;
            configuration.Save();
        }

        var choices = VkChoices.Common;
        var idx = Array.FindIndex(choices, c => c.Vk == configuration.MainVk);
        if (idx < 0) idx = Array.FindIndex(choices, c => c.Vk == 0x4D); // M
        if (idx < 0) idx = 0;

        if (ImGui.BeginCombo("Main key", choices[idx].Label))
        {
            for (var i = 0; i < choices.Length; i++)
            {
                var selected = choices[i].Vk == configuration.MainVk;
                if (ImGui.Selectable(choices[i].Label, selected))
                {
                    configuration.MainVk = choices[i].Vk;
                    configuration.Save();
                }

                if (selected)
                    ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }

        ImGui.Text($"Current: {plugin.GetChordLabel()}");

        var noMods = !configuration.ModCtrl && !configuration.ModAlt && !configuration.ModShift && !configuration.ModWin;
        if (noMods)
        {
            ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f),
                "⚠ Using a single key w/o modifiers may conflict with other apps.");
        }

        var risky =
            configuration.MainVk < 0x7C || configuration.MainVk > 0x87; // not F13–F24

        if (risky)
        {
            ImGui.TextColored(
                new Vector4(1f, 0.6f, 0.2f, 1f),
                "⚠ Non-F13–F24 keys may conflict with other apps or games."
            );
        }

        ImGui.Spacing();

        ImGui.Button("Test chord (hold)");
        var active = ImGui.IsItemActive();

        if (active && !plugin.IsTestHoldActive)
        {
            plugin.BeginTestHold();
        }
        else if (!active && plugin.IsTestHoldActive)
        {
            plugin.EndTestHold();
        }

        ImGui.SameLine();
        ImGui.TextUnformatted("(hold mouse down)");

        if (ImGui.Button("Reset to recommended default (F24)"))
        {
            configuration.ModCtrl = false;
            configuration.ModAlt = false;
            configuration.ModShift = false;
            configuration.ModWin = false;

            configuration.MainVk = 0x87; // F24
            configuration.HoldWhileDead = true;

            configuration.Save();
        }

        ImGui.TextWrapped(
            "F24 is an OS-level function key that almost no apps use. "
            + "It’s ideal for automation and won’t interfere with gameplay."
        );
    }

    private static string FormatDelay(int ms)
    {
        if (ms == 0)
            return "No delay";
        
        if (ms < 1000)
            return $"{ms} ms";
        
        if (ms % 1000 == 0)
            return $"{ms / 1000} s";
        
        return $"{ms / 1000f:0.0} s";
    }
}

