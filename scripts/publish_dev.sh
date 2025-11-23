#!/bin/bash

set -e

echo "Build project..."
dotnet build MiniBase/MiniBase.csproj -c Release -o bin

echo "Cleanup mod folder..."
rm -rf "${MOD_FOLDER}"
mkdir -p "${MOD_FOLDER}"

echo "Copy mod files..."
cp -p bin/MiniBase-merged.dll "${MOD_FOLDER}"
cp -p bin/mod.yaml "${MOD_FOLDER}"
cp -p bin/mod_info.yaml "${MOD_FOLDER}"
cp -r -p MiniBase/ModAssets/dlc "${MOD_FOLDER}"
cp -r -p MiniBase/ModAssets/worldgen "${MOD_FOLDER}"
cp -r -p anim "${MOD_FOLDER}"

echo "Successfully updated mod files"
