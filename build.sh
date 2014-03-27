#!/bin/bash
if [ ! -f tools/FAKE/tools/FAKE.exe ]; then
  mono .nuget/NuGet.exe install FAKE -OutputDirectory tools -ExcludeVersion -Prerelease
fi
mono tools/FAKE/tools/FAKE.exe build.fsx $@
