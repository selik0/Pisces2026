@echo off
setlocal

rem 通用入口：直接修改 CONFIG，使用对应大区的 config.json。
rem config.json 中的 common_folders 配置本次批量生成的 Excel 文件夹。
set "GENERATOR=%~dp0generate.py"
set "CONFIG=%~dp0config.json"

where py >nul 2>nul
if %errorlevel% equ 0 (
    py -3 "%GENERATOR%" --config "%CONFIG%" %*
) else (
    where python >nul 2>nul
    if not %errorlevel% equ 0 (
        echo Python 3 was not found. Install Python 3 and openpyxl first.
        echo python -m pip install openpyxl
        pause
        exit /b 1
    )
    python "%GENERATOR%" --config "%CONFIG%" %*
)

set "RESULT=%errorlevel%"
if not "%RESULT%"=="0" echo Generation failed with exit code %RESULT%.
if "%RESULT%"=="0" echo Generation completed.
pause
exit /b %RESULT%
