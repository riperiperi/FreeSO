using FSO.Common.DatabaseService.Model;
using System.Threading.Tasks;

namespace FSO.Common.DatabaseService
{
    public interface IDatabaseService
    {
        bool IsConnected { get; }
        Task<LoadAvatarByIDResponse> LoadAvatarById(LoadAvatarByIDRequest request);
        Task<SearchResponse> Search(SearchRequest request, bool exact);
        Task<GetTop100Response> GetTop100(GetTop100Request request);
    }
}
