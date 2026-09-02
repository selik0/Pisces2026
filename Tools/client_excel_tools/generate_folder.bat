@echo off
setlocal

rem 单独转表入口：直接修改 CONFIG，使用对应大区的 config.json。
rem 该 config.json 中的 common_folders 配置本次要转换的文件夹。
set "CONFIG=%~dp0config.json"

if not exist "%CONFIG%" (
    echo Config file not found: "%CONFIG%"
    pause
    exit /b 1
)

set "GENERATOR=%~dp0generate.py"

where py >nul 2>nul
if %errorlevel% equ 0 (
    py -3 "%GENERATOR%" --config "%CONFIG%"
) else (
    where python >nul 2>nul
    if not %errorlevel% equ 0 (
        echo Python 3 was not found. Install Python 3 and open openpyxl first.
        echo python -m pip install openpyxl
        pause
        exit /b 1
    )
    python "%GENERATOR%" --config "%CONFIG%"
)

set "RESULT=%errorlevel%"
if "%RESULT%"=="0" echo Generation completed.
if not "%RESULT%"=="0" echo Generation failed with exit code %RESULT%.
pause
exit /b %RESULT%
