namespace FSO.UpdateWorker
{
    internal class UpdateWorkerConfig
    {
        public string authorName { get; set; } = "riperiperi";
        public string repoName { get; set; } = "FreeSO";
        public string remeshAuthorName { get; set; } = "riperiperi";
        public string remeshRepoName { get; set; } = "FSO.Remeshes";
        public string targetPath { get; set; } = "update.json";
        public string[] remeshChannels { get; set; } = ["prod"];
        public string autoRemeshChannel { get; set; } = "prod";
        public string? githubToken { get; set; }
        public bool clearCache { get; set; }
    }
}
