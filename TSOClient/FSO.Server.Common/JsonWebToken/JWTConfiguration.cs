namespace FSO.Server.Servers.Api.JsonWebToken
{
    public class JWTConfiguration
    {
        public byte[] Key;
        public int TokenDuration = 3600;
    }
}
