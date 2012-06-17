luanet.load_assembly("Project Dollhouse Client")
luanet.load_assembly("System")
luanet.load_assembly("System.Collections.Generic")

UIScreen = luanet.import_type("TSOClient.LUI.UIScreen")
LoginScreen = UIScreen(ScreenManager)
ScreenManager:AddScreen(LoginScreen, "")

LoginScreen:LoadBackground(0x00, 0x00, "login")
LoginScreen:CreateLoginDialog(220, 150)

LuaFunctions = luanet.import_type("TSOClient.LuaFunctions")

function ButtonHandler(Button)
	if Button.StrID == "BtnExit" then
		LuaFunctions.ApplicationExit(0)
	end
end

function Update()
end

function LoginSuccess()
	PersonSelectionScreen = UIScreen(ScreenManager)

	ScreenManager:RemoveScreen(LoginScreen)
	ScreenManager:AddScreen(PersonSelectionScreen,
		"gamedata\\luascripts\\personselection.lua")
end
