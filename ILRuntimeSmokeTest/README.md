# ILRuntime 桌面冒烟测试

在 **.NET 控制台**里加载一份预编译的 `IlrtHotDll.dll`，用 **ILRuntime** 解释执行其中的静态方法（不依赖 Unity）。

## 准备 ILRuntime 源码

官方未在 NuGet 发布主包，本示例通过 **ProjectReference** 引用源码工程。若尚无目录，请在 `ILRuntimeSmokeTest` 下执行：

```bash
git clone --depth 1 https://github.com/Ourpalm/ILRuntime.git third_party/ILRuntime
```

（仓库已带 `third_party/ILRuntime` 时可跳过。）

## 构建与运行

在仓库根目录：

```bash
dotnet build ILRuntimeSmokeTest/ILRuntimeSmokeTest.sln
dotnet run --project ILRuntimeSmokeTest/IlrtHost
```

期望输出类似：`[IlrtHotDll] hello, ILRuntime`

## 项目说明

| 项目 | 说明 |
|------|------|
| `IlrtHotDll` | `net10.0` 类库，模拟“热更 DLL” |
| `IlrtHost` | 引用 `third_party/ILRuntime/ILRuntime`，构建后将 `IlrtHotDll` 复制到输出目录 `hot/` |

## 你还可以测什么

1. **改 `HotEntry.Run` 的字符串**，只重新 `dotnet build IlrtHotDll`，再跑 `IlrtHost`（若未改宿主，可感受“只换 DLL”的流程）。  
2. **阅读官方 CLI**：`third_party/ILRuntime/ILRuntimeTestCLI` + `TestCases`（完整测试矩阵，需按该项目的用法传参）。  
3. **Unity**：仍以官方文档为准，需拷贝源码进 Assets、生成绑定等，与本示例无关。

## 注意

- ILRuntime 的命名空间为 **`ILRuntime.Runtime.Enviorment`**（`Enviorment` 为历史拼写）。  
- 更复杂的调用需按文档注册 **Delegate**、**跨域继承适配器** 等；本示例仅最小静态调用。
- **宿主工程**、**ILRuntime** 与 `third_party` 内相关工程均仅面向 `net10.0`（与 `IlrtHotDll` 一致）。  
- **不要用** 上游的 `LoadAssemblyFile` 直接测复杂程序集：它过早关闭 `FileStream`，Cecil 延迟读 IL 时会报错；本示例用 **`ReadAllBytes` + `MemoryStream` + `LoadAssembly`**。若自行打开 `USE_PDB` 编译 ILRuntime，可再尝试带符号加载。

## 热更 DLL 编写注意事项（.NET 8+ / ILRuntime 兼容性）

在面向 `net8.0` 及更高版本编译的程序集里，C# 编译器会将部分语法糖展开为新的运行时类型，而 ILRuntime 解释器尚未支持它们，**运行时会抛异常**。在热更 DLL 中需要规避以下写法：

### 1. 带格式说明符的字符串插值

```csharp
// 会生成 DefaultInterpolatedStringHandler（ref struct），ILRuntime 无法装箱
string s = $"value = {x:F2}";
```

改为 `.ToString()` + 拼接：

```csharp
string s = "value = " + x.ToString("F2");
```

不带格式说明符的简单插值（如 `$"hello, {name}"`）通常安全，编译器会将其优化为 `string.Concat`。

### 2. 4 个及以上参数的 `string.Format`

```csharp
// 编译器会用 InlineArray<object> 传参，ILRuntime 不支持
string s = string.Format("{0} {1} {2} {3}", a, b, c, d);
```

拆成多次拼接，或分两步 Format：

```csharp
string s = a.ToString() + " " + b.ToString() + " " + c.ToString() + " " + d.ToString();
```

### 3. ref struct 相关

任何直接使用 `Span<T>`、`ReadOnlySpan<T>`、`DefaultInterpolatedStringHandler` 等 **ref struct** 的代码，ILRuntime 都无法解释执行（`Cannot create boxed ByRef-like values`）。热更代码应避免显式或隐式依赖它们。

### 4. 通用建议

- 热更 DLL 中尽量使用**经典 API**（`string.Concat`、`StringBuilder`、`ToString` 等）。
- 遇到 `NotSupportedException` 或 `IndexOutOfRangeException`（涉及 `InlineArray`）时，检查编译器是否将语法糖展开为了上述新类型。
- 性能测试建议用 `dotnet run -c Release`，确保宿主和 ILRuntime 都以 Release 编译。
