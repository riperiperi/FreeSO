@echo off
SETLOCAL EnableDelayedExpansion

:: ── Configuration ────────────────────────────────────────────────────────────
:: Env-var overrides for devs / community hosts. Set FREESO_SERVER_URL etc.
:: before running to point at a different server / client / TSO source.
:: Use the explicit `latest-client` release tag (not `releases/latest/...`)
:: since GitHub's "latest" is whichever release was published most recently
:: and resolves to the wrong one when both latest-client and latest-server
:: tags exist.
IF NOT DEFINED GAME_NAME          SET "GAME_NAME=EdenSO"
IF NOT DEFINED FREESO_SERVER_URL  SET "FREESO_SERVER_URL=https://api.edenso.net"
IF NOT DEFINED FREESO_CLIENT_URL  SET "FREESO_CLIENT_URL=https://github.com/TheGreatCodeholio/FreeSO/releases/download/latest-client/freeso-client-windows-ogl.zip"
IF NOT DEFINED FREESO_TSO_URL     SET "FREESO_TSO_URL=https://archive.org/download/TheSimsOnline_201802/TSO.zip"
IF NOT DEFINED FREESO_REMESH_URL  SET "FREESO_REMESH_URL=https://github.com/ItsSim/fsolauncher/releases/download/1.12.1-prod.24/remeshes-1.0.0-1726774408.zip"
SET "SERVER_URL=%FREESO_SERVER_URL%"
SET "CLIENT_URL=%FREESO_CLIENT_URL%"
SET "TSO_URL=%FREESO_TSO_URL%"
SET "REMESH_URL=%FREESO_REMESH_URL%"
:: Default to APPDATA (Roaming) so the install follows the user across
:: domain machines. Switch to %LOCALAPPDATA% if you'd rather it stay
:: pinned to a single PC (large install — ~1 GB after TSO content).
SET "DEFAULT_INSTALL=%APPDATA%\%GAME_NAME%"
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

:: ── Detect existing install ──────────────────────────────────────────────────
SET "EXISTING_CONFIG="
IF EXIST "%INSTALL_DIR%\FreeSO.exe" (
    ECHO.
    ECHO Existing install detected at %INSTALL_DIR%
    SET /P "REINSTALL_CHOICE=Update keeping config (u), Reinstall fresh (r), or Cancel (c)? [u]: "
    IF "!REINSTALL_CHOICE!"=="" SET "REINSTALL_CHOICE=u"
    IF /I "!REINSTALL_CHOICE!"=="c" (
        ECHO Cancelled.
        EXIT /B 0
    )
    IF /I "!REINSTALL_CHOICE!"=="u" (
        IF EXIST "%INSTALL_DIR%\Content\config.ini" (
            COPY /Y "%INSTALL_DIR%\Content\config.ini" "%TEMP%\edenso-config-backup.ini" >NUL
            SET "EXISTING_CONFIG=1"
            ECHO Backed up your existing Content\config.ini
        )
    )
)

ECHO.
ECHO Installing to: %INSTALL_DIR%
ECHO.

:: ── Optional components ──────────────────────────────────────────────────────
SET /P "INSTALL_TSO=Download and install The Sims Online game files? [y/n]: "
SET /P "INSTALL_REMESH=Install 3D remesh package (recommended)? [y/n]: "

ECHO.
ECHO Downloading and installing — please do not close this window.
ECHO.

:: ── Create install directory ─────────────────────────────────────────────────
IF NOT EXIST "%INSTALL_DIR%" MKDIR "%INSTALL_DIR%"

:: ── Download and extract client ──────────────────────────────────────────────
ECHO [1/4] Downloading %GAME_NAME% client...
IF NOT EXIST "%TEMP%\edenso-client.zip" (
    powershell -Command "Invoke-WebRequest '%CLIENT_URL%' -OutFile '%TEMP%\edenso-client.zip.incomplete'" ^
        && REN "%TEMP%\edenso-client.zip.incomplete" "edenso-client.zip"
    IF ERRORLEVEL 1 GOTO fail
)

ECHO [1/4] Extracting %GAME_NAME% client...
powershell -Command "Expand-Archive '%TEMP%\edenso-client.zip' -DestinationPath '%INSTALL_DIR%' -Force"
IF ERRORLEVEL 1 GOTO fail

:: ── Download and extract TSO ─────────────────────────────────────────────────
IF /I "%INSTALL_TSO%"=="y" (
    IF NOT EXIST "%TEMP%\TSO.zip" (
        ECHO [2/4] Downloading TSO game files...
        powershell -Command "Invoke-WebRequest '%TSO_URL%' -OutFile '%TEMP%\TSO.zip.incomplete'" ^
            && REN "%TEMP%\TSO.zip.incomplete" "TSO.zip"
        IF ERRORLEVEL 1 GOTO fail
    )

    ECHO [2/4] Extracting TSO game files...
    IF NOT EXIST "%TEMP%\tso-extract" MKDIR "%TEMP%\tso-extract"
    powershell -Command "Expand-Archive '%TEMP%\TSO.zip' -DestinationPath '%TEMP%\tso-extract' -Force"
    IF ERRORLEVEL 1 GOTO fail

    ECHO [2/4] Installing TSO content...
    IF NOT EXIST "%INSTALL_DIR%\game" MKDIR "%INSTALL_DIR%\game"
    extrac32.exe /Y /A /E "%TEMP%\tso-extract\Data1.cab" /L "%INSTALL_DIR%\game"
    IF ERRORLEVEL 1 GOTO fail
) ELSE (
    ECHO [2/4] Skipping TSO game files.
)

:: ── Download and extract remesh package ─────────────────────────────────────
IF /I "%INSTALL_REMESH%"=="y" (
    IF NOT EXIST "%TEMP%\edenso-remeshes.zip" (
        ECHO [3/4] Downloading 3D remesh package...
        powershell -Command "Invoke-WebRequest '%REMESH_URL%' -OutFile '%TEMP%\edenso-remeshes.zip.incomplete'" ^
            && REN "%TEMP%\edenso-remeshes.zip.incomplete" "edenso-remeshes.zip"
        IF ERRORLEVEL 1 GOTO fail
    )

    ECHO [3/4] Extracting 3D remesh package...
    IF NOT EXIST "%INSTALL_DIR%\Content\MeshReplace" MKDIR "%INSTALL_DIR%\Content\MeshReplace"
    powershell -Command "Expand-Archive '%TEMP%\edenso-remeshes.zip' -DestinationPath '%INSTALL_DIR%\Content\MeshReplace' -Force"
    IF ERRORLEVEL 1 GOTO fail
) ELSE (
    ECHO [3/4] Skipping remesh package.
)

:: ── Write config.ini ─────────────────────────────────────────────────────────
:: Absolute StartupPath so launches from Desktop / Start Menu work no
:: matter what working directory the launcher chose.
ECHO [4/4] Configuring client...
IF DEFINED EXISTING_CONFIG (
    COPY /Y "%TEMP%\edenso-config-backup.ini" "%INSTALL_DIR%\Content\config.ini" >NUL
    ECHO        Restored your existing Content\config.ini
) ELSE (
    (
        ECHO # %GAME_NAME% Settings File
        ECHO CurrentLang=english
        ECHO StartupPath=%INSTALL_DIR%\game\TSOClient\
        ECHO UseCustomServer=True
        ECHO GameEntryUrl=%SERVER_URL%
        ECHO CitySelectorUrl=%SERVER_URL%
    ) > "%INSTALL_DIR%\Content\config.ini"
)

:: ── Shortcuts: Desktop + Start Menu ──────────────────────────────────────────
ECHO Creating shortcuts...

SET "PWS=powershell.exe -ExecutionPolicy Bypass -NoLogo -NonInteractive -NoProfile"
SET "EXE=%INSTALL_DIR%\FreeSO.exe"

FOR /F "usebackq tokens=2,*" %%A IN (
    `reg query "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders" /v Desktop 2^>nul`
) DO SET "DESKTOP=%%B"

FOR /F "usebackq tokens=2,*" %%A IN (
    `reg query "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders" /v Programs 2^>nul`
) DO SET "STARTMENU=%%B"

:: Each shortcut is one PowerShell call on a single line. cmd's ^ line
:: continuation is NOT recognised inside double-quoted strings, so the
:: previous multi-line form was being parsed as "run the cmdlet ^",
:: producing  '^' is not recognized  errors. Single line, no ambiguity.
%PWS% -Command "$s = (New-Object -ComObject WScript.Shell).CreateShortcut('%DESKTOP%\%GAME_NAME%.lnk'); $s.TargetPath = '%EXE%'; $s.WorkingDirectory = '%INSTALL_DIR%'; $s.IconLocation = '%EXE%'; $s.Save()"

%PWS% -Command "$s = (New-Object -ComObject WScript.Shell).CreateShortcut('%DESKTOP%\%GAME_NAME% (3D).lnk'); $s.TargetPath = '%EXE%'; $s.Arguments = '-3d'; $s.WorkingDirectory = '%INSTALL_DIR%'; $s.IconLocation = '%EXE%'; $s.Save()"

IF NOT EXIST "%STARTMENU%\%GAME_NAME%" MKDIR "%STARTMENU%\%GAME_NAME%"

%PWS% -Command "$s = (New-Object -ComObject WScript.Shell).CreateShortcut('%STARTMENU%\%GAME_NAME%\%GAME_NAME%.lnk'); $s.TargetPath = '%EXE%'; $s.WorkingDirectory = '%INSTALL_DIR%'; $s.IconLocation = '%EXE%'; $s.Save()"

%PWS% -Command "$s = (New-Object -ComObject WScript.Shell).CreateShortcut('%STARTMENU%\%GAME_NAME%\%GAME_NAME% (3D).lnk'); $s.TargetPath = '%EXE%'; $s.Arguments = '-3d'; $s.WorkingDirectory = '%INSTALL_DIR%'; $s.IconLocation = '%EXE%'; $s.Save()"

:: ── Done ─────────────────────────────────────────────────────────────────────
ECHO.
ECHO  ================================================
ECHO   Installation complete!
ECHO   Launch via the Desktop or Start Menu shortcut,
ECHO   or run: "%INSTALL_DIR%\FreeSO.exe"
ECHO  ================================================
ECHO.
PAUSE
EXIT /B 0

:fail
ECHO.
ECHO  ================================================
ECHO   An error occurred during installation.
ECHO   Try running the installer again — partial
ECHO   downloads in %TEMP% will be reused.
ECHO  ================================================
ECHO.
PAUSE
EXIT /B 1