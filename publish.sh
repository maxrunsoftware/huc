#! /bin/bash
set -x #echo on

mkdir -p ./publish/win-x64
rm -f ./publish/win-x64/*

mkdir -p ./publish/osx-x64
rm -f ./publish/osx-x64/*

mkdir -p ./publish/linux-x64
rm -f ./publish/linux-x64/*

rm -f ./publish/*.zip

cd HavokMultimedia.Utilities.Console

dotnet publish -o ../publish/win-x64   -r win-x64   -p:PublishSingleFile=true --self-contained true --nologo -p:PublishReadyToRunShowWarnings=true -p:PublishReadyToRun=false -p:IncludeAllContentForSelfExtract=true -p:DebugType=embedded

dotnet publish -o ../publish/osx-x64   -r osx-x64   -p:PublishSingleFile=true --self-contained true --nologo -p:PublishReadyToRunShowWarnings=true -p:PublishReadyToRun=false -p:IncludeAllContentForSelfExtract=true -p:DebugType=embedded

dotnet publish -o ../publish/linux-x64 -r linux-x64 -p:PublishSingleFile=true --self-contained true --nologo -p:PublishReadyToRunShowWarnings=true -p:PublishReadyToRun=false -p:IncludeAllContentForSelfExtract=true -p:DebugType=embedded

cd ..

mv ./publish/win-x64/HavokMultimedia.Utilities.Console.exe ./publish/win-x64/huc.exe
zip -9 -j ./publish/huc-win.zip ./publish/win-x64/huc.exe

mv ./publish/osx-x64/HavokMultimedia.Utilities.Console ./publish/osx-x64/huc
zip -9 -j ./publish/huc-osx.zip ./publish/osx-x64/huc

mv ./publish/linux-x64/HavokMultimedia.Utilities.Console ./publish/linux-x64/huc
zip -9 -j ./publish/huc-linux.zip ./publish/linux-x64/huc


