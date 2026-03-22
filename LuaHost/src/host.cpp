#include <cstdio>
#include <cstring>
#include <string>

#define NOMINMAX
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

extern "C" {
#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"
}

// --- Native engine APIs exposed to Lua ---

static int frame_counter = 0;

static int l_engine_log(lua_State* L)
{
    const char* msg = luaL_checkstring(L, 1);
    printf("  [C++ engine_log] %s\n", msg);
    return 0;
}

static int l_engine_get_frame(lua_State* L)
{
    lua_pushinteger(L, frame_counter++);
    return 1;
}

// High-precision timer for Lua
static int l_clock_ms(lua_State* L)
{
    LARGE_INTEGER freq, now;
    QueryPerformanceFrequency(&freq);
    QueryPerformanceCounter(&now);
    double ms = (double)now.QuadPart / (double)freq.QuadPart * 1000.0;
    lua_pushnumber(L, ms);
    return 1;
}

// Nop function for cross-call benchmark
static int l_engine_nop(lua_State* L)
{
    int v = (int)luaL_checkinteger(L, 1);
    lua_pushinteger(L, v);
    return 1;
}

static std::string get_exe_dir()
{
    char buf[MAX_PATH];
    GetModuleFileNameA(nullptr, buf, MAX_PATH);
    std::string path(buf);
    auto pos = path.find_last_of("\\/");
    return path.substr(0, pos);
}

int main()
{
    printf("=== C++ Lua Host ===\n\n");

    lua_State* L = luaL_newstate();
    luaL_openlibs(L);

    // Register native functions
    lua_register(L, "engine_log", l_engine_log);
    lua_register(L, "engine_get_frame", l_engine_get_frame);
    lua_register(L, "clock_ms", l_clock_ms);
    lua_register(L, "engine_nop", l_engine_nop);

    // Load and run the benchmark script
    std::string script = get_exe_dir() + "\\scripts\\benchmark.lua";
    int err = luaL_dofile(L, script.c_str());
    if (err)
    {
        printf("[host] ERROR: %s\n", lua_tostring(L, -1));
        lua_pop(L, 1);
    }

    lua_close(L);
    printf("\n[host] Done.\n");
    return 0;
}
