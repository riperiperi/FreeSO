luanet.load_assembly("Project Dollhouse Client")
--luanet.load_assembly("TSOClient.LUI")

IsometricView = luanet.import_type("TSOClient.LUI.IsometricView")
ThreeDScene = luanet.import_type("TSOClient.ThreeD.ThreeDScene")
UIScreen = luanet.import_type("TSOClient.LUI.UIScreen")

Sim = luanet.import_type("TSOClient.VM.Sim")
Guid = luanet.import_type("System.Guid")

CASScreen = ScreenManager.CurrentUIScreen
CASScreen:LoadBackground(0x3DC, 0x001, "")

--Create a 3D-scene in order to render sims.
SimScene = ThreeDScene(ThreeDManager)
ThreeDManager:AddScene(SimScene)

CurrentID = 1
CurrentChannelPlaying = LuaFunctions.LoadMusictrack(StartupPath ..
"\\music\\modes\\create\\tsocas1_v2.mp3", CurrentID, true)


CASScreen:CreateLabel(10, "LblCreateSims", 275, 11)

--Alpha is: 1 = (255,0,255), 2 = (254, 2, 254), 3 = (255, 1, 255)
CASScreen:CreateButton(0x3E0, 0x001, 743, 58, 1, false, "CancelButton")
CASScreen:CreateNetworkButton(0x81C, 0x001, 743, 114, 1, false, "AcceptButton")
CASScreen:CreateButton(0x81D, 0x001, 762, 8, 1, false, "ExitButton")

CASScreen:CreateButton(0x3E4, 0x001, 292, 142, 1, false, "FemaleButton")
CASScreen:CreateButton(0x3EB, 0x001, 292, 198, 1, false, "MaleButton")
CASScreen:CreateButton(0x81F, 0x001, 294, 271, 1, false, "SkinLightButton")
CASScreen:CreateButton(0x820, 0x001, 294, 321, 1, false, "SkinMediumButton")
CASScreen:CreateButton(0x81E, 0x001, 294, 372, 1, false, "SkinDarkButton")

CASScreen:CreateButton(0x822, 0x001, 240, 360, 1, false, "ScrollUpButton")
CASScreen:CreateButton(0x821, 0x001, 241, 557, 1, false, "ScrollDownButton")

CASScreen:CreateTextEdit(33, 364, 194, 195, false, 499)

Heads = CASScreen:CreateHeadCatalogBrowser(378, 77, "HeadBrowser")

Bodies = CASScreen:CreateBodyCatalogBrowser(370, 348, "BodyBrowser")

ThreeDView = SimScene:Create3DView(180, 280, true, "3DView")
CharGuid = Guid.NewGuid()
Character = Sim(CharGuid:ToString())
Character.HeadXPos = 4.0
Character.HeadYPos = 6.0

Male = false;
Tone = 0;
function ButtonHandler(Button)
	if Button.StrID == "ExitButton" then
		LuaFunctions.ApplicationExit(0)
	elseif Button.StrID == "MaleButton" then
		Heads:SetGender(true)
		Bodies:SetGender(true);
		Male = true
	elseif Button.StrID == "FemaleButton" then
		Heads:SetGender(false)
		Bodies:SetGender(false)
		Male = false
	elseif Button.StrID == "SkinLightButton" then
		Heads:SetSkinColor(0)
		Bodies:SetSkinColor(0)
		Tone = 0
	elseif Button.StrID == "SkinMediumButton" then
		Heads:SetSkinColor(1)
		Bodies:SetSkinColor(1)
		Tone = 1
	elseif Button.StrID == "SkinDarkButton" then
		Heads:SetSkinColor(2)
		Bodies:SetSkinColor(2)
		Tone = 2
	elseif Button.StrID == Heads.StrID .. "LeftArrow" then
		Heads:GoLeft()
	elseif Button.StrID == Heads.StrID .. "RightArrow" then
		Heads:GoRight()
	elseif Button.StrID == Bodies.StrID .. "LeftArrow" then
		Bodies:GoLeft()
	elseif Button.StrID == Bodies.StrID .. "RightArrow" then
		Bodies:GoRight()
	elseif Button.StrID == "CancelButton" then
		LuaFunctions.StopMusicTrack(CurrentChannelPlaying)

		PersonSelectionScreen = UIScreen(ScreenManager)
		ScreenManager:RemoveScreen(CASScreen)
		ThreeDManager:RemoveScene(SimScene)
		ScreenManager:AddScreen(PersonSelectionScreen, "gamedata\\luascripts\\personselection.lua")
	elseif Button.StrID == "AcceptButton" then
		LotView = IsometricView(ScreenManager)
		ScreenManager:RemoveScreen(CASScreen)
		ThreeDManager:RemoveScene(SimScene)
		ScreenManager:AddScreen(LotView, "gamedata\\luascripts\\isometriclotview.lua")
	else
				ThreeDView:LoadHeadMesh(Character, Heads:GetOutfitFromStrID(Button.StrID), Heads.SkinColor)
	end
end

function Update()
	if Male == false then
		CASScreen:HighlightButton("FemaleButton")
	else
		CASScreen:HighlightButton("MaleButton")
	end
	if Tone == 0 then
		CASScreen:HighlightButton("SkinLightButton")
	elseif Tone == 1 then
		CASScreen:HighlightButton("SkinMediumButton")
	elseif Tone == 2 then
		CASScreen:HighlightButton("SkinDarkButton")
	end
end
