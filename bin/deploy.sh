#!/bin/sh

Version=$1
Source=$2
ApiKey=$3

curl -L -o nuget.exe https://dist.nuget.org/win-x86-commandline/latest/nuget.exe
export NUGET='mono nuget.exe'
$NUGET pack ./ObjectOrientedDB/ObjectOrientedDB.nuspec -Version $Version
$NUGET push ObjectOrientedDB/bin/Release/ObjectOrientedDB.*.nupkg -k $ApiKey -s $Source
