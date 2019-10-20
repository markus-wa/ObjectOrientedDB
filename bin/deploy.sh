#!/bin/sh

set -e

Version=$1
Source=$2
ApiKey=$3

cd ObjectOrientedDB
curl -L -o nuget.exe https://dist.nuget.org/win-x86-commandline/latest/nuget.exe
export NUGET='mono nuget.exe'
$NUGET pack ObjectOrientedDB.nuspec -Version $Version
$NUGET push ObjectOrientedDB.$Version.nupkg -ApiKey $ApiKey -Source $Source
