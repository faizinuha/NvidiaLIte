@echo off
set "SIGNTOOL=%~dp0bin\osslsigncode.exe"
set "CERT=%~dp0ZeroMixCert.pfx"
set "PASS=ZeroMixPass"
set "TS=http://timestamp.digicert.com"

echo [%DATE% %TIME%] Signing: %1 >> "%~dp0sign_log.txt"
"%SIGNTOOL%" sign -pkcs12 "%CERT%" -pass %PASS% -ts %TS% -in %1 -out %1.signed >> "%~dp0sign_log.txt" 2>&1
if %ERRORLEVEL% equ 0 (
    move /Y %1.signed %1 >> "%~dp0sign_log.txt" 2>&1
    echo Successfully signed and moved. >> "%~dp0sign_log.txt"
) else (
    echo Failed to sign %1 with exit code %ERRORLEVEL% >> "%~dp0sign_log.txt"
    exit /b %ERRORLEVEL%
)
