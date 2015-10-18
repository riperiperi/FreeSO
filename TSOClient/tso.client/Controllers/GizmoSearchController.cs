using FSO.Client.UI.Panels;
using FSO.Common.DatabaseService;
using FSO.Common.DatabaseService.Model;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers
{
    public class GizmoSearchController
    {
        private UIGizmoSearch View;
        private IClientDataService DataService;
        private IDatabaseService DatabaseService;

        public GizmoSearchController(UIGizmoSearch view, IClientDataService dataService, IDatabaseService database)
        {
            this.View = view;
            this.DataService = dataService;
            this.DatabaseService = database;
        }

        public void Search(string query, SearchType type, bool exact)
        {
            DatabaseService.Search(new SearchRequest { Query = query, Type = type }, exact)
                .ContinueWith(x =>
                {
                    object[] ids = x.Result.Items.Select(y => (object)y.EntityId).ToArray();
                    if (type == SearchType.SIMS)
                    {
                        var results = x.Result.Items.Select(q =>
                        {
                            return new GizmoAvatarSearchResult() { Result = q };
                        }).ToList();

                        if (ids.Length > 0)
                        {
                            var avatars = DataService.GetMany<Avatar>(ids).Result;
                            foreach(var item in avatars){
                                results.First(f => f.Result.EntityId == item.Avatar_Id).Avatar = item;
                            }
                        }

                        View.SetResults(results);
                    }
                });
        }
    }


    public class GizmoAvatarSearchResult
    {
        public Avatar Avatar;
        public SearchResponseItem Result;
    }
}
