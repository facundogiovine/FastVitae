@echo off
rem Build the portable exe
dotnet publish "%~dp0src\FastVitae\FastVitae.csproj" -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  -p:SatelliteResourceLanguages=en ^
  -p:DebugType=none ^
  -p:PublishDocumentationFiles=false ^
  -o "%~dp0publish"
