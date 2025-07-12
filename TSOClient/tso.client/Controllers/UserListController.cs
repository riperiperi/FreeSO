using FSO.Client.Regulators;
using FSO.Server.Protocol.Electron.Packets;

namespace FSO.Client.Controllers
{
    internal class UserListController
    {
        private CityConnectionRegulator City;

        public ArchiveClientList UserList => City.UserList;

        public UserListController(CityConnectionRegulator city)
        {
            City = city;
        }
    }
}
