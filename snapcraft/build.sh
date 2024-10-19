#!/usr/bin/env bash
rm -rf build
cd ../src/cli
dotnet publish -r linux-x64 --self-contained true -c Release 
cp -r bin/Release/net8.0/linux-x64/publish/ ../../snapcraft/build
cd -
snapcraft
