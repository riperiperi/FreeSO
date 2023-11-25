namespace FSO.Server.Common.Config
{
    public class AWSConfig
    {
        public string Region { get; set; } = "eu-west-2";
        public string Bucket { get; set; } = "fso-updates";
        public string AccessKeyID { get; set; }
        public string SecretAccessKey { get; set; }
    }
}
