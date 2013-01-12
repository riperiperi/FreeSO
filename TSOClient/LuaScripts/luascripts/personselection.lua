luanet.load_assembly("PProject Dollhouse Client")
luanet.load_assembly("System")
luanet.load_assembly("System.Collections.Generic")

UIScreen = luanet.import_type("TSOClient.LUI.UIScreen")
PersonSelectionScreen = ScreenManager.CurrentUIScreen

PersonSelectionScreen:LoadBackground(0x3FA, 0x001, "")
PersonSelectionScreen:CreateButton(0x895, 0x001, 10, 8, 1, false, "CreditsButton")
PersonSelectionScreen:CreateButton(0x3FF, 0x001, 762, 8, 1, false, "ExitButton")

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
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 110, 500, 11, 1, "CreateASimBtn1")
	PersonSelectionScreen:AddScaleFactorToButton("CreateASimBtn1", 0.4, 0.2)
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 375, 500, 11, 1, "CreateASimBtn2")
	PersonSelectionScreen:AddScaleFactorToButton("CreateASimBtn2", 0.4, 0.2)
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 645, 500, 11, 1, "CreateASimBtn3")
	PersonSelectionScreen:AddScaleFactorToButton("CreateASimBtn3", 0.4, 0.2)

	PersonSelectionScreen:CreateButton(0x7C8, 0x001, 70, 80, 2, false, "AvatarButton1")
	PersonSelectionScreen:CreateButton(0x7C8, 0x001, 330, 80, 2, false, "AvatarButton2")
	PersonSelectionScreen:CreateButton(0x7C8, 0x001, 590, 80, 2, false, "AvatarButton3")

	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 64, 317, 1, true, "EnterLotButton1")
	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 325, 317, 1, true, "EnterLotButton2")
	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 585, 317, 1, true, "EnterLotButton3")

	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 192, 317, 1, true, "DescriptionTabBtn1")
	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 453, 317, 1, true, "DescriptionTabBtn2")
	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 713, 317, 1, true, "DescriptionTabBtn3")
elseif PlayerAccount.Sims.Count == 1 then
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 110, 500, 11, 1, "CreateASimBtn1")
	PersonSelectionScreen:AddScaleFactorToButton("CreateASimBtn1", 7, 4)
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 375, 500, 11, 1, "CreateASimBtn2")
	PersonSelectionScreen:AddScaleFactorToButton("CreateASimBtn2", 7, 4)
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 375, 500, 12, 1, "RetireASimBtn1")
	PersonSelectionScreen:AddScaleFactorToButton("RetireASimBtn1", 7, 4)

	PersonSelectionScreen:CreateButton(0x7C8, 0x001, 70, 80, 2, false, "AvatarButton1")
	PersonSelectionScreen:CreateButton(0x7C8, 0x001, 330, 80, 2, false, "AvatarButton2")
	PersonSelectionScreen:CreateButton(0x7C9, 0x001, 590, 80, 2, false, "AvatarButton3")

	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 64, 317, 1, true, "EnterLotButton1")
	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 325, 317, 1, true, "EnterLotButton2")
	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 585, 317, 1, false, "EnterLotButton3")

	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 192, 317, 1, true, "DescriptionTabBtn1")
	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 453, 317, 1, true, "DescriptionTabBtn2")
	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 713, 317, 1, false, "DescriptionTabBtn3")

	PersonSelectionScreen:CreateTextLabel(PlayerAccount.Sims[0].Name, "PersonNameText1", 22, 52)
elseif PlayerAccount.Sims.Count == 2 then
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 110, 500, 11, 1, "CreateASimBtn1")
	PersonSelectionScreen:AddScaleFactorToButton("CreateASimBtn1", 7, 4)
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 110, 500, 12, 1, "RetireASimBtn1")
	PersonSelectionScreen:AddScaleFactorToButton("RetireASimBtn1", 7, 4)
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 110, 500, 12, 1, "RetireASimBtn2")
	PersonSelectionScreen:AddScaleFactorToButton("RetireASimBtn2", 7, 4)

	PersonSelectionScreen:CreateButton(0x7C8, 0x001, 70, 80, 2, false, "AvatarButton1")
	PersonSelectionScreen:CreateButton(0x7C9, 0x001, 330, 80, 2, false, "AvatarButton2")
	PersonSelectionScreen:CreateButton(0x7C9, 0x001, 590, 80, 2, false, "AvatarButton3")

	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 64, 317, 1, true, "EnterLotButton1")
	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 325, 317, 1, false, "EnterLotButton2")
	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 585, 317, 1, false, "EnterLotButton3")

	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 192, 317, 1, true, "DescriptionTabBtn1")
	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 453, 317, 1, false, "DescriptionTabBtn2")
	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 713, 317, 1, false, "DescriptionTabBtn3")

	PersonSelectionScreen:CreateTextLabel(PlayerAccount.Sims[0].Name, "PersonNameText1", 22, 52)
	PersonSelectionScreen:CreateTextLabel(PlayerAccount.Sims[1].Name, "PersonNameText2", 282, 52)
elseif PlayerAccount.Sims.Count == 3 then
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 110, 500, 12, 1, "RetireASimBtn1")
	PersonSelectionScreen:AddScaleFactorToButton("RetireASimBtn1", 7, 4)
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 110, 500, 12, 1, "RetireASimBtn2")
	PersonSelectionScreen:AddScaleFactorToButton("RetireASimBtn2", 7, 4)
	PersonSelectionScreen:CreateTextButton(0x1E7, 0x001, 110, 500, 12, 1, "RetireASimBtn3")
	PersonSelectionScreen:AddScaleFactorToButton("RetireASimBtn3", 7, 4)

	PersonSelectionScreen:CreateButton(0x7C9, 0x001, 70, 80, 2, "AvatarButton1")
	PersonSelectionScreen:CreateButton(0x7C9, 0x001, 330, 80, 2, "AvatarButton2")
	PersonSelectionScreen:CreateButton(0x7C9, 0x001, 590, 80, 2, "AvatarButton3")

	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 64, 317, 1, false, "EnterLotButton1")
	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 325, 317, 1, false, "EnterLotButton2")
	PersonSelectionScreen:CreateButton(0x7c1, 0x001, 585, 317, 1, false, "EnterLotButton3")

	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 192, 317, 1, false, "DescriptionTabBtn1")
	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 453, 317, 1, false, "DescriptionTabBtn2")
	PersonSelectionScreen:CreateButton(0x7C0, 0x001, 713, 317, 1, false, "DescriptionTabBtn3")

	PersonSelectionScreen:CreateTextLabel(PlayerAccount.Sims[0].Name, "PersonNameText1", 22, 52)
	PersonSelectionScreen:CreateTextLabel(PlayerAccount.Sims[1].Name, "PersonNameText2", 282, 52)
	PersonSelectionScreen:CreateTextLabel(PlayerAccount.Sims[2].Name, "PersonNameText3", 542, 52)
end

-- TextID 11: "You Three Sims: Which One Do You Want To Play?"
PersonSelectionScreen:CreateLabel(8, "LblPlaySims", 175, 10)

PersonSelectionScreen:CreateLabel(9, "LblTime", 648, 8)

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
		ScreenManager:AddScreen(CASScreen, "gamedata\\luascripts\\personselectionedit.lua")
	elseif Button.StrID == "CreateASimBtn2" then
		LuaFunctions.StopMusicTrack(CurrentChannelPlaying)
		LuaFunctions.RemoveAllMusictracks()

		ScreenManager:RemoveScreen(PersonSelectionScreen)
		CASScreen = UIScreen(ScreenManager)
		ScreenManager:AddScreen(CASScreen, "gamedata\\luascripts\\personselectionedit.lua")
	elseif Button.StrID == "CreateASimBtn3" then
		LuaFunctions.StopMusicTrack(CurrentChannelPlaying)
		LuaFunctions.RemoveAllMusictracks()

		ScreenManager:RemoveScreen(PersonSelectionScreen)
		CASScreen = UIScreen(ScreenManager)
		ScreenManager:AddScreen(CASScreen, "gamedata\\luascripts\\personselectionedit.lua")
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
