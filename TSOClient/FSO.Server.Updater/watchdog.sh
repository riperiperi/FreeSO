#!/bin/sh
mono watchdog.exe run --update
while [ $? -ne 2 ]; do
    mono watchdog.exe run 
done