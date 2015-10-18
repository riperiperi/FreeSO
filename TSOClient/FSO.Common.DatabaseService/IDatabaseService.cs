using FSO.Common.DatabaseService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DatabaseService
{
    public interface IDatabaseService
    {
        Task<LoadAvatarByIDResponse> LoadAvatarById(LoadAvatarByIDRequest request);
        Task<SearchResponse> Search(SearchRequest request, bool exact);
    }
}
