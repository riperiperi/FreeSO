luanet.load_assembly("Project Dollhouse Client")
luanet.load_assembly("System")
luanet.load_assembly("System.Threading")

UIScreen = luanet.import_type("TSOClient.LUI.UIScreen")
ContentManager = luanet.import_type("Project Dollhouse Client.ContentManager")
EventWaitHandle = luanet.import_type("System.Threading.EventWaitHandle")

LoadingScreen = UIScreen(ScreenManager)
ScreenManager:AddScreen(LoadingScreen, "")
ewh = nil

--others\\setup.bmp
LoadingScreen:LoadBackground(0x3a3, 0x001, "")

LoadingScreen:CreateLabel(11, "LblLoadText", 400, 600)

DateTime = luanet.import_type("System.DateTime")
Random = luanet.import_type("System.Random")

-- Called whenever the loadingscreen needs to be updated.
function UpdateLoadingscreen()
	Rnd = Random(DateTime.Now.Millisecond)
	RndNum = Rnd:Next(100)

	ewh = EventWaitHandle.OpenExisting("Go_Away_Stupid_Loading_Screen_GO_U_HEARD_ME_DONT_MAKE_ME_GET_MY_STICK_OUT");


	--This isn't really random enough, but it works.
	if RndNum >= 1 and RndNum <= 10 then
		LoadingScreen:UpdateLabelWithID("LblLoadText", 13)
	elseif RndNum >= 5 and RndNum <= 15 then
		LoadingScreen:UpdateLabelWithID("LblLoadText", 14)
	elseif RndNum >= 15 and RndNum <= 25 then
		LoadingScreen:UpdateLabelWithID("LblLoadText", 15)
	elseif RndNum >= 25 and RndNum <= 35 then
		LoadingScreen:UpdateLabelWithID("LblLoadText", 16)
	--elseif RndNum >= 20 and RndNum <= 30 then
		--LoadingScreen:UpdateLabelWithID("LblLoadText", 17)
	--elseif RndNum >= 35 and RndNum <= 45 then
		--LoadingScreen:UpdateLabelWithID("LblLoadText", 18)
	elseif RndNum >= 40 and RndNum <= 50 then
		LoadingScreen:UpdateLabelWithID("LblLoadText", 19)
	elseif RndNum >= 45 and RndNum <= 55 then
		LoadingScreen:UpdateLabelWithID("LblLoadText", 20)
	elseif RndNum >= 50 and RndNum <= 60 then
		LoadingScreen:UpdateLabelWithID("LblLoadText", 21)
	elseif RndNum >= 70 and RndNum <= 80 then
		LoadingScreen:UpdateLabelWithID("LblLoadText", 22)
	elseif RndNum >= 90 and RndNum <= 100 then
		LoadingScreen:UpdateLabelWithID("LblLoadText", 23)
	else
		LoadingScreen:UpdateLabelWithID("LblLoadText", 13)
	end
end

function Update()
	if ewh ~= nil and ewh:WaitOne(1) == true then
		LoadingDone()
	end
end

-- Called when loading is done.
function LoadingDone()
	LoginScreen = UIScreen(ScreenManager)


	ScreenManager:RemoveScreen(LoadingScreen)
	ScreenManager:AddScreen(LoginScreen,
		"gamedata\\luascripts\\login.lua")
end
