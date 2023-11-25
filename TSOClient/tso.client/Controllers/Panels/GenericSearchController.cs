using FSO.Common.DatabaseService;
using FSO.Common.DatabaseService.Model;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

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
                    GameThread.InUpdate(() =>
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
                });
        }

        public void SearchLots(string query, bool exact, Action<List<GizmoLotSearchResult>> callback)
        {
            DatabaseService.Search(new SearchRequest { Query = query, Type = SearchType.LOTS }, exact)
                .ContinueWith(x =>
                {
                    GameThread.InUpdate(() =>
                    {
                        object[] ids = x.Result.Items.Select(y => (object)y.EntityId).ToArray();
                        var results = x.Result.Items.Select(q =>
                        {
                            return new GizmoLotSearchResult() { Result = q };
                        }).ToList();

                        if (ids.Length > 0)
                        {
                            var lots = DataService.GetMany<Lot>(ids).Result;
                            foreach (var item in lots)
                            {
                                results.First(f => f.Result.EntityId == item.Id).Lot = item;
                            }
                        }

                        callback(results);
                    });
                });
        }
    }
}
