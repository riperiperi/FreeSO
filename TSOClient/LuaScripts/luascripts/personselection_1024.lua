luanet.load_assembly("PProject Dollhouse Client")
luanet.load_assembly("System")
luanet.load_assembly("System.Collections.Generic")

UIScreen = luanet.import_type("TSOClient.LUI.UIScreen")
PersonSelectionScreen = UIScreen(ScreenManager)
ScreenManager:AddScreen(PersonSelectionScreen,"")

PersonSelectionScreen:LoadBackground(0x3FA, 0x001, "")
PersonSelectionScreen:CreateButton(0x895, 0x001, 10, 15, 1, false, "CreditsButton")
PersonSelectionScreen:CreateButton(0x3FF, 0x001, 978, 15, 1, false, "ExitButton")

--Different tracks that can be played on this screen.
MusicTracks = {}
MusicTracks[1] = StartupPath .. "\\music\\modes\\select\\tsosas1_v2.mp3"
MusicTracks[2] = StartupPath .. "\\music\\modes\\select\\tsosas2_v2.mp3"
MusicTracks[3] = StartupPath .. "\\music\\modes\\select\\tsosas3.mp3"
MusicTracks[4] = StartupPath .. "\\music\\modes\\select\\tsosas4.mp3"
MusicTracks[5] = StartupPath .. "\\music\\modes\\select\\tsosas5.mp3"

LuaFunctions = luanet.import_type("TSOClient.LuaFunctions")

CurrentID = 1
CurrentChannelPlaying = LuaFunctions.LoadMusictrack(MusicTracks[math.random(6)],
													CurrentID, false)

PlayerAccount = luanet.import_type("TSOClient.PlayerAccount")
ThreeDScene = luanet.import_type("TSOClient.ThreeD.ThreeDScene")

--Create a 3D-scene in order to render sims.
SimScene = ThreeDScene(ThreeDManager)
ThreeDManager:AddScene(SimScene)

--Decide which buttons to create based on how many sims
--the current account has.
if PlayerAccount.Sims.Count == 0 then
	--TextID 11: "Create A Sim"
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 142, 500, 11, 1, "CreateASimBtn1")
	PersonSelectionScreen:AddScaleFactorToButton("CreateASimBtn1", 0.20, 0.2)
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 475, 500, 11, 1, "CreateASimBtn2")
	PersonSelectionScreen:AddScaleFactorToButton("CreateASimBtn2", 0.20, 0.2)
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 810, 500, 11, 1, "CreateASimBtn3")
	PersonSelectionScreen:AddScaleFactorToButton("CreateASimBtn3", 0.20, 0.2)

	PersonSelectionScreen:CreateButton(0x7C8, 0x001, 102, 115, 2, false, "AvatarButton1")
	PersonSelectionScreen:AddScaleFactorToButton("AvatarButton1", 0.09, 0.17)
	PersonSelectionScreen:CreateButton(0x7C8, 0x001, 435, 115, 2, false, "AvatarButton2")
	PersonSelectionScreen:AddScaleFactorToButton("AvatarButton2", 0.09, 0.17)
	PersonSelectionScreen:CreateButton(0x7C8, 0x001, 768, 115, 2, false, "AvatarButton3")
	PersonSelectionScreen:AddScaleFactorToButton("AvatarButton3", 0.09, 0.17)

	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 84, 410, 1, true, "EnterLotButton1")
	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 420, 410, 1, true, "EnterLotButton2")
	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 753, 410, 1, true, "EnterLotButton3")

	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 250, 410, 1, true, "DescriptionTabBtn1")
	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 585, 410, 1, true, "DescriptionTabBtn2")
	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 916, 410, 1, true, "DescriptionTabBtn3")
elseif PlayerAccount.Sims.Count == 1 then
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 142, 500, 11, 1, "CreateASimBtn1")
	PersonSelectionScreen:AddScaleFactorToButton("CreateASimBtn1", 0.20, 0.2)
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 475, 500, 11, 1, "CreateASimBtn2")
	PersonSelectionScreen:AddScaleFactorToButton("CreateASimBtn2", 0.20, 0.2)
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 810, 500, 12, 1, "RetireASimBtn1")
	PersonSelectionScreen:AddScaleFactorToButton("RetireASimBtn1", 0.20, 0.2)

	PersonSelectionScreen:CreateButton(0x7C8, 0x001, 102, 115, 2, false, "AvatarButton1")
	PersonSelectionScreen:AddScaleFactorToButton("AvatarButton1", 0.09, 0.17)
	PersonSelectionScreen:CreateButton(0x7C8, 0x001, 435, 115, 2, false, "AvatarButton2")
	PersonSelectionScreen:AddScaleFactorToButton("AvatarButton2", 0.09, 0.17)
	PersonSelectionScreen:CreateButton(0x7C8, 0x001, 768, 115, 2, false, "AvatarButton3")
	PersonSelectionScreen:AddScaleFactorToButton("AvatarButton3", 0.09, 0.17)

	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 84, 410, 1, true, "EnterLotButton1")
	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 420, 410, 1, true, "EnterLotButton2")
	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 753, 410, 1, false, "EnterLotButton3")

	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 192, 410, 1, true, "DescriptionTabBtn1")
	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 585, 410, 1, true, "DescriptionTabBtn2")
	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 916, 410, 1, false, "DescriptionTabBtn3")

	PersonSelectionScreen:CreateTextLabel(PlayerAccount.Sims[0].Name, "PersonNameText1", 22, 52)
elseif PlayerAccount.Sims.Count == 2 then
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 142, 500, 11, 1, "CreateASimBtn1")
	PersonSelectionScreen:AddScaleFactorToButton("CreateASimBtn1", 0.20, 0.2)
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 475, 500, 12, 1, "RetireASimBtn2")
	PersonSelectionScreen:AddScaleFactorToButton("RetireASimBtn1", 0.20, 0.2)
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 810, 500, 12, 1, "RetireASimBtn3")
	PersonSelectionScreen:AddScaleFactorToButton("RetireASimBtn2", 0.20, 0.2)

	PersonSelectionScreen:CreateButton(0x7C8, 0x001, 102, 115, 2, false, "AvatarButton1")
	PersonSelectionScreen:AddScaleFactorToButton("AvatarButton1", 0.09, 0.17)
	PersonSelectionScreen:CreateButton(0x7C8, 0x001, 435, 115, 2, false, "AvatarButton2")
	PersonSelectionScreen:AddScaleFactorToButton("AvatarButton2", 0.09, 0.17)
	PersonSelectionScreen:CreateButton(0x7C8, 0x001, 768, 115, 2, false, "AvatarButton3")
	PersonSelectionScreen:AddScaleFactorToButton("AvatarButton3", 0.09, 0.17)

	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 84, 410, 1, true, "EnterLotButton1")
	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 420, 410, 1, false, "EnterLotButton2")
	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 753, 410, 1, false, "EnterLotButton3")

	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 192, 410, 1, true, "DescriptionTabBtn1")
	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 585, 410, 1, false, "DescriptionTabBtn2")
	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 916, 410, 1, false, "DescriptionTabBtn3")

	PersonSelectionScreen:CreateTextLabel(PlayerAccount.Sims[0].Name, "PersonNameText1", 22, 52)
	PersonSelectionScreen:CreateTextLabel(PlayerAccount.Sims[1].Name, "PersonNameText2", 282, 52)
elseif PlayerAccount.Sims.Count == 3 then
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 142, 500, 12, 1, "RetireASimBtn1")
	PersonSelectionScreen:AddScaleFactorToButton("RetireASimBtn1", 0.20, 0.2)
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 475, 500, 12, 1, "RetireASimBtn2")
	PersonSelectionScreen:AddScaleFactorToButton("RetireASimBtn2", 0.20, 0.2)
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 810, 500, 12, 1, "RetireASimBtn3")
	PersonSelectionScreen:AddScaleFactorToButton("RetireASimBtn3", 0.20, 0.2)

	PersonSelectionScreen:CreateButton(0x7C8, 0x001, 102, 115, 2, false, "AvatarButton1")
	PersonSelectionScreen:AddScaleFactorToButton("AvatarButton1", 0.09, 0.17)
	PersonSelectionScreen:CreateButton(0x7C8, 0x001, 435, 115, 2, false, "AvatarButton2")
	PersonSelectionScreen:AddScaleFactorToButton("AvatarButton2", 0.09, 0.17)
	PersonSelectionScreen:CreateButton(0x7C8, 0x001, 768, 115, 2, false, "AvatarButton3")
	PersonSelectionScreen:AddScaleFactorToButton("AvatarButton3", 0.09, 0.17)

	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 84, 410, 1, false, "EnterLotButton1")
	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 420, 410, 1, false, "EnterLotButton2")
	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 753, 410, 1, false, "EnterLotButton3")

	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 192, 410, 1, false, "DescriptionTabBtn1")
	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 585, 410, 1, false, "DescriptionTabBtn2")
	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 916, 410, 1, false, "DescriptionTabBtn3")

	PersonSelectionScreen:CreateTextLabel(PlayerAccount.Sims[0].Name, "PersonNameText1", 22, 52)
	PersonSelectionScreen:CreateTextLabel(PlayerAccount.Sims[1].Name, "PersonNameText2", 282, 52)
	PersonSelectionScreen:CreateTextLabel(PlayerAccount.Sims[2].Name, "PersonNameText3", 542, 52)
end

-- TextID 11: "You Three Sims: Which One Do You Want To Play?"
PersonSelectionScreen:CreateLabel(8, "LblPlaySims", 280, 15)

PersonSelectionScreen:CreateLabel(9, "LblTime", 840, 15)

GlobalSettings = luanet.import_type("TSOClient.GlobalSettings")

if GlobalSettings.Default.ShowHints == true then
	PersonSelectionScreen:CreateInfoPopup(210, 250, 1, "hint1.bmp", 7)
end

UIButton = luanet.import_type("TSOClient.LUI.UIButton")
function ButtonHandler(Button)
	--Button.CurrentFrame = Button.CurrentFrame + 1

	if Button.StrID == "ExitButton" then
		LuaFunctions.ApplicationExit(0)
	elseif Button.StrID == "CreditsButton" then
		LuaFunctions.StopMusicTrack(CurrentChannelPlaying)
		LuaFunctions.RemoveAllMusictracks()

		ScreenManager:RemoveScreen(PersonSelectionScreen)
		CreditsScreen = UIScreen(ScreenManager)
		ScreenManager:AddScreen(CreditsScreen, "gamedata\\luascripts\\credits.lua")
	elseif Button.StrID == "CreateASimBtn1" then
		LuaFunctions.StopMusicTrack(CurrentChannelPlaying)
		LuaFunctions.RemoveAllMusictracks()

		ScreenManager:RemoveScreen(PersonSelectionScreen)
		CASScreen = UIScreen(ScreenManager)
		ScreenManager:AddScreen(CASScreen, "gamedata\\luascripts\\personselectionedit_1024.lua")
	elseif Button.StrID == "CreateASimBtn2" then
		LuaFunctions.StopMusicTrack(CurrentChannelPlaying)
		LuaFunctions.RemoveAllMusictracks()

		ScreenManager:RemoveScreen(PersonSelectionScreen)
		CASScreen = UIScreen(ScreenManager)
		ScreenManager:AddScreen(CASScreen, "gamedata\\luascripts\\personselectionedit_1024.lua")
	elseif Button.StrID == "CreateASimBtn3" then
		LuaFunctions.StopMusicTrack(CurrentChannelPlaying)
		LuaFunctions.RemoveAllMusictracks()

		ScreenManager:RemoveScreen(PersonSelectionScreen)
		CASScreen = UIScreen(ScreenManager)
		ScreenManager:AddScreen(CASScreen, "gamedata\\luascripts\\personselectionedit_1024.lua")
	elseif Button.StrID == "OKCheckBtn" then
		PersonSelectionScreen:RemoveInfoPopup(1);
	end
end

DateTime = luanet.import_type("System.DateTime")

function Update()
	PersonSelectionScreen:UpdateLabel("LblTime", DateTime.Now:ToString("hh:mm:ss"))

	if LuaFunctions.IsMusictrackPlaying(CurrentChannelPlaying) == false then
		CurrentID = CurrentID + 1
		CurrentChannelPlaying = LuaFunctions.LoadMusictrack(
											 MusicTracks[math.random(6)],
											 CurrentID, false)
	end
end
