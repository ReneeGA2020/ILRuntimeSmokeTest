# CoreCLR Interpreter Hot-Update Smoke Test

Evaluates multiple C# hot-update approaches for a game engine architecture (C++ host + C# scripting), with a focus on iOS compatibility where JIT is blocked by W^X policy.

## Conclusion

CoreCLR interpreter (coming in .NET 11) is the winning approach: same C# code, same DLL, same toolchain across all platforms. The runtime automatically selects JIT (Windows/Android) or interpreter (iOS). No second language, no third-party interpreter, no platform branching in user code.

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

### Performance Summary (n=1,000,000, Windows x64)

| Benchmark | JIT | CoreCLR Interp M1 | Lua 5.4 | WASI | ILRuntime |
|-----------|-----|-------------------|---------|------|-----------|
| dict lookup | 13 ms | 53 ms | 26 ms | 189 ms | 537 ms |
| math loop | 2.4 ms | 34 ms | 14 ms | 35 ms | 76 ms |
| interface dispatch | 3 ms | 48 ms | 31 ms | 31 ms | — |
| object alloc | 7 ms | 114 ms | 149 ms | 28 ms | — |
| try/catch | 0.2 ms | 25 ms | 221 ms | 9 ms | — |

In Mode 1, BCL operations (Dictionary, List, etc.) run at native R2R speed.

## Platform Matrix

| Platform | Execution | Hot-Update | Interpreter Needed? |
|----------|-----------|------------|-------------------|
| Windows/Linux | JIT | `AssemblyLoadContext` → JIT | No |
| Android | JIT | `AssemblyLoadContext` → JIT | No |
| iOS/tvOS | CoreCLR interpreter | `AssemblyLoadContext` → interpreted | Yes (automatic) |

Same code, same DLL, same IL scanning — zero platform branching.

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
