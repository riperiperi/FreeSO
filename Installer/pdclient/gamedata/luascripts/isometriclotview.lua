luanet.load_assembly("Project Dollhouse Client")
luanet.load_assembly("Microsoft")
luanet.load_assembly("Microsoft.Xna")
luanet.load_assembly("Microsoft.Xna.Framework")

IsometricView = luanet.import_type("TSOClient.LUI.IsometricView")
Mouse = luanet.import_type("Microsoft.Xna.Framework.Input.Mouse")

IsometricScreen = IsometricView(ScreenManager)
ScreenManager:AddScreen(IsometricScreen,"")

--Alpha is: 1 = (255,0,255), 2 = (254, 2, 254), 3 = (255, 1, 255)
imageTypeMask = 0x00000001;

-- UCP
liveModeButtonImage = 0x000004BD;
buyModeButtonImage = 0x000004B5;
buildModeButtonImage = 0x000004B4;
houseModeButtonImage = 0x000004BB;
optionsModeButtonImage = 0x000004C0;
wallsDownButtonImage = 0x000004C7;
wallsCutawayButtonImage = 0x000004C6;
wallsUpButtonImage = 0x000004C8;
roofButtonImage = 0x000004C2;
secondFloorButtonImage = 0x000004C5;
firstFloorButtonImage = 0x000004B8;
closeZoomButtonImage = 0x000004B6;
mediumZoomButtonImage = 0x000004BE;
farZoomButtonImage = 0x000004B7;
neighborhoodButtonImage = 0x000004BF;
worldButtonImage = 0x000004C9;
zoomInButtonImage = 0x000004CA;
zoomOutButtonImage = 0x000004CB;
rotateClockwiseButtonImage = 0x000004C3;
rotateCounterClockwiseButtonImage= 0x000004C4;
friendshipWebButtonImage = 0x000004B9;
phoneButtonImage = 0x000004C1;
helpButtonImage = 0x000004BA;
budgetButtonImage = 0x000004B3;
houseViewSelectButtonImage = 0x000004BC;
bookmarkButtonImage = 0x000004B2;

-- THIS HAS A FLAG OF 2
BackgroundGameImage = 0x000000D6;
-- THIS HAS A FLAG OF 2
BackgroundMatchmakerImage = 0x000000D7;


IsometricScreen:CreateImage(BackgroundGameImage, 0x002, 0, 390, 0, "IsoView")

 -- Mode buttons

IsometricScreen:CreateButton(liveModeButtonImage, imageTypeMask, 8, 6, 1, false, "LiveModeButton")
IsometricScreen:CreateButton(buyModeButtonImage, imageTypeMask, 63, 16, 1, false, "BuyModeButton")
IsometricScreen:CreateButton(buildModeButtonImage, imageTypeMask, 107, 44, 1, false, "BuildModeButton")
IsometricScreen:CreateButton(houseModeButtonImage, imageTypeMask, 136, 81, 1, false, "HouseModeButton")
IsometricScreen:CreateButton(optionsModeButtonImage, imageTypeMask, 150, 118, 1, false, "OptionsModeButton")

 -- House view buttons

IsometricScreen:CreateButton(firstFloorButtonImage, imageTypeMask, 8, 82, 1, false, "FirstFloorButton")
IsometricScreen:CreateButton(secondFloorButtonImage, imageTypeMask, 7, 67, 1, false, "SecondFloorButton")

IsometricScreen:CreateButton(wallsDownButtonImage, imageTypeMask, 35, 69, 1, false, "WallsDownButton")
IsometricScreen:CreateButton(wallsCutawayButtonImage, imageTypeMask, 35, 69, 1, false, "WallsCutawayButton")
IsometricScreen:CreateButton(wallsUpButtonImage, imageTypeMask, 35, 69, 1, false, "WallsUpButton")
IsometricScreen:CreateButton(roofButtonImage, imageTypeMask, 35, 69, 1, false, "RoofViewButton")

 -- Zoom control buttons

IsometricScreen:CreateButton(closeZoomButtonImage, imageTypeMask, 67, 97, 1, false, "CloseZoomButton")
IsometricScreen:CreateButton(mediumZoomButtonImage, imageTypeMask, 49, 109, 1, false, "MediumZoomButton")
IsometricScreen:CreateButton(farZoomButtonImage, imageTypeMask, 44, 127, 1, false, "FarZoomButton")
IsometricScreen:CreateButton(neighborhoodButtonImage, imageTypeMask, 47, 147, 1, false, "NeighborhoodButton")
IsometricScreen:CreateButton(worldButtonImage, imageTypeMask, 61, 162, 1, false, "WorldButton")
IsometricScreen:CreateButton(zoomInButtonImage, imageTypeMask, 82, 110, 1, false, "ZoomInButton")
IsometricScreen:CreateButton(zoomOutButtonImage, imageTypeMask, 82, 141, 1, false, "ZoomOutButton")
IsometricScreen:CreateButton(rotateClockwiseButtonImage, imageTypeMask, 62, 125, 1, false, "RotateClockwiseButton")
IsometricScreen:CreateButton(rotateCounterClockwiseButtonImage, imageTypeMask, 103, 125, 1, false, "RotateCounterClockwiseButton")

 -- special buttons

IsometricScreen:CreateButton(friendshipWebButtonImage, imageTypeMask, 24, 171, 1, false, "FriendshipWebButton")
IsometricScreen:CreateButton(phoneButtonImage, imageTypeMask, 7, 140, 1, false, "PhoneButton")
IsometricScreen:CreateButton(helpButtonImage, imageTypeMask, 66, 184, 1, false, "HelpButton")
IsometricScreen:CreateButton(budgetButtonImage, imageTypeMask, 94, 177, 1, false, "BudgetButton")
IsometricScreen:CreateButton(houseViewSelectButtonImage, imageTypeMask, 52, 64, 1, false, "HouseViewSelectButton")
IsometricScreen:CreateButton(bookmarkButtonImage, imageTypeMask, 9, 110, 1, false, "BookmarkButton")


currentGraphicalMode = -1;

function PrepareBuildMode()
	isBuildMode = true
	currentGraphicalMode = 0;

	btnImage3 = 0x00000427;
	btnImage4 = 0x0000042B;
	btnImage5 = 0x00000429;
	btnImage6 = 0x0000042A;
	btnImage7 = 0x00000426;
	btnImage8 = 0x0000041D;
	btnImage9 = 0x00000420;
	btnImage10= 0x0000041E;
	btnImage11= 0x0000041C;
	btnImage12= 0x0000042C;
	btnImage13= 0x00000422;
	btnImage14= 0x0000041F;
	btnImagePreviousPage = 0x00000423;
	btnImageNextPage = 0x00000424;
	sliderImage = 0x00000425;
	subtoolsBackground = 0x0000041A;
	dividerImage = 0x0000041B;
	buildBackground = 0x000000D8;

	IsometricScreen:CreateImage(0x000000d8, 0x00000002, 177, 486, 0, "BGBuyBuild");

	IsometricScreen:CreateButton(btnImage3, imageTypeMask, 79 + 177, 14 + 96, 1, false, "TerrainButton")
	IsometricScreen:CreateButton(btnImage4, imageTypeMask, 129 + 177, 14 + 96, 1, false, "WaterButton")
	IsometricScreen:CreateButton(btnImage5, imageTypeMask, 168 + 177, 13 + 96, 1, false, "WallButton")
	IsometricScreen:CreateButton(btnImage6, imageTypeMask, 211 + 177, 12 + 96, 1, false, "WallpaperButton")
	IsometricScreen:CreateButton(btnImage7, imageTypeMask, 252 + 177, 13 + 96, 1, false, "StairButton")
	IsometricScreen:CreateButton(btnImage8, imageTypeMask, 297 + 177, 17 + 96, 1, false, "FireplaceButton")

	IsometricScreen:CreateButton(btnImage9, imageTypeMask, 77 + 177, 66 + 96, 1, false, "PlantButton")
	IsometricScreen:CreateButton(btnImage10, imageTypeMask, 119 + 177, 96 + 71, 1, false, "FloorButton")
	IsometricScreen:CreateButton(btnImage11, imageTypeMask, 172 + 177, 96 + 64, 1, false, "DoorButton")
	IsometricScreen:CreateButton(btnImage12, imageTypeMask, 214 + 177, 96 + 66, 1, false,"WindowButton")
	IsometricScreen:CreateButton(btnImage13, imageTypeMask, 250 + 177, 96 + 66, 1, false, "RoofButton")
	IsometricScreen:CreateButton(btnImage14, imageTypeMask, 298 + 177, 96 + 66, 1, false, "HandButton")
end

isTerrainToolSelected = false;
isWaterToolSelected = false;
isWallToolSelected = false;
isWallPaperToolSelected = false;
isStairToolSelected = false;
isFireplaceToolSelected = false;
isPlantToolSelected = false
isFloorToolSelected = false;
isDoorToolSelected = false;
isWindowToolSelected = false;
isRoofToolSelected = false;
isHandToolSelected = false;

function ButtonHandlerBuild(Button)
	shouldCreateCatalog = false
	shouldDestroyCatalog = true
	catalogName = "noCatalog"
	if Button.StrID == "TerrainButton" then
		isTerrainToolSelected = true;
		isWaterToolSelected = false;
		isWallToolSelected = false;
		isWallPaperToolSelected = false;
		isStairToolSelected = false;
		isFireplaceToolSelected = false;
		isPlantToolSelected = false
		isFloorToolSelected = false;
		isDoorToolSelected = false;
		isWindowToolSelected = false;
		isRoofToolSelected = false;
		isHandToolSelected = false;
	elseif Button.StrID == "WaterButton" then
		isTerrainToolSelected = false;
		isWaterToolSelected = true;
		isWallToolSelected = false;
		isWallPaperToolSelected = false;
		isStairToolSelected = false;
		isFireplaceToolSelected = false;
		isPlantToolSelected = false
		isFloorToolSelected = false;
		isDoorToolSelected = false;
		isWindowToolSelected = false;
		isRoofToolSelected = false;
		isHandToolSelected = false;
	elseif Button.StrID == "WallButton" then
		isTerrainToolSelected = false;
		isWaterToolSelected = false;
		isWallToolSelected = true;
		isWallPaperToolSelected = false;
		isStairToolSelected = false;
		isFireplaceToolSelected = false;
		isPlantToolSelected = false
		isFloorToolSelected = false;
		isDoorToolSelected = false;
		isWindowToolSelected = false;
		isRoofToolSelected = false;
		isHandToolSelected = false;
	elseif Button.StrID == "WallpaperButton" then
		isTerrainToolSelected = false;
		isWaterToolSelected = false;
		isWallToolSelected = false;
		isWallPaperToolSelected = true;
		isStairToolSelected = false;
		isFireplaceToolSelected = false;
		isPlantToolSelected = false
		isFloorToolSelected = false;
		isDoorToolSelected = false;
		isWindowToolSelected = false;
		isRoofToolSelected = false;
		isHandToolSelected = false;
	elseif Button.StrID == "StairButton" then
		isTerrainToolSelected = false;
		isWaterToolSelected = false;
		isWallToolSelected = false;
		isWallPaperToolSelected = false;
		isStairToolSelected = true;
		isFireplaceToolSelected = false;
		isPlantToolSelected = false
		isFloorToolSelected = false;
		isDoorToolSelected = false;
		isWindowToolSelected = false;
		isRoofToolSelected = false;
		isHandToolSelected = false;
	elseif Button.StrID == "FireplaceButton" then
		isTerrainToolSelected = false;
		isWaterToolSelected = false;
		isWallToolSelected = false;
		isWallPaperToolSelected = false;
		isStairToolSelected = false;
		isFireplaceToolSelected = true;
		isPlantToolSelected = false
		isFloorToolSelected = false;
		isDoorToolSelected = false;
		isWindowToolSelected = false;
		isRoofToolSelected = false;
		isHandToolSelected = false;
	elseif Button.StrID == "PlantButton" then
		isTerrainToolSelected = false;
		isWaterToolSelected = false;
		isWallToolSelected = false;
		isWallPaperToolSelected = false;
		isStairToolSelected = false;
		isFireplaceToolSelected = false;
		isPlantToolSelected = true
		isFloorToolSelected = false;
		isDoorToolSelected = false;
		isWindowToolSelected = false;
		isRoofToolSelected = false;
		isHandToolSelected = false;
	elseif Button.StrID == "FloorButton" then
		isTerrainToolSelected = false;
		isWaterToolSelected = false;
		isWallToolSelected = false;
		isWallPaperToolSelected = false;
		isStairToolSelected = false;
		isFireplaceToolSelected = false;
		isPlantToolSelected = false
		isFloorToolSelected = true;
		isDoorToolSelected = false;
		isWindowToolSelected = false;
		isRoofToolSelected = false;
		isHandToolSelected = false;
		shouldCreateCatalog = true
		catalogName = "Floor"
	elseif Button.StrID == "DoorButton" then
		isTerrainToolSelected = false;
		isWaterToolSelected = false;
		isWallToolSelected = false;
		isWallPaperToolSelected = false;
		isStairToolSelected = false;
		isFireplaceToolSelected = false;
		isPlantToolSelected = false
		isFloorToolSelected = false;
		isDoorToolSelected = true;
		isWindowToolSelected = false;
		isRoofToolSelected = false;
		isHandToolSelected = false;
	elseif Button.StrID == "WindowButton" then
		isTerrainToolSelected = false;
		isWaterToolSelected = false;
		isWallToolSelected = false;
		isWallPaperToolSelected = false;
		isStairToolSelected = false;
		isFireplaceToolSelected = false;
		isPlantToolSelected = false
		isFloorToolSelected = false;
		isDoorToolSelected = false;
		isWindowToolSelected = true;
		isRoofToolSelected = false;
		isHandToolSelected = false;
	elseif Button.StrID == "RoofButton" then
		isTerrainToolSelected = false;
		isWaterToolSelected = false;
		isWallToolSelected = false;
		isWallPaperToolSelected = false;
		isStairToolSelected = false;
		isFireplaceToolSelected = false;
		isPlantToolSelected = false
		isFloorToolSelected = false;
		isDoorToolSelected = false;
		isWindowToolSelected = false;
		isRoofToolSelected = true;
		isHandToolSelected = false;
	elseif Button.StrID == "HandButton" then
		isTerrainToolSelected = false;
		isWaterToolSelected = false;
		isWallToolSelected = false;
		isWallPaperToolSelected = false;
		isStairToolSelected = false;
		isFireplaceToolSelected = false;
		isPlantToolSelected = false
		isFloorToolSelected = false;
		isDoorToolSelected = false;
		isWindowToolSelected = false;
		isRoofToolSelected = false;
		isHandToolSelected = true;
	else
		shouldDestroyCatalog = false;
	end
	if shouldDestroyCatalog then
		IsometricScreen:RemoveAllCatalogs();
	end
	if shouldCreateCatalog then
		IsometricScreen:RemoveElement(catalogName.."Catalog")
		IsometricScreen:CreateCatalog(subtoolsBackground, imageTypeMask, 364, 593, 10, catalogName, catalogName.."Catalog")
		shouldCreateCatalog = false
	end
end

function UpdateBuildMode()
	IsometricScreen:HighlightButton("BuildModeButton");

	if isTerrainToolSelected then
		IsometricScreen:HighlightButton("TerrainButton");
	elseif isWaterToolSelected then
		IsometricScreen:HighlightButton("WaterButton");
	elseif isWallToolSelected then
		IsometricScreen:HighlightButton("WallButton");
	elseif isWallPaperToolSelected then
		IsometricScreen:HighlightButton("WallpaperButton");
	elseif isStairToolSelected then
		IsometricScreen:HighlightButton("StairButton");
	elseif isFireplaceToolSelected then
		IsometricScreen:HighlightButton("FireplaceButton");
	elseif isPlantToolSelected then
		IsometricScreen:HighlightButton("PlantButton");
	elseif isFloorToolSelected then
		IsometricScreen:HighlightButton("FloorButton");
	elseif isDoorToolSelected then
		IsometricScreen:HighlightButton("DoorButton");
	elseif isWindowToolSelected then
		IsometricScreen:HighlightButton("WindowButton");
	elseif isRoofToolSelected then
		IsometricScreen:HighlightButton("RoofButton");
	elseif isHandToolSelected then
		IsometricScreen:HighlightButton("HandButton");
	end
end

function DestroyBuildMode()
	currentGraphicalMode = -1;
	isBuildMode = false

	IsometricScreen:RemoveElement("BGBuyBuild");

	IsometricScreen:RemoveElement("TerrainButton");
	IsometricScreen:RemoveElement("WaterButton");
	IsometricScreen:RemoveElement("WallButton");
	IsometricScreen:RemoveElement("WallpaperButton");
	IsometricScreen:RemoveElement("StairButton");
	IsometricScreen:RemoveElement("FireplaceButton");

	IsometricScreen:RemoveElement("PlantButton");
	IsometricScreen:RemoveElement("FloorButton");
	IsometricScreen:RemoveElement("DoorButton");
	IsometricScreen:RemoveElement("WindowButton");
	IsometricScreen:RemoveElement("RoofButton");
	IsometricScreen:RemoveElement("HandButton");

	--IsometricScreen:RemoveElement("PreviousPageButton");
	--IsometricScreen:RemoveElement("NextPageButton");

	isTerrainToolSelected = false;
	isWaterToolSelected = false;
	isWallToolSelected = false;
	isWallPaperToolSelected = false;
	isStairToolSelected = false;
	isFireplaceToolSelected = false;
	isPlantToolSelected = false
	isFloorToolSelected = false;
	isDoorToolSelected = false;
	isWindowToolSelected = false;
	isRoofToolSelected = false;
	isHandToolSelected = false;

	IsometricScreen:RemoveAllCatalogs();
end

isBuildMode = false
isBuyMode = false
isLiveMode = false

function ButtonHandler(Button)
	if Button.StrID == "ExitButton" then
		LuaFunctions.ApplicationExit(0)
	elseif Button.StrID == "BuildModeButton" then
		if (isBuildMode == false) then
			PrepareBuildMode();
		end
		isBuildMode = true;
		isBuyMode = false;
		isLiveMode = false;
		IsometricScreen:HighlightButton("BuildModeButton");
	elseif Button.StrID == "BuyModeButton" then
		if isBuildMode then
			DestroyBuildMode();
		end
		isBuildMode = false;
		isBuyMode = true;
		isLiveMode = false;
	elseif Button.StrID == "LiveModeButton" then
		if isBuildMode then
			DestroyBuildMode();
		end
		isBuildMode = false;
		isBuyMode = false;
		isLiveMode = true;
	end
	if isBuildMode then
		ButtonHandlerBuild(Button)
	end

end

function Update()
	if IsometricScreen.MouseX >= 800 then
		IsometricScreen:MoveLeft()
	elseif IsometricScreen.MouseX <= 0 then
		IsometricScreen:MoveRight()
	end
	if IsometricScreen.MouseY >= 600 then
		IsometricScreen:MoveDown()
	elseif IsometricScreen.MouseY <= 0 then
		IsometricScreen:MoveUp()
	end
	if isBuildMode then
		UpdateBuildMode();
	end
end
