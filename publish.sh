#! /bin/bash

runWin=1
runOSX=1
runLinux=1
runZip=1

if [[ " $@ " =~ " nozip " ]]; then
   runZip=0
fi

if [[ " $@ " =~ " win " ]]; then
   runOSX=0
   runLinux=0
   runZip=0
fi

if [[ " $@ " =~ " osx " ]]; then
   runWin=0
   runLinux=0
   runZip=0
fi

if [[ " $@ " =~ " linux " ]]; then
   runWin=0
   runOSX=0
   runZip=0
fi

echo runWin: $runWin
echo runOSX: $runOSX
echo runLinux: $runLinux
echo runZip: $runZip

set -x #echo on

mkdir -p ./publish/win-x64
rm -f ./publish/win-x64/*

mkdir -p ./publish/osx-x64
rm -f ./publish/osx-x64/*

mkdir -p ./publish/linux-x64
rm -f ./publish/linux-x64/*

rm -f ./publish/*.zip

cd MaxRunSoftware.Utilities.Console

if [[ $runWin = 1 ]]; then
dotnet publish -o ../publish/win-x64   -r win-x64   -p:PublishSingleFile=true --self-contained true --nologo -p:PublishReadyToRunShowWarnings=true -p:PublishReadyToRun=false -p:IncludeAllContentForSelfExtract=true -p:DebugType=embedded
mv ../publish/win-x64/MaxRunSoftware.Utilities.Console.exe ../publish/win-x64/huc.exe
fi

if [[ $runOSX = 1 ]]; then
dotnet publish -o ../publish/osx-x64   -r osx-x64   -p:PublishSingleFile=true --self-contained true --nologo -p:PublishReadyToRunShowWarnings=true -p:PublishReadyToRun=false -p:IncludeAllContentForSelfExtract=true -p:DebugType=embedded
mv ../publish/osx-x64/MaxRunSoftware.Utilities.Console ../publish/osx-x64/huc
fi

if [[ $runLinux = 1 ]]; then
dotnet publish -o ../publish/linux-x64 -r linux-x64 -p:PublishSingleFile=true --self-contained true --nologo -p:PublishReadyToRunShowWarnings=true -p:PublishReadyToRun=false -p:IncludeAllContentForSelfExtract=true -p:DebugType=embedded
mv ../publish/linux-x64/MaxRunSoftware.Utilities.Console ../publish/linux-x64/huc
fi

cd ..

if [[ $runZip = 1 ]]; then
zip -9 -j ./publish/huc-win.zip ./publish/win-x64/huc.exe
zip -9 -j ./publish/huc-osx.zip ./publish/osx-x64/huc
zip -9 -j ./publish/huc-linux.zip ./publish/linux-x64/huc
zip -9 -j ./publish/libs.zip ./MaxRunSoftware.Utilities.External/bin/Debug/net5.0/*.* -x "ref" 
fi
