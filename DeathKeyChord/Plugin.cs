using System;
using System.Runtime.InteropServices;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using DeathKeyChord.Windows;

namespace DeathKeyChord;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;


    private const string CommandName = "/deathkeychord";

    public Configuration Configuration { get; }

    public readonly WindowSystem WindowSystem = new("DeathKeyChord");
    private ConfigWindow ConfigWindow { get; }
    private MainWindow MainWindow { get; }

    private readonly KeyChordInjectorWin32 injector;

    private bool wasDead;
    private bool chordHeld;
    private long lastActionAtMs;

    private const int DebounceMs = 500;

    private long lastDebugTickAtMs;
    public bool DebugWasDead => wasDead;
    public bool DebugChordHeld => chordHeld;
    public uint? DebugHp => ObjectTable.LocalPlayer?.CurrentHp;


    public Plugin()
    {
        injector = new KeyChordInjectorWin32(Log);
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the DeathKeyChord window."
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        Framework.Update += OnFrameworkUpdate;

        Debug($"[${PluginInterface.Manifest.Name}] Loaded. Enabled={Configuration.Enabled}, Mode={(Configuration.HoldWhileDead ? "Hold" : "Tap")}, Chord={GetChordLabel()}", force: true);
        Debug($"CanInject={CanInject()}");
    }

    public void Dispose()
    {
        SafeReleaseChord();
        EndTestHold();

        Framework.Update -= OnFrameworkUpdate;

        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args) => ToggleMainUi();

    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();

    public string GetChordLabel() => ChordFormatting.Format(Configuration);

    public bool CanInject()
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    private bool testHoldActive;

    public bool IsTestHoldActive => testHoldActive;

    public void BeginTestHold()
    {
        if (!CanInject() || testHoldActive) return;

        injector.HoldChordDown(Configuration.ModCtrl, Configuration.ModAlt, Configuration.ModShift, Configuration.ModWin, Configuration.MainVk);
        testHoldActive = true;
        Log.Information($"BeginTestHold: {GetChordLabel()}");
    }

    public void EndTestHold()
    {
        if (!CanInject() || !testHoldActive) return;

        injector.HoldChordUp(Configuration.ModCtrl, Configuration.ModAlt, Configuration.ModShift, Configuration.ModWin, Configuration.MainVk);
        testHoldActive = false;
        Log.Information($"EndTestHold: {GetChordLabel()}");
    }

    private void OnFrameworkUpdate(IFramework _)
    {
        if (!Configuration.Enabled)
        {
            // If user disables the plugin while chord is held, release it.
            if (chordHeld) SafeReleaseChord();
            return;
        }

        if (!CanInject())
        {
            // Nothing to do (e.g. if someone ever ran this in a non-Windows runtime)
            if (chordHeld) SafeReleaseChord();
            return;
        }

        var player = ObjectTable.LocalPlayer;
        if (player == null)
        {
            // Not logged in; ensure we aren't holding anything.
            if (chordHeld) SafeReleaseChord();
            wasDead = false;
            return;
        }

        if (!PlayerState.IsLoaded)
        {
            // Changing zones
            if (chordHeld) SafeReleaseChord();
            wasDead = false;
            return;
        }

        if (Configuration.DebugEnabled && Configuration.DebugVerboseUpdate)
        {
            var now = Environment.TickCount64;
            if (now - lastDebugTickAtMs >= Configuration.DebugUpdateIntervalMs)
            {
                lastDebugTickAtMs = now;
                Debug($"Tick: wasDead={wasDead}, chordHeld={chordHeld}, HP={player.CurrentHp}, Enabled={Configuration.Enabled}, CanInject={CanInject()}");
            }
        }



        bool isDead = player.CurrentHp <= 0;

        long nowMs = Environment.TickCount64;
        bool canAct = (nowMs - lastActionAtMs) > DebounceMs;

        // transitions
        if (!wasDead && isDead && canAct)
        {
            Debug($"Death detected. HP={player.CurrentHp}. Mode={(Configuration.HoldWhileDead ? "Hold" : "Tap")}");
            wasDead = true;
            lastActionAtMs = nowMs;
            OnDeath();
        }
        else if (wasDead && !isDead && canAct)
        {
            Debug($"Revive detected. HP={player.CurrentHp}. Mode={(Configuration.HoldWhileDead ? "Hold" : "Tap")}");
            wasDead = false;
            lastActionAtMs = nowMs;
            OnRevive();
        }
    }

    private void OnDeath()
    {
        Debug($"OnDeath: chord={GetChordLabel()}");

        if (Configuration.HoldWhileDead)
        {
            if (!chordHeld)
            {
                injector.HoldChordDown(Configuration.ModCtrl, Configuration.ModAlt, Configuration.ModShift, Configuration.ModWin, Configuration.MainVk);
                chordHeld = true;
                Debug("OnDeath: HoldChordDown sent.");
            } 
            else
            {
               Debug("OnDeath: chord already held; no action."); 
            }
        }
        else
        {
            injector.TapChord(Configuration.ModCtrl, Configuration.ModAlt, Configuration.ModShift, Configuration.ModWin, Configuration.MainVk);
            Debug("OnDeath: TapChord sent.");
        }
    }

    private void OnRevive()
    {
        Debug($"OnRevive: chord={GetChordLabel()}");

        if (Configuration.HoldWhileDead)
        {
            SafeReleaseChord();
        }
        else
        {
            injector.TapChord(Configuration.ModCtrl, Configuration.ModAlt, Configuration.ModShift, Configuration.ModWin, Configuration.MainVk);
            Debug("OnRevive: TapChord sent.");
        }
    }

    private void SafeReleaseChord()
    {
        Debug("SafeReleaseChord: releasing chord (if held).");

        if (!CanInject())
        {
            chordHeld = false;
            Debug("SafeReleaseChord: CanInject=false; cleared internal state only.");
            return;
        }

        // Release even if we "think" it isn't held; safe and prevents stuck modifiers.
        injector.HoldChordUp(Configuration.ModCtrl, Configuration.ModAlt, Configuration.ModShift, Configuration.ModWin, Configuration.MainVk);
        chordHeld = false;
        Debug("SafeReleaseChord: HoldChordUp sent.");
    }

    private void Debug(string message, bool force = false)
    {
        if (!Configuration.DebugEnabled && !force)
            return;

        Log.Information($"[DeathKeyChord] {message}");

        if (Configuration.DebugChatEcho)
        {
            try
            {
                ChatGui.Print($"[DeathKeyChord] {message}");
            }
            catch
            {
                // ignore chat failures
            }
        }
    }
}
