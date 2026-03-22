cmake_minimum_required(VERSION 3.20)
project(LuaHost LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 17)

# Lua 5.4 as a static library (compiled from source)
file(GLOB LUA_SOURCES lua_src/*.c)
list(REMOVE_ITEM LUA_SOURCES
    ${CMAKE_CURRENT_SOURCE_DIR}/lua_src/lua.c
    ${CMAKE_CURRENT_SOURCE_DIR}/lua_src/luac.c
    ${CMAKE_CURRENT_SOURCE_DIR}/lua_src/onelua.c)

add_library(lua54 STATIC ${LUA_SOURCES})
target_include_directories(lua54 PUBLIC lua_src)
target_compile_definitions(lua54 PRIVATE _CRT_SECURE_NO_WARNINGS)

# Host executable
add_executable(luahost src/host.cpp)
target_link_libraries(luahost PRIVATE lua54)
target_include_directories(luahost PRIVATE lua_src)
target_compile_definitions(luahost PRIVATE _CRT_SECURE_NO_WARNINGS)

set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
