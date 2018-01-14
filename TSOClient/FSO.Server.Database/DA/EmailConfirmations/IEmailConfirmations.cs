namespace FSO.Server.Database.DA.EmailConfirmation
{
    public interface IEmailConfirmations
    {
        void Create(EmailConfirmation confirm);
        EmailConfirmation GetByEmail(string token);
        EmailConfirmation GetByToken(string token);
        void Remove(string token);
    }
}