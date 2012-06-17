luanet.load_assembly("Project Dollhouse Client")
luanet.load_assembly("System")
luanet.load_assembly("System.Collections.Generic")

UIScreen = luanet.import_type("TSOClient.LUI.UIScreen")
CreditsScreen = UIScreen(ScreenManager)
ScreenManager:AddScreen(CreditsScreen, "")
CreditsScreen:LoadBackground(0x8AC, 0x001, "")

--Alpha is: 1 = (255,0,255), 2 = (254, 2, 254), 3 = (255, 1, 255)
CreditsScreen:CreateButton(0x8AA, 0x001, 10, 8, 1, false, "BackButton")
CreditsScreen:CreateButton(0x8AD, 0x001, 762, 8, 1, false, "ExitButton")
CreditsScreen:CreateButton(0x8AE, 0x001, 71, 440, 3, false, "MaxisButton")
CreditsScreen:CreateImage(0x8AF, 0x001, 15, 82, 2, "LogoButton")

LuaFunctions = luanet.import_type("TSOClient.LuaFunctions")

function ButtonHandler(Button)
	if Button.StrID == "ExitButton" then
		LuaFunctions.ApplicationExit(0)
    elseif Button.StrID == "BackButton" then
		PersonSelectionScreen = UIScreen(ScreenManager)
		ScreenManager:RemoveScreen(CreditsScreen)
		ScreenManager:AddScreen(PersonSelectionScreen,
		"gamedata\\luascripts\\personselection.lua")
	end
end

function Update()
end
