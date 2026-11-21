using System;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace BBEntropyEffectMinimalRepro;

/// <summary>
/// Waits for the game to finish loading, then drives the JS dumper.
/// No GameGlobal reflection: MinimalRepro.Tick() retries until JsManager exists.
/// </summary>
public class PluginRunner : MonoBehaviour
{
    private const float StartDelaySeconds = 20f;
    private float _nextTick;

    public void Update()
    {
        var now = UnityEngine.Time.realtimeSinceStartup;
        if (now < StartDelaySeconds || now < _nextTick)
            return;

        _nextTick = now + 1f;

        try
        {
            MinimalRepro.Tick();
        }
        catch (Exception e)
        {
            Plugin.Log.LogDebug($"[Runner] {e}");
        }
    }

    public static void Create()
    {
        ClassInjector.RegisterTypeInIl2Cpp<PluginRunner>();

        var go = new GameObject("BBEntropyEffectMinimalReproRunner");
        go.hideFlags = HideFlags.HideAndDontSave;
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<PluginRunner>();

        Plugin.Log.LogInfo("[Runner] Minimal repro runner created.");
    }
}
