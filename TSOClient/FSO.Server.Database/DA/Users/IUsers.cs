using FSO.Server.Database.DA.Utils;
using System.Collections.Generic;

namespace FSO.Server.Database.DA.Users
{
    public interface IUsers
    {
        User GetById(uint id);
        List<User> GetByRegisterIP(string ip);
        List<User> GetByLastIP(string ip);
        void UpdateConnectIP(uint id, string ip);
        void UpdateBanned(uint id, bool banned);
        void UpdateClientID(uint id, string cid);
        void UpdateVerified(uint id, bool verified);
        User GetByUsername(string username);
        UserAuthenticate GetAuthenticationSettings(uint userId);
        PagedList<User> All(int offset = 0, int limit = 20, string orderBy = "register_date");
        List<UserSummary> AllSummaries();
        uint Create(User user);
        bool Delete(uint id);
        void CreateAuth(UserAuthenticate auth);
        User GetByEmail(string email);
        void UpdateAuth(UserAuthenticate auth);
        void UpdateLastLogin(uint id, uint last_login);
        void UpdatePermissions(uint id, bool is_moderator, bool is_admin);

        DbAuthAttempt GetRemainingAuth(uint user_id, string ip);
        int FailedConsecutive(uint user_id, string ip);
        int FailedAuth(uint attempt_id, uint delay, int failLimit);
        void NewFailedAuth(uint user_id, string ip, uint delay);
        void SuccessfulAuth(uint user_id, string ip);
    }
}
