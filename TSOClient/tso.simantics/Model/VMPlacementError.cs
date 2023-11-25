namespace FSO.SimAntics.Model
{
    public enum VMPlacementError
    {
        Success = -1,

        // NOTE: Each of the below corresponds to a string in cst 137: placement_errors
        // Don't change the names or order of these!

        LocationOutOfBounds = 0,
        //^Location out of bounds^

        LevelOutOfBounds = 1,
        //^Level out of bounds^

        MustBeAtTileCenter = 2,
        //^Must be at center of tile^

        MustBeInCorner = 3,
        //^Must be in corner^

        CantBeInCorner = 4,
        //^Can't be in corner^

        MustBeAgainstWall = 5,
        //^Must be against wall^

        CantBeThroughWall = 6,
        //^Can't intersect wall^

        MustBeOnUpstairsFloorHole = 7,
        //^Must be over hole in floor^

        MustBeOutside = 8,
        //^Must be outside^

        MustBeInside = 9,
        //^Must be inside^

        CantIntersectOtherObjects = 10,
        //^Can't intersect other objects^

        MustBeOnGround = 11,
        //^Must be on its own tile on the ground^

        CantFindSlot = 12,
        //^Can't find a place to put it^

        CantSupportWeight = 13,
        //^Can't support the weight^

        CantSupportSize = 14,
        //^Can't support the size^

        CantPlaceOnTop = 15,
        //^Can't place on top^

        CantPlaceOnSlope = 16,
        //^Can't place on slope^

        CantPlaceInAir = 17,
        //^Must place on second-story floor tile^

        NotAllowedOnFloor = 18,
        //^Must be placed on terrain^

        NotAllowedOnTerrain = 19,
        //^Must place on floor tile^

        CantAfford = 20,
        //^Insufficient funds^

        MustBeAgainstUnusedWall = 21,
        //^Must be against unused wall^

        MustBeOnDiagonal = 22,
        //^Must be on diagonal^

        HeightNotAllowed = 23,
        //^Must place on table or surface^

        InsufficientFunds = 24,
        //^Insufficient Funds^

        SpecialShowMoneyError = 25,
        //^User should never see this message^

        HasWater = 26,
        //^Cannot add more water^

        NoWater = 27,
        //^No water to delete^

        CantPlaceOnWater = 28,
        //^Cannot place over water^

        MustPlaceOnWater = 29,
        //^Must be placed on water^

        MustPlaceOnPool = 30,
        //^Must be placed on pool tile^

        MustBeOnFirstLevel = 31,
        //^Must be on first story^

        // wall-specific
        Floor2NeedsSupport = 32,
        //^Second story wall needs support from first floor^

        MustRemoveObjectsOnWall = 33,
        //^Must remove objects on wall first^

        // wallpaper-specific
        MustBeAgainst2ndFloorWall = 34,
        //^Must be on second floor wall^

        CannotDeleteObject = 35,
        //^Can't delete object in use^

        ObjectNotOwnedByYou = 36,
        //^You do not own that!^

        CounterHeight = 37,
        //^Must be placed on counter^

        CantBePickedup = 38,
        //^Can't pick up^

        InUse = 39,
        //^Can't select object in use^

        CantBePickedupOutOfBounds = 40,
        //^Can't pick up--location out of bounds^

        CantEffectFirstLevelFromSecondLevel = 41,
        //^Can't effect first level from second level^

        CantPlaceInAirOtherLevel = 42,
        //^Must connect to second story^

        NotAllowedOnFloorOtherLevel = 43,
        //^Can't intersect second-story object^

        CannotPlaceComputerOnEndTable = 44,
        //^Can't place on end table^

        CannotDeletePoolWhilePeopleAreInIt = 45,
        //^Can't delete pool with Sims in it^

        MustRemoveLadderOrDivingBoardBeforeDeletingPool = 46,
        //^Must first delete ladder or diving board^

        CannotWallpaperFence = 47,
        //^Can't place wallpaper on fences^

        MustHaveMoreSpaceFromCeiling = 48,
        //^Must have more space from ceiling^

        TooManyObjectsOnTheLot = 49,
        //^Property object limit exceeded^

        CantPlaceOnForSaleObject = 50
        //^Can't place on for-sale object^

    }
}
