namespace FSO.Server.Common
{
    public class ApiAbstract
    {
        public event APIRequestShutdownDelegate OnRequestShutdown;
        public event APIBroadcastMessageDelegate OnBroadcastMessage;
        public event APIRequestUserDisconnectDelegate OnRequestUserDisconnect;
        public event APIRequestMailNotifyDelegate OnRequestMailNotify;
        public event APIInvalidateAvatarDelegate OnInvalidateAvatar;

        public delegate void APIRequestShutdownDelegate(uint time, ShutdownType type);
        public delegate void APIBroadcastMessageDelegate(string sender, string title, string message);
        public delegate void APIRequestUserDisconnectDelegate(uint user_id);
        public delegate void APIRequestMailNotifyDelegate(int message_id, string subject, string body, uint target_id);
        public delegate void APIInvalidateAvatarDelegate(uint avatar_id);

        public void RequestShutdown(uint time, ShutdownType type)
        {
            OnRequestShutdown?.Invoke(time, type);
        }

        /// <summary>
        /// Asks the server to disconnect a user.
        /// </summary>
        /// <param name="user_id"></param>
        public void RequestUserDisconnect(uint user_id)
        {
            OnRequestUserDisconnect?.Invoke(user_id);
        }

        /// <summary>
        /// Asks the server to notify the client about the new message.
        /// </summary>
        /// <param name="message_id"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="target_id"></param>
        public void RequestMailNotify(int message_id, string subject, string body, uint target_id)
        {
            OnRequestMailNotify(message_id, subject, body, target_id);
        }

        public void BroadcastMessage(string sender, string title, string message)
        {
            OnBroadcastMessage?.Invoke(sender, title, message);
        }

        /// <summary>
        /// Asks the city to invalidate its in-memory cached Avatar model so the next
        /// client poll re-loads from the DB. Called by API endpoints (the portal flow)
        /// when an out-of-band write changes the avatar's persisted state and the
        /// client UI needs to see the new value within a poll cycle.
        /// </summary>
        public void InvalidateAvatar(uint avatar_id)
        {
            OnInvalidateAvatar?.Invoke(avatar_id);
        }
    }
}
