# CoreCLR Interpreter Hot-Update Smoke Test

Evaluates multiple C# hot-update approaches for a game engine architecture (C++ host + C# scripting), with a focus on iOS compatibility where JIT is blocked by W^X policy.

## Conclusion

CoreCLR interpreter (coming in .NET 11) is the winning approach: same C# code, same DLL, same toolchain across all platforms. The runtime automatically selects JIT (Windows/Android) or interpreter (iOS). No second language, no third-party interpreter, no platform branching in user code.

**Browser WASM support validated**: The same plugin DLL also runs in Browser WASM via Mono's AOT host + interpreter hybrid — AOT-compiled BCL/engine at native WASM speed, dynamically loaded plugins interpreted at runtime with only 2-3x overhead. All four target platforms share one plugin DLL.

## Projects

### Core Architecture (NativeHost)

The three-layer architecture prototype:

```
C++ engine (host.cpp)
  └─ GameEngine.dll (C# engine layer, loaded via hostfxr)
       ├─ PluginManager: AssemblyLoadContext loading + IL security scanning
       └─ SamplePlugin.dll (hot-update plugin, implements IGamePlugin)
```

| Directory | Description |
|-----------|-------------|
| `NativeHost/src/` | C++ host embedding CoreCLR via hostfxr |
| `NativeHost/managed/GameEngine/` | C# engine layer — plugin lifecycle, IL scanning, native interop |
| `NativeHost/managed/PluginApi/` | Shared interface (`IGamePlugin`, `IEngineServices`) |
| `NativeHost/managed/SamplePlugin/` | Example hot-update plugin |

### Security

| Directory | Description |
|-----------|-------------|
| `IlScanner/` | Mono.Cecil-based IL scanner — blocks dangerous APIs (file I/O, networking, reflection, P/Invoke, unsafe) before loading plugin DLLs |
| `TestPluginSafe/` | Clean plugin that passes scanning |
| `TestPluginDangerous/` | Plugin with file I/O, networking, P/Invoke, unsafe — rejected by scanner |

### Benchmarks

| Directory | Description |
|-----------|-------------|
| `CoreClrInterpTest/` | CoreCLR interpreter benchmarks (dict lookup, math, interface dispatch, cross-call, etc.) |
| `BenchLib/` | R2R-compiled benchmark library for Mode 1 native vs interpreted comparison |
| `LuaHost/` | C++ host embedding Lua 5.4 with equivalent benchmarks |
| `Wasi/` | WASI-WASM (Mono interpreter) baseline |
| `ILRuntimeSmokeTest/` | Third-party ILRuntime interpreter baseline |
| `WasmBrowserNet10/` | Browser WASM: AOT host + dynamically loaded interpreted plugin |
| `BrowserPlugin/` | Dynamically loaded interpreted plugin for Browser WASM test |
| `BrowserPluginApi/` | Shared plugin interface (`IBrowserPlugin`) |

### Performance Summary (n=1,000,000, Windows x64)

| Benchmark | JIT | CoreCLR Interp M1 | Lua 5.4 | WASI | ILRuntime |
|-----------|-----|-------------------|---------|------|-----------|
| dict lookup | 13 ms | 53 ms | 26 ms | 189 ms | 537 ms |
| math loop | 2.4 ms | 34 ms | 14 ms | 35 ms | 76 ms |
| interface dispatch | 3 ms | 48 ms | 31 ms | 31 ms | — |
| object alloc | 7 ms | 114 ms | 149 ms | 28 ms | — |
| try/catch | 0.2 ms | 25 ms | 221 ms | 9 ms | — |

In Mode 1, BCL operations (Dictionary, List, etc.) run at native R2R speed.

### Browser WASM Performance (n=1,000,000, AOT vs Interpreted Plugin)

| Benchmark | AOT Host | Interpreted Plugin | Ratio |
|-----------|----------|-------------------|-------|
| dict lookup | 33 ms | 89 ms | 2.7x |
| math loop | 7 ms | 13 ms | 1.9x |
| interface dispatch | 21 ms | 50 ms | 2.4x |
| property get+set | 6 ms | 4 ms | 0.7x |
| object alloc+use | 15 ms | 14 ms | 1.0x |
| List\<int\> add | 9 ms | 26 ms | 2.9x |
| List\<int\> iterate | 14 ms | 21 ms | 1.5x |
| try/catch (no throw) | 1.3 ms | 14 ms | 10.5x |

Interpreted plugins only 2-3x slower for most operations. Object allocation shows zero overhead — same GC/allocator runs in AOT. Plugin DLL is pure IL (7 KB), downloaded via HTTP and loaded at runtime.

## Platform Matrix

| Platform | Runtime | Engine/BCL | Plugin | Loading |
|----------|---------|-----------|--------|---------|
| Windows/Linux | CoreCLR | JIT | JIT | `AssemblyLoadContext` |
| Android | CoreCLR | JIT | JIT | `AssemblyLoadContext` |
| iOS/tvOS | CoreCLR | R2R (AOT) | Interpreted | `AssemblyLoadContext` |
| Browser WASM | Mono | AOT→WASM | Interpreted | `Assembly.Load(byte[])` via HTTP |

Same C# code, same plugin DLL, same IL scanning — zero platform branching across all four targets.

## Prerequisites

- .NET 11 Preview SDK
- Visual Studio 2022/2026 with C++ workload (for NativeHost/LuaHost)
- CMake 3.20+

## Quick Start

```powershell
# Build and run the three-layer architecture demo
dotnet build NativeHost/managed/GameEngine/GameEngine.csproj -c Release
dotnet build NativeHost/managed/SamplePlugin/SamplePlugin.csproj -c Release

cd NativeHost/build
cmake .. -A x64
cmake --build . --config Release

# Deploy managed outputs
Copy-Item ../managed/GameEngine/bin/Release/net11.0/* Release/managed/ -Force
Copy-Item ../managed/SamplePlugin/bin/Release/net11.0/SamplePlugin.dll Release/plugins/

# Run
./Release/host.exe
```

## Testing Interpreter on Desktop

The CoreCLR interpreter is enabled in Release only for iOS/WASM. To test on Windows, build a custom CoreCLR from [dotnet/runtime](https://github.com/dotnet/runtime) with `FEATURE_INTERPRETER` force-enabled, then:

```powershell
$env:DOTNET_ROOT = "path/to/custom/runtime"
$env:DOTNET_InterpMode = "1"
./host.exe
```

See `NativeHost/dotnet_interp/` (gitignored) for the expected layout.

## License

ILRuntime third-party code retains its original license. Lua 5.4 source is under the MIT license. All other code in this repository is provided as-is for evaluation purposes.
