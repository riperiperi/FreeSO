!include LogicLib.nsh

Name "Project Dollhouse Client Installer"

Outfile "PDInstaller.exe"

RequestExecutionLevel admin

Page directory
Page instfiles

Var MaxisRegistry

Function .onInit
	ReadRegStr $MaxisRegistry HKLM 'SOFTWARE\Maxis\The Sims Online\' 'InstallDir'

	${If} $MaxisRegistry != ""
		StrCpy $INSTDIR  '$MaxisRegistry'
	${ElseIf} $MaxisRegistry == ""
		${If} ${FileExists} '$PROGRAMFILES\Maxis\The Sims Online'
			StrCpy $INSTDIR '$PROGRAMFILES\Maxis\The Sims Online\'
		${ElseIf} ${FileExists} '$PROGRAMFILES32\Maxis\The Sims Online\'
			StrCpy $INSTDIR '$PROGRAMFILES32\Maxis\The Sims Online\'
		${ElseIf} ${FileExists} '$PROGRAMFILES64\Maxis\The Sims Online\'
			StrCpy $INSTDIR '$PROGRAMFILES64\Maxis\The Sims Online\'
		${Else}
			MessageBox MB_OK "Couldn't find TSO installed on your system! Please locate the path!"
		${EndIf}
	${EndIf}
FunctionEnd

Section "InstallLua"
	CreateDirectory '$TEMP\PDInstaller'
	;Where files are installed to...
	SetOutPath '$TEMP\PDInstaller'

	MessageBox MB_YESNO "Install Lua for Windows?" /SD IDYES IDNO EndInstallLua
	File "lua\LuaForWindows_v5.1.4-45.exe"
	ExecWait '$TEMP\PDInstaller\LuaForWindows_v5.1.4-45.exe'

	EndInstallLua:
SectionEnd

Section "InstallBASS"
	MessageBox MB_YESNO "Install Bass.NET?" /SD IDYES IDNO EndInstallBASS
	File "bass\Bass.Net.msi"
	ExecWait '"msiexec" /i "$TEMP\PDInstaller\Bass.Net.msi"'

	EndInstallBASS:
SectionEnd

Section "InstallXNA"
	MessageBox MB_YESNO "Install XNA 3.1?" /SD IDYES IDNO EndInstallXNA
	File "xna\xnafx31_redist.msi"
	ExecWait '"msiexec" /i "$TEMP\PDInstaller\xnafx31_redist.msi"'

	EndInstallXNA:
SectionEnd

Section "Main"
	CreateDirectory '$INSTDIR\TSOClient\gamedata\luascripts'

	;Where files are installed to...
	SetOutPath '$INSTDIR\TSOClient\gamedata\luascripts\'

	File "pdclient\gamedata\luascripts\credits.lua"
	File "pdclient\gamedata\luascripts\loading.lua"
	File "pdclient\gamedata\luascripts\login.lua"
	File "pdclient\gamedata\luascripts\personselection.lua"
	File "pdclient\gamedata\luascripts\personselectionedit.lua"

	CreateDirectory '$INSTDIR\TSOClient\gamedata\settings'
	SetOutPath '$INSTDIR\TSOClient\gamedata\settings\'

	File "pdclient\gamedata\settings\settings.lua"

	CreateDirectory '$INSTDIR\TSOClient\gamedata\uitext\luatext'
	CreateDirectory '$INSTDIR\TSOClient\gamedata\uitext\luatext\english'
	CreateDirectory '$INSTDIR\TSOClient\gamedata\uitext\luatext\norwegian'
	SetOutPath '$INSTDIR\TSOClient\gamedata\uitext\luatext\english\'

	File "pdclient\gamedata\uitext\luatext\english\english.lua"
	SetOutPath '$INSTDIR\TSOClient\gamedata\uitext\luatext\norwegian\'
	File "pdclient\gamedata\uitext\luatext\norwegian\norwegian.lua"

	SetOutPath '$INSTDIR\TSOClient\'

	File "pdclient\SimsLib.dll"
	File "pdclient\Project Dollhouse Client.exe"
	File "pdclient\LuaInterface.dll"
	File "pdclient\Bass.Net.dll"
	File "pdclient\lua51.dll"
	File "pdclient\Project Dollhouse Client.exe.config"
	File "pdclient\NAudio.dll"
	File "pdclient\bass.dll"

	SetOutPath '$INSTDIR\TSOPatch\'
	
	File "pdclient\PDPatcher.exe"
	File "pdclient\ICSharpCode.SharpZipLib.dll"

	CreateDirectory '$INSTDIR\TSOClient\Content'
	SetOutPath '$INSTDIR\TSOClient\Content\'

	File "pdclient\Content\login.xnb"
	File "pdclient\Content\ComicSans.xnb"
	File "pdclient\Content\ComicSansSmall.xnb"
SectionEnd