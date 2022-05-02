#!/bin/bash
mcs *.cs -out:minipl.exe
mono minipl.exe
gcc -o program program.c