using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using Js;
using NOAH.Core;
using Puerts;

namespace BBEntropyEffectMinimalRepro;

/// <summary>
/// Minimal, self-contained JS dumper.
///
/// It reads every data/js/** resource from StreamingAssets/ab/merge.json,
/// decrypts it through the game's own JsLoader, and writes the plaintext.
/// </summary>
internal static class MinimalRepro
{
    private const int BatchSize = 100;

    private static bool _initialized;
    private static bool _completed;
    private static bool _warned;
    private static float _nextAttempt;

    private static string _dumpDirectory;
    private static ILoader _loader;
    private static readonly List<string> _pending = new();
    private static int _index;
    private static int _success;
    private static int _failed;

    public static void Tick()
    {
        if (_completed)
            return;

        var now = SafeNow();
        if (now < 10f || now < _nextAttempt)
            return;

        _nextAttempt = now + 1f;

        try
        {
            if (!_initialized && !Initialize())
                return;

            ProcessBatch();
        }
        catch (Exception e)
        {
            if (!_warned)
            {
                _warned = true;
                Plugin.Log.LogWarning($"[MinimalRepro] {e.Message}");
            }
        }
    }

    private static bool Initialize()
    {
        var manager = SingletonBehaviour<JsManager>.s_instance;
        if (manager == null)
            return false;

        var loader = manager.JsLoader;
        if (loader == null)
            return false;

        var mergePath = FindMergeJson();
        if (mergePath == null)
        {
            if (!_warned)
            {
                _warned = true;
                Plugin.Log.LogWarning("[MinimalRepro] merge.json not found.");
            }
            return false;
        }

        _dumpDirectory = Path.Combine(
            Paths.ConfigPath,
            "BBEntropyEffectMinimalRepro",
            "js_dump");

        Directory.CreateDirectory(_dumpDirectory);

        _pending.Clear();
        foreach (var line in File.ReadLines(mergePath))
        {
            var resource = ExtractResourcePath(line);
            if (resource == null)
                continue;

            if (!resource.StartsWith("data/js/", StringComparison.Ordinal))
                continue;

            if (!resource.EndsWith(".ab", StringComparison.Ordinal))
                continue;

            _pending.Add(resource);
        }

        _pending.Sort(StringComparer.Ordinal);

        _loader = loader;
        _initialized = true;

        Plugin.Log.LogInfo($"[MinimalRepro] Found {_pending.Count} JS resources in {mergePath}");
        return true;
    }

    private static void ProcessBatch()
    {
        var end = Math.Min(_index + BatchSize, _pending.Count);

        for (; _index < end; _index++)
        {
            var resource = _pending[_index];
            var module = ToModulePath(resource);

            var text = ReadAny(_loader, resource, module);
            if (string.IsNullOrEmpty(text))
            {
                _failed++;
                Plugin.Log.LogDebug($"[MinimalRepro] failed: {resource}");
                continue;
            }

            var path = Path.Combine(_dumpDirectory, module + ".js");
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(path, text, new UTF8Encoding(false));
            _success++;
        }

        if (_index >= _pending.Count)
        {
            _completed = true;
            Plugin.Log.LogInfo(
                $"[MinimalRepro] Done: {_success} dumped, {_failed} failed, {_pending.Count} total -> {_dumpDirectory}");
        }
        else if (_index % 250 == 0)
        {
            Plugin.Log.LogInfo($"[MinimalRepro] Progress: {_index}/{_pending.Count}");
        }
    }

    private static string ReadAny(ILoader loader, string resource, string module)
    {
        var candidates = new[]
        {
            module,
            module + ".js",
            "data/js/" + module,
            "data/js/" + module + ".js",
            resource,
        };

        foreach (var candidate in candidates)
        {
            try
            {
                var text = ReadFile(loader, candidate, out _);
                if (string.IsNullOrEmpty(text))
                    continue;

                if (string.Equals(text, candidate, StringComparison.Ordinal))
                    continue;

                return text;
            }
            catch (Exception e)
            {
                Plugin.Log.LogDebug($"[MinimalRepro] \"{candidate}\" failed: {e.Message}");
            }
        }

        return null;
    }

    private static string ReadFile(ILoader loader, string path, out string debugPath)
    {
        debugPath = null;

        var runtime = loader.TryCast<RuntimeLoader>();
        if (runtime != null)
            return runtime.ReadFile(path, out debugPath);

        var dev = loader.TryCast<DevLoader>();
        if (dev != null)
            return dev.ReadFile(path, out debugPath);

        var defaultLoader = loader.TryCast<DefaultLoader>();
        if (defaultLoader != null)
            return defaultLoader.ReadFile(path, out debugPath);

        return null;
    }

    private static string FindMergeJson()
    {
        var candidates = new[]
        {
            Path.Combine(UnityEngine.Application.streamingAssetsPath, "ab", "merge.json"),
            Path.Combine(Paths.GameRootPath, "BlazblueEntropyEffect_Data", "StreamingAssets", "ab", "merge.json"),
        };

        foreach (var candidate in candidates)
        {
            try
            {
                if (File.Exists(candidate))
                    return candidate;
            }
            catch
            {
                // try the next candidate
            }
        }

        return null;
    }

    private static string ExtractResourcePath(string line)
    {
        const string marker = "\"r\": \"";
        var start = line.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
            return null;

        start += marker.Length;
        var end = line.IndexOf('"', start);
        if (end < 0)
            return null;

        return line.Substring(start, end - start);
    }

    private static string ToModulePath(string resource)
    {
        // data/js/core/util.ab -> core/util
        var module = resource.Substring("data/js/".Length);
        if (module.EndsWith(".ab", StringComparison.Ordinal))
            module = module.Substring(0, module.Length - 3);
        if (module.EndsWith(".js", StringComparison.Ordinal))
            module = module.Substring(0, module.Length - 3);
        return module;
    }

    private static float SafeNow()
    {
        try
        {
            return UnityEngine.Time.realtimeSinceStartup;
        }
        catch
        {
            return float.MaxValue;
        }
    }
}
