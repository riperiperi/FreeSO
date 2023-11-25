using FSO.Client.UI.Panels;
using FSO.Common.DatabaseService;
using FSO.Common.DatabaseService.Model;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Utils;
using System.Linq;

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
                    GameThread.InUpdate(() =>
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
                                foreach (var item in avatars)
                                {
                                    results.First(f => f.Result.EntityId == item.Avatar_Id).Avatar = item;
                                }
                            }

                            View.SetResults(results);
                        }
                        else if (type == SearchType.LOTS)
                        {
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

                            View.SetResults(results);
                        }
                        else if (type == SearchType.NHOOD)
                        {
                            var results = x.Result.Items.Select(q =>
                            {
                                return new GizmoNhoodSearchResult() { Result = q };
                            }).ToList();

                            if (ids.Length > 0)
                            {
                                var lots = DataService.GetMany<Neighborhood>(ids).Result;
                                foreach (var item in lots)
                                {
                                    results.First(f => f.Result.EntityId == item.Id).Lot = item;
                                }
                            }

                            View.SetResults(results);
                        }
                    });
                });
        }
    }


    public class GizmoAvatarSearchResult
    {
        public Avatar Avatar;
        public SearchResponseItem Result;

        public bool IsOnline
        {
            get
            {
                if(Avatar != null)
                {
                    return Avatar.Avatar_IsOnline;
                }
                return false;
            }
        }

        public bool IsOffline
        {
            get
            {
                return !IsOnline;
            }
        }
    }

    public class GizmoLotSearchResult
    {
        public Lot Lot;
        public SearchResponseItem Result;

        public bool IsOnline
        {
            get
            {
                if (Lot != null)
                {
                    return Lot.Lot_IsOnline;
                }
                return false;
            }
        }

        public bool IsOffline
        {
            get
            {
                return !IsOnline;
            }
        }
    }

    public class GizmoNhoodSearchResult
    {
        public Neighborhood Lot;
        public SearchResponseItem Result;

        public bool IsOnline
        {
            get
            {
                return true;
            }
        }

        public bool IsOffline
        {
            get
            {
                return !IsOnline;
            }
        }
    }
}
