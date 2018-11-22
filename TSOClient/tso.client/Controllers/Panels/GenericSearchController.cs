using FSO.Common.DatabaseService;
using FSO.Common.DatabaseService.Model;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers.Panels
{
    public class GenericSearchController
    {
        private IClientDataService DataService;
        private IDatabaseService DatabaseService;

        public GenericSearchController(IClientDataService dataService, IDatabaseService database)
        {
            this.DataService = dataService;
            this.DatabaseService = database;
        }

        public void Search(string query, bool exact, Action<List<GizmoAvatarSearchResult>> callback)
        {
            DatabaseService.Search(new SearchRequest { Query = query, Type = SearchType.SIMS }, exact)
                .ContinueWith(x =>
                {
                    object[] ids = x.Result.Items.Select(y => (object)y.EntityId).ToArray();
                    var results = x.Result.Items.Select(q =>
                    {
                        return new GizmoAvatarSearchResult() { Result = q };
                    }).ToList();

                    if (ids.Length > 0)
                    {
                        var avatars = DataService.GetMany<Avatar>(ids).Result;
                        foreach (var item in avatars)
                        {
                            results.First(f => f.Result.EntityId == item.Avatar_Id).Avatar = item;
                        }
                    }

                    callback(results);
                });
        }
    }
}
