using System.Threading.Tasks;

namespace FSO.Server.Api.Core.Services
{
    public interface IUpdateUploader
    {
        Task<string> UploadFile(string destPath, string fileName, string groupName);
    }
}
