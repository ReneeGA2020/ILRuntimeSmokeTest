#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <string>

#define NOMINMAX
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include <nethost.h>
#include <hostfxr.h>
#include <coreclr_delegates.h>

static hostfxr_initialize_for_runtime_config_fn  hostfxr_init         = nullptr;
static hostfxr_get_runtime_delegate_fn           hostfxr_get_delegate = nullptr;
static hostfxr_close_fn                          hostfxr_close_ctx    = nullptr;

// --- Engine callbacks exposed to C# ---
extern "C" void __stdcall engine_log(const wchar_t* message)
{
    wprintf(L"  [engine] %s\n", message);
}

extern "C" int __stdcall engine_get_frame(void)
{
    static int frame = 0;
    return frame++;
}

static bool load_hostfxr()
{
    wchar_t path[MAX_PATH];
    size_t  size = sizeof(path) / sizeof(wchar_t);
    if (get_hostfxr_path(path, &size, nullptr) != 0) return false;

    HMODULE lib = LoadLibraryW(path);
    if (!lib) return false;

    hostfxr_init         = (hostfxr_initialize_for_runtime_config_fn)GetProcAddress(lib, "hostfxr_initialize_for_runtime_config");
    hostfxr_get_delegate = (hostfxr_get_runtime_delegate_fn)GetProcAddress(lib, "hostfxr_get_runtime_delegate");
    hostfxr_close_ctx    = (hostfxr_close_fn)GetProcAddress(lib, "hostfxr_close");

    return hostfxr_init && hostfxr_get_delegate && hostfxr_close_ctx;
}

static std::wstring get_exe_dir()
{
    wchar_t buf[MAX_PATH];
    GetModuleFileNameW(nullptr, buf, MAX_PATH);
    std::wstring path(buf);
    return path.substr(0, path.find_last_of(L"\\/"));
}

int wmain(int argc, wchar_t* argv[])
{
    wprintf(L"=== C++ Engine Host (Three-Layer Architecture) ===\n\n");

    if (!load_hostfxr())
    {
        wprintf(L"ERROR: Could not load hostfxr\n");
        return 1;
    }

    std::wstring exe_dir = get_exe_dir();
    std::wstring config  = exe_dir + L"\\managed\\GameEngine.runtimeconfig.json";

    hostfxr_handle ctx = nullptr;
    int rc = hostfxr_init(config.c_str(), nullptr, &ctx);
    if (rc != 0 || !ctx)
    {
        wprintf(L"ERROR: hostfxr_initialize failed: 0x%08x\n", rc);
        if (ctx) hostfxr_close_ctx(ctx);
        return 1;
    }

    load_assembly_and_get_function_pointer_fn load_asm = nullptr;
    rc = hostfxr_get_delegate(ctx, hdt_load_assembly_and_get_function_pointer, (void**)&load_asm);
    if (rc != 0 || !load_asm)
    {
        wprintf(L"ERROR: get_runtime_delegate failed: 0x%08x\n", rc);
        hostfxr_close_ctx(ctx);
        return 1;
    }

    std::wstring engine_asm = exe_dir + L"\\managed\\GameEngine.dll";

    // Resolve managed entry points
    typedef void (CORECLR_DELEGATE_CALLTYPE *init_fn)(void*, void*);
    typedef void (CORECLR_DELEGATE_CALLTYPE *load_plugin_fn)(const wchar_t*);
    typedef void (CORECLR_DELEGATE_CALLTYPE *tick_fn)();
    typedef void (CORECLR_DELEGATE_CALLTYPE *shutdown_fn)();

    init_fn        engine_init     = nullptr;
    load_plugin_fn engine_load     = nullptr;
    tick_fn        engine_tick     = nullptr;
    shutdown_fn    engine_shutdown = nullptr;

    auto load_fn = [&](const wchar_t* method, void** out) -> bool {
        rc = load_asm(engine_asm.c_str(), L"GameEngine.EngineHost, GameEngine",
                      method, UNMANAGEDCALLERSONLY_METHOD, nullptr, out);
        if (rc != 0) wprintf(L"ERROR: Failed to load %s: 0x%08x\n", method, rc);
        return rc == 0;
    };

    if (!load_fn(L"Init",       (void**)&engine_init)     ||
        !load_fn(L"LoadPlugin", (void**)&engine_load)     ||
        !load_fn(L"Tick",       (void**)&engine_tick)     ||
        !load_fn(L"Shutdown",   (void**)&engine_shutdown))
    {
        hostfxr_close_ctx(ctx);
        return 1;
    }

    wprintf(L"[host] Runtime and engine loaded\n\n");

    // --- Init: pass native callbacks to C# engine ---
    wprintf(L"--- Init ---\n");
    engine_init((void*)engine_log, (void*)engine_get_frame);

    // --- Load plugin(s) ---
    wprintf(L"\n--- Load Plugins ---\n");
    std::wstring plugin_path = exe_dir + L"\\plugins\\SamplePlugin.dll";

    if (argc > 1)
        plugin_path = argv[1];

    engine_load(plugin_path.c_str());

    // --- Game loop ---
    wprintf(L"\n--- Game Loop (5 ticks) ---\n");
    for (int i = 0; i < 5; i++)
        engine_tick();

    // --- Shutdown ---
    wprintf(L"\n--- Shutdown ---\n");
    engine_shutdown();

    hostfxr_close_ctx(ctx);
    wprintf(L"\n[host] Done.\n");
    return 0;
}
