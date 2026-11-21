# BbEeExtractJsFiles

Minimal BepInEx IL2CPP plugin that dumps the game's encrypted JavaScript
modules to plaintext files.

## What it does

1. Reads `StreamingAssets/ab/merge.json`.
2. Collects every `data/js/**` resource path.
3. Calls the game's own `JsLoader` (`RuntimeLoader` / `DevLoader` /
   `DefaultLoader`) to decrypt each `TextAsset`.
4. Writes the plaintext to:

```
<game>/BepInEx/config/BBEntropyEffectMinimalRepro/js_dump/<module>.js
```

Example output:

```
js_dump/core/util.js
js_dump/core/potentialutil.js
js_dump/server/model/breeddungeon.js
js_dump/window/battle/battlechoosepotential.js
```

## Requirements

- BlazBlue Entropy Effect (Steam PC)
- BepInEx 6 IL2CPP
- .NET 6 SDK
- The game's IL2CPP interop assemblies

## Setup the interop assemblies

This repository does **not** include any game DLLs. Create a `dlls/` folder
next to `MinimalRepro.csproj` and copy these files from the game's
`BepInEx/interop` folder:

```
Assembly-CSharp.dll
UnityEngine.dll
UnityEngine.CoreModule.dll
Il2Cppmscorlib.dll
NOAH.Core.Runtime.dll
com.tencent.puerts.core.dll
```

If your interop files live elsewhere:

```bash
dotnet build -c Release -p:DllsDir="/path/to/dlls"
```

## Build

```bash
dotnet build -c Release
```

Output:

```
bin/Release/net6.0/BBEntropyEffectMinimalRepro.dll
```

## Deploy

Copy the DLL to the game's plugin folder:

```bash
GAME_DIR="<path-to-BlazblueEntropyEffect>"
cp bin/Release/net6.0/BBEntropyEffectMinimalRepro.dll \
   "$GAME_DIR/BepInEx/plugins/"
```

Start the game and wait about 20 seconds.

## Logs

The plugin logs with the prefix:

```
BBEntropyEffect Minimal JS Dump Repro
```

Expected output:

```
[MinimalRepro] Found 1034 JS resources in .../ab/merge.json
[MinimalRepro] Progress: 250/1034
...
[MinimalRepro] Done: 1034 dumped, 0 failed, 1034 total -> .../js_dump
```

## Notes

- 100 modules are processed per `Update` tick.
- The plugin retries initialization until `JsManager` and `merge.json` are available.
- Some `data/js/**` entries are source maps or other non-module TextAssets; they are still written as `.js`.
- Failed entries are logged as `[MinimalRepro] failed: data/js/...`.
- No game DLLs or build outputs are tracked in this repository.
