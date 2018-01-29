namespace FSO.Server.Database.DA.EmailConfirmation
{
    public interface IEmailConfirmations
    {
        string Create(EmailConfirmation confirm);
        EmailConfirmation GetByEmail(string email, ConfirmationType type);
        EmailConfirmation GetByToken(string token);
        void Remove(string token);
    }
}