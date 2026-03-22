cmake_minimum_required(VERSION 3.20)
project(NativeHost LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)

set(DOTNET_HOST_DIR "C:/Program Files/dotnet/packs/Microsoft.NETCore.App.Host.win-x64/11.0.0-preview.2.26159.112/runtimes/win-x64/native")

set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")

add_executable(host src/host.cpp)

target_include_directories(host PRIVATE "${DOTNET_HOST_DIR}")

target_link_libraries(host PRIVATE "${DOTNET_HOST_DIR}/libnethost.lib")

target_compile_definitions(host PRIVATE
    NETHOST_USE_AS_STATIC
    _CRT_SECURE_NO_WARNINGS
    UNICODE
    _UNICODE
)
