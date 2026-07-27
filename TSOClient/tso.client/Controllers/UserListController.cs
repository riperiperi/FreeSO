using FSO.Client.Regulators;
using FSO.Client.UI.Model;
using FSO.Common.Utils;
using FSO.Server.Clients;
using FSO.Server.Protocol.Electron.Packets;
using FSO.UI.Model;
using System;

namespace FSO.Client.Controllers
{
    internal class UserListController : IAriesMessageSubscriber, IDisposable
    {
        private CityConnectionRegulator City;

        public ArchiveClientList UserList;

        public Action<bool> FlashCallback;

        public UserListController(CityConnectionRegulator city)
        {
            City = city;

            UserList = City.UserList;

            City.Client.AddSubscriber(this);
        }

        private void SignalNewVerification()
        {
            FlashCallback?.Invoke(true);
            HIT.HITVM.Get().PlaySoundEvent(UISounds.LetterQueueFull);
        }

        private void SignalNoVerifications()
        {
            FlashCallback?.Invoke(false);
        }

        private void UpdateUserList(ArchiveClientList newList)
        {
            if (UserList != null && newList != null)
            {
                // Try determine the difference. If there are new verifications pending, play a sound and notify the user list button.

                if (newList.Pending.Length == 0)
                {
                    SignalNoVerifications();
                }
                else if (newList.Pending.Length != UserList.Pending.Length)
                {
                    SignalNewVerification();
                }
                else
                {
                    // Are any new pending verifications not in the last?

                    foreach (var newEntry in newList.Pending)
                    {
                        if (Array.FindIndex(UserList.Pending, (oldEntry) => oldEntry.UserId == newEntry.UserId) == -1)
                        {
                            SignalNewVerification();
                            break;
                        }
                    }
                }

                DiscordRpcEngine.SetArchivePlayers(UserList.Clients.Length);
            }

            UserList = newList;
        }

        public void MessageReceived(AriesClient client, object message)
        {
            if (message is ArchiveClientList list)
            {
                GameThread.InUpdate(() =>
                {
                    UpdateUserList(list);
                });
            }
        }

        public void Dispose()
        {
            City.Client.RemoveSubscriber(this);
        }
    }
}
