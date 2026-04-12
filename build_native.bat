@echo off
cd /d C:\dev\Janet-CSharp\native
cmake -B build
cmake --build build --config Release
