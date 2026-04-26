@echo off
SETLOCAL EnableDelayedExpansion

:: ── Configuration ────────────────────────────────────────────────────────────
SET "GAME_NAME=FreeSO"
SET "SERVER_URL=https://fso.icarey.net"
SET "CLIENT_URL=https://github.com/TheGreatCodeholio/FreeSO/releases/latest/download/freeso-client-windows-ogl.zip"
SET "TSO_URL=https://archive.org/download/TheSimsOnline_201802/TSO.zip"
SET "REMESH_URL=https://github.com/ItsSim/fsolauncher/releases/download/1.12.1-prod.24/remeshes-1.0.0-1726774408.zip"
SET "DEFAULT_INSTALL=%LOCALAPPDATA%\%GAME_NAME%"
:: ─────────────────────────────────────────────────────────────────────────────

TITLE %GAME_NAME% Installer

ECHO.
ECHO  ================================================
ECHO   %GAME_NAME% Installer for Windows
ECHO  ================================================
ECHO.

:: ── Install directory ────────────────────────────────────────────────────────
SET "INSTALL_DIR="
SET /P "INSTALL_DIR=Install location [%DEFAULT_INSTALL%]: "
IF "%INSTALL_DIR%"=="" SET "INSTALL_DIR=%DEFAULT_INSTALL%"

ECHO.
ECHO Installing to: %INSTALL_DIR%
ECHO.

:: ── Optional components ──────────────────────────────────────────────────────
SET /P "INSTALL_TSO=Download and install The Sims Online game files? [y/n]: "
SET /P "INSTALL_REMESH=Install 3D remesh package? [y/n]: "

ECHO.
ECHO Downloading and installing — please do not close this window.
ECHO.

:: ── Create install directory ─────────────────────────────────────────────────
IF NOT EXIST "%INSTALL_DIR%" MKDIR "%INSTALL_DIR%"

:: ── Download and extract client ──────────────────────────────────────────────
IF NOT EXIST "%TEMP%\freeso-client.zip" (
    ECHO [1/3] Downloading %GAME_NAME% client...
    powershell -Command "Invoke-WebRequest '%CLIENT_URL%' -OutFile '%TEMP%\freeso-client.zip.incomplete'" ^
        && REN "%TEMP%\freeso-client.zip.incomplete" "freeso-client.zip"
    IF ERRORLEVEL 1 GOTO fail
)

ECHO [1/3] Extracting %GAME_NAME% client...
powershell -Command "Expand-Archive '%TEMP%\freeso-client.zip' -DestinationPath '%INSTALL_DIR%' -Force"
IF ERRORLEVEL 1 GOTO fail

:: ── Download and extract TSO ─────────────────────────────────────────────────
IF /I "%INSTALL_TSO%"=="y" (
    IF NOT EXIST "%TEMP%\TSO.zip" (
        ECHO [2/3] Downloading TSO game files...
        powershell -Command "Invoke-WebRequest '%TSO_URL%' -OutFile '%TEMP%\TSO.zip.incomplete'" ^
            && REN "%TEMP%\TSO.zip.incomplete" "TSO.zip"
        IF ERRORLEVEL 1 GOTO fail
    )

    ECHO [2/3] Extracting TSO game files...
    IF NOT EXIST "%TEMP%\tso-extract" MKDIR "%TEMP%\tso-extract"
    powershell -Command "Expand-Archive '%TEMP%\TSO.zip' -DestinationPath '%TEMP%\tso-extract' -Force"
    IF ERRORLEVEL 1 GOTO fail

    ECHO [2/3] Installing TSO content...
    IF NOT EXIST "%INSTALL_DIR%\game" MKDIR "%INSTALL_DIR%\game"
    extrac32.exe /Y /A /E "%TEMP%\tso-extract\Data1.cab" /L "%INSTALL_DIR%\game"
    IF ERRORLEVEL 1 GOTO fail
) ELSE (
    ECHO [2/3] Skipping TSO game files.
)

:: ── Download and extract remesh package ─────────────────────────────────────
IF /I "%INSTALL_REMESH%"=="y" (
    IF NOT EXIST "%TEMP%\remeshes.zip" (
        ECHO [3/3] Downloading 3D remesh package...
        powershell -Command "Invoke-WebRequest '%REMESH_URL%' -OutFile '%TEMP%\remeshes.zip.incomplete'" ^
            && REN "%TEMP%\remeshes.zip.incomplete" "remeshes.zip"
        IF ERRORLEVEL 1 GOTO fail
    )

    ECHO [3/3] Extracting 3D remesh package...
    IF NOT EXIST "%INSTALL_DIR%\Content\MeshReplace" MKDIR "%INSTALL_DIR%\Content\MeshReplace"
    powershell -Command "Expand-Archive '%TEMP%\remeshes.zip' -DestinationPath '%INSTALL_DIR%\Content\MeshReplace' -Force"
    IF ERRORLEVEL 1 GOTO fail
) ELSE (
    ECHO [3/3] Skipping remesh package.
)

:: ── Write config.ini ─────────────────────────────────────────────────────────
ECHO Configuring client...
(
    ECHO # FreeSO Settings File
    ECHO CurrentLang=english
    ECHO StartupPath=game\TSOClient\
    ECHO UseCustomServer=True
    ECHO GameEntryUrl=%SERVER_URL%
    ECHO CitySelectorUrl=%SERVER_URL%
) > "%INSTALL_DIR%\Content\config.ini"

:: ── Desktop shortcuts ────────────────────────────────────────────────────────
ECHO Creating desktop shortcuts...

SET "PWS=powershell.exe -ExecutionPolicy Bypass -NoLogo -NonInteractive -NoProfile"
SET "EXE=%INSTALL_DIR%\FreeSO.exe"

FOR /F "usebackq tokens=2,*" %%A IN (
    `reg query "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders" /v Desktop 2^>nul`
) DO SET "DESKTOP=%%B"

%PWS% -Command ^
    "$ws = New-Object -ComObject WScript.Shell; ^
     $s = $ws.CreateShortcut('%DESKTOP%\%GAME_NAME%.lnk'); ^
     $s.TargetPath = '%EXE%'; ^
     $s.WorkingDirectory = '%INSTALL_DIR%'; ^
     $s.Save()"

%PWS% -Command ^
    "$ws = New-Object -ComObject WScript.Shell; ^
     $s = $ws.CreateShortcut('%DESKTOP%\%GAME_NAME% (3D).lnk'); ^
     $s.TargetPath = '%EXE%'; ^
     $s.Arguments = '-3d'; ^
     $s.WorkingDirectory = '%INSTALL_DIR%'; ^
     $s.Save()"

:: ── Done ─────────────────────────────────────────────────────────────────────
ECHO.
ECHO  ================================================
ECHO   Installation complete!
ECHO   Launch via the desktop shortcut or run:
ECHO   "%INSTALL_DIR%\FreeSO.exe"
ECHO  ================================================
ECHO.
PAUSE
EXIT /B 0

:fail
ECHO.
ECHO  ================================================
ECHO   An error occurred during installation.
ECHO   Try running the installer again.
ECHO  ================================================
ECHO.
PAUSE
EXIT /B 1