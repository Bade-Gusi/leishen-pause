@echo off
echo ========== 正在发布 PUBG助手 ==========
echo.

dotnet publish leishen/leishen.csproj -c Release -r win-x86 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true

echo.
echo ========== 发布完成！==========
echo 输出目录: leishen/bin/Release/net8.0-windows/win-x86/publish/
pause
