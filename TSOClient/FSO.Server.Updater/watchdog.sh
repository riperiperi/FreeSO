#!/bin/sh
mono watchdog.exe run
while [ $? -ne 2 ]; do
    mono watchdog.exe run
done