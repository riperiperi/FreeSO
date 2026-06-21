namespace FSO.UpdateWorker
{
    internal class UpdateWorkerConfig
    {
        public string authorName { get; set; } = "riperiperi";
        public string repoName { get; set; } = "FreeSO";
        public string targetPath { get; set; } = "update.json";
        public string? githubToken { get; set; }
    }
}
