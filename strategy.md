# CoreCLR 解释器热更新：优势、风险与 DLL 组织策略

## 核心优势

### 1. 零平台分支

同一份 C# 代码、同一个 DLL 文件在所有平台上运行。运行时自动选择执行方式：
- Windows/Android/macOS：JIT 全速执行
- iOS/tvOS：CoreCLR 解释器执行（BCL 通过 R2R 仍为原生速度）

业务代码、加载逻辑、安全检查全平台共享，无需 `#if` 条件编译。

### 2. 完整的 C# 语言支持

与 ILRuntime 不同，CoreCLR 解释器支持所有 C# 语言特性：
- `ref struct`、`Span<T>`、`stackalloc`
- `async/await`、`IAsyncEnumerable`
- 模式匹配、record 类型
- 泛型（包括值类型泛型）
- 字符串插值（`DefaultInterpolatedStringHandler`）

插件开发者无需学习任何限制或变通写法。

### 3. 类型共享与零开销互操作

解释代码和原生代码运行在同一个 GC 堆中，共享同一套类型系统。引擎层的 `Vector3`、`Entity`、`Component` 等类型可以直接在插件中使用，无需：
- 序列化/反序列化
- 绑定代码生成
- 手动类型注册
- Marshalling

### 4. Mode 1 的自动性能分层

`DOTNET_InterpMode=1` 自动区分 R2R 和非 R2R 代码：
- 包含 R2R 原生代码的 DLL → 原生速度执行
- 仅含 IL 的 DLL → 解释执行

BCL（`System.Collections`、`System.Linq` 等）自带 R2R，热更新插件中调用 `Dictionary<K,V>.TryGetValue()` 实际上以原生速度运行，只有插件自身的逻辑走解释器。

### 5. 统一的安全模型

IL 扫描在 DLL 加载前进行，与执行方式无关。JIT 平台和解释器平台使用完全相同的 `PluginScanner` 和 `ScanPolicy`。

---

## 风险

### 1. 成熟度风险（中等）

CoreCLR 解释器目标 .NET 11（2026 年 11 月）正式发布。目前可从源码编译（[dotnet/runtime](https://github.com/dotnet/runtime)）构建出 CoreCLR 解释器，在 iOS 构建中已无条件启用 Release，但功能仍在积极开发中。 SDK 下载地址：[.NET 11 Preview SDK](https://dotnet.microsoft.com/en-us/download/dotnet/11/sdk)

**缓解措施：**
- 架构设计（AssemblyLoadContext + IL 扫描）不依赖解释器，在 JIT 平台上同样成立
- ILRuntime 作为 iOS 的后备方案，已在本项目中验证可用
- 持续关注 `dotnet/runtime` 仓库的解释器相关 PR 和 milestone

### 2. 解释器性能（低至中等）

纯计算密集型代码在解释模式下比 JIT 慢 4-20 倍。但 Mode 1 下 BCL 操作以原生速度运行，实际影响取决于插件代码特征。

**缓解措施：**
- 性能敏感的逻辑放在引擎 C# 层（R2R 编译），通过 API 暴露给插件
- 避免在插件中写大循环数学运算，改用引擎提供的批量 API
- 将频繁调用的工具函数封装在 R2R 库中

### 3. IL 裁剪与泛型（低）

`PublishTrimmed` 可能删除引擎层中插件需要的泛型方法 IL 元数据。

**缓解措施：**
- 引擎层和 PluginApi 使用 `<TrimmerRootAssembly>` 保留完整，只裁剪 BCL
- 详见 trimming.md

### 4. 安全边界（低至中等）

IL 扫描是静态分析，不是进程级隔离。对于可信的内部开发者编写的热更新代码完全够用，但无法阻止刻意绕过的攻击者。

**缓解措施：**
- 内部热更新代码：IL 扫描足够，甚至可以跳过
- 第三方用户插件：IL 扫描 + 代码审查，或在独立进程中加载（牺牲性能）

### 5. iOS 包体大小（低）

Self-contained 发布需要打包 CoreCLR 运行时，估计 15-25 MB（裁剪后）。

**缓解措施：**
- `PublishTrimmed` 移除未使用的 BCL 模块
- 对比：Unity IL2CPP 构建通常也在 20-50 MB

---

## DLL 组织策略

### Mode 1 的核心规则

CoreCLR 解释器 Mode 1 的行为完全由 DLL 自身决定：
- DLL 包含 R2R 原生代码 → 走原生执行
- DLL 仅包含 IL → 走解释器（iOS）或 JIT（其他平台）

**不需要任何文件夹约定、配置文件或代码标记来告诉运行时如何执行。** 编译方式（`dotnet build` vs `dotnet publish -p:PublishReadyToRun=true`）决定了一切。

### 推荐方案：按角色划分文件夹 + R2R 标记

```
MyGame.app/
  engine                          ← C++ 宿主
  runtime/                        ← .NET 运行时（self-contained 自动生成）
    coreclr.dll / libcoreclr.dylib
    System.Private.CoreLib.dll
    System.Runtime.dll             ← BCL，自带 R2R
    System.Collections.dll
    ...
  managed/                        ← 引擎 C# 层（R2R 编译）
    GameEngine.dll                 ← PublishReadyToRun=true
    PluginApi.dll                  ← PublishReadyToRun=true
    IlScanner.dll
    Mono.Cecil.dll
    GameEngine.runtimeconfig.json
    GameEngine.deps.json
  plugins/                        ← 热更新插件（纯 IL，可替换）
    MyPlugin.dll                   ← dotnet build 产物，纯 IL
    CommunityPlugin.dll
```

文件夹划分的意义**不是**告诉运行时如何执行（那由 R2R 标记自动决定），而是：

| 目的 | managed/ | plugins/ |
|------|----------|----------|
| 更新频率 | 随 app 版本更新 | 可随时下载/替换 |
| 信任级别 | 完全信任，不扫描 | 不信任，加载前 IL 扫描 |
| 编译方式 | `dotnet publish -r <rid> -p:PublishReadyToRun=true` | `dotnet build` |
| iOS 上的行为 | 原生速度（R2R） | 解释执行 |
| 其他平台行为 | 原生速度（R2R） | 原生速度（JIT） |

### 替代方案：不划分文件夹

也可以把所有 DLL 放在同一目录，纯靠 R2R 标记区分：

```
MyGame.app/
  engine
  managed/
    GameEngine.dll                 ← R2R
    PluginApi.dll                  ← R2R
    MyPlugin.dll                   ← 纯 IL
    CommunityPlugin.dll            ← 纯 IL
    ...
```

**优点：** 部署简单，不需要多路径管理。

**缺点：**
- 无法通过文件系统权限区分引擎和插件
- 热更新替换文件时可能误覆盖引擎 DLL
- IL 扫描逻辑需要自行判断哪些是插件（通过命名约定或配置文件）

### 不推荐：动态切换 R2R

不要试图为同一个 DLL 准备 R2R 和纯 IL 两个版本。R2R DLL 在非 R2R 平台上照样可以 JIT（R2R 只是额外携带预编译代码，IL 仍然完整保留），所以一个 R2R DLL 在所有平台都能工作。问题是你不希望热更新插件被 R2R 编译——那样就失去了"随时替换"的灵活性（R2R 是平台相关的）。

### 决策流程图

```
这个 DLL 需要频繁更新 / 用户提供？
  ├─ 是 → 纯 IL（dotnet build），放 plugins/，加载前 IL 扫描
  └─ 否 → 这个 DLL 对性能敏感？
           ├─ 是 → R2R 编译，放 managed/
           └─ 否 → 都行，放 managed/，R2R 可选
```

---

## 总结

| 方面 | 结论 |
|------|------|
| 跨平台统一性 | 同一份代码、同一份 IL DLL、同一套工具链，执行方式由运行时自动选择 |
| 性能策略 | 性能敏感逻辑放引擎层（R2R），插件通过 API 调用而非自行实现 |
| 安全策略 | 引擎层完全信任，插件加载前 IL 扫描 |
| DLL 组织 | 按角色（引擎/插件）分目录，R2R 标记自动决定执行方式 |
| 裁剪策略 | 只裁剪 BCL，保留引擎层和 PluginApi |
