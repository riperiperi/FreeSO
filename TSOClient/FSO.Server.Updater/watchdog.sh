#!/bin/sh

while [ $? -ne 2 ]; do
    mono watchdog.exe run --core
    dotnet exec FSO.Server.Core.dll run
done