dotnet build -c Release HotfixAOT/
dotnet build -c Release_Patched HotfixAOT/
dotnet build -c Release PatchTool/
cd PatchTool/bin/Release/net10.0
PatchTool -i -h ../../../../HotfixAOT/Patched/HotfixAOT.hash -o ../../../../HotfixAOT/Patched/HotfixAOT.dll ../../../../HotfixAOT/bin/Release/net10.0/HotfixAOT.dll
PatchTool -p -o ../../../../HotfixAOT/Patched/HotfixAOT.patch ../../../../HotfixAOT/Patched/HotfixAOT.hash ../../../../HotfixAOT/bin/Release_Patched/net10.0/HotfixAOT.dll
