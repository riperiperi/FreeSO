using FSO.Common.Domain.Realestate;
using FSO.Content.Model;
using FSO.Server.Protocol.Electron.Model.CityEditCommands;
using FSO.Server.Protocol.Electron.Packets;
using Microsoft.Xna.Framework;

namespace FSO.Common.Domain.RealestateDomain
{
    public interface IShardRealestateDomain
    {
        bool Dynamic { get; }
        CityUndoStack UndoStack { get; }
        event Action<Rectangle> OnMapChange;
        int GetPurchasePrice(ushort x, ushort y);
        bool IsOpenable(ushort x, ushort y);
        bool IsPurchasable(ushort x, ushort y);
        int GetSlope(ushort x, ushort y);
        CityMap GetMap();
        int AppendCommand(CityEditBase command);
        void SetMyTempCommand(CityEditBase command);
        bool HandleUserCommand(CityUpdateCommand command);
        void TrackUndo(uint avatarId);
    }
}