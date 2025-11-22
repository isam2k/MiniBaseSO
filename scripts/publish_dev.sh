#!/bin/bash

mkdir -p "${MOD_FOLDER}"
cp -p MiniBase/bin/Release/MiniBase-merged.dll "${MOD_FOLDER}"
cp -p MiniBase/bin/Release/mod.yaml "${MOD_FOLDER}"
cp -p MiniBase/bin/Release/mod_info.yaml "${MOD_FOLDER}"
cp -r -p MiniBase/ModAssets/dlc "${MOD_FOLDER}"
cp -r -p MiniBase/ModAssets/worldgen "${MOD_FOLDER}"

echo "Successfully updated mod files"
date -r "MiniBase/bin/Release/MiniBase-merged.dll" "+%H:%M:%S %d.%m.%Y"
