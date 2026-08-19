@echo off
chcp 65001 >nul
call "%~dp0..\build.cmd" Release %*
