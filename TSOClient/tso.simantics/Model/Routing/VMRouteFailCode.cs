namespace FSO.SimAntics.Model.Routing
{
    public enum VMRouteFailCode : short
    {
        Success = 0,
        Unknown = 1,
        NoRoomRoute = 2,
        NoPath = 3, //pathfind failed
        Interrupted = 4,
        CantSit = 5,
        CantStand = 6, //with blocking object
        NoValidGoals = 7,
        DestTileOccupied = 8,
        DestChairOccupied = 9, //with blocking object
        NoChair = 10,
        WallInWay = 11, 
        AltsDontMatch = 12,
        DestTileOccupiedPerson = 13 //with blocking object
    }
}
