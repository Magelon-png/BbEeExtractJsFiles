using System;
using System.Threading;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

namespace BBEntropyEffectMinimalRepro;

[BepInPlugin(Guid, Name, Version)]
public class Plugin : BasePlugin
{
    public const string Guid = "BBEntropyEffectMinimalRepro";
    public const string Name = "BBEntropyEffect Minimal JS Dump Repro";
    public const string Version = "1.0.0";

    internal static new ManualLogSource Log;
    internal static Plugin Instance;
    internal static int MainThreadId;

    public override void Load()
    {
        Instance = this;
        Log = base.Log;
        MainThreadId = Thread.CurrentThread.ManagedThreadId;

        try
        {
            PluginRunner.Create();
        }
        catch (Exception e)
        {
            Log.LogError("Failed to create the minimal repro runner.");
            Log.LogError(e);
        }

        Log.LogInfo($"{Guid} loaded. Output: BepInEx/config/BBEntropyEffectMinimalRepro/js_dump");
    }
}
