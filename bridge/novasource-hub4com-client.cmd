@echo off
setlocal

if "%~2"=="" (
    echo Usage: %~nx0 SERVER_ADDRESS TCP_PORT
    echo Local hub4com endpoint defaults to COM51.
    exit /b 2
)

call "C:\Program Files (x86)\hub4com\com2tcp-rfc2217.bat" \\.\COM51 "%~1" "%~2"
exit /b %ERRORLEVEL%
