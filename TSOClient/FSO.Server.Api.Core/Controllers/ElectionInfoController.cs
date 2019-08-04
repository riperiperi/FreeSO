using FSO.Server.Api.Core.Utils;
using FSO.Server.Common;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using FSO.Server.Database.DA.Avatars;
using FSO.Server.Database.DA.Bulletin;
using FSO.Server.Database.DA.Elections;

namespace FSO.Server.Api.Core.Controllers
{
    [EnableCors]
    [ApiController]
    public class ElectionController : ControllerBase
    {
        [HttpGet]
        [Route("userapi/elections/neighborhood/id/{nhoodid}.json")]
        public IActionResult GetByNhood(int shardid, uint nhoodid)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var Nhood = da.Neighborhoods.Get(nhoodid);
                if (Nhood.election_cycle_id == null)
                {
                    var JSONError = new JSONElectionError();
                    JSONError.Error = "Election Cycle not found";
                    return ApiResponse.Json(HttpStatusCode.NotFound, JSONError);
                }

                var ElectionCycle = da.Elections.GetCycle((uint)Nhood.election_cycle_id);
                
                var ElectionCandidates = new List<DbElectionCandidate>();
                if (ElectionCycle.current_state == Database.DA.Elections.DbElectionCycleState.election)
                {
                    ElectionCandidates = da.Elections.GetCandidates(ElectionCycle.cycle_id, Database.DA.Elections.DbCandidateState.running);
                }
                if (ElectionCycle.current_state == Database.DA.Elections.DbElectionCycleState.ended)
                {
                    ElectionCandidates = da.Elections.GetCandidates(ElectionCycle.cycle_id, Database.DA.Elections.DbCandidateState.won);
                }

                if (ElectionCycle == null)
                {
                    var JSONError = new JSONElectionError();
                    JSONError.Error = "Election Cycle not found";
                    return ApiResponse.Json(HttpStatusCode.NotFound, JSONError);
                }

                List<JSONCandidates> CandidatesJSON = new List<JSONCandidates>();
                foreach (var Candidate in ElectionCandidates)
                {
                    CandidatesJSON.Add(new JSONCandidates
                    {
                       Candidate_Avatar_ID = Candidate.candidate_avatar_id,
                       Comment = Candidate.comment,
                       State = Candidate.state
                    });

                }
                var ElectionJSON = new JSONElections();
                ElectionJSON.Candidates = CandidatesJSON;
                ElectionJSON.Current_State = ElectionCycle.current_state;
                ElectionJSON.Neighborhood_ID = Nhood.neighborhood_id;
                ElectionJSON.Start_Date = ElectionCycle.start_date;
                ElectionJSON.End_Date = ElectionCycle.end_date;
                return ApiResponse.Json(HttpStatusCode.OK, ElectionJSON);
            }
        }
    }
    public class JSONElectionError
    {
        public string Error { get; set; }
    }
    public class JSONElections
    {
        public DbElectionCycleState Current_State { get; set; }
        public int Neighborhood_ID { get; set; }
        public uint Start_Date { get; set; }
        public uint End_Date { get; set; }
        public List<JSONCandidates> Candidates { get; set; }
    }
    public class JSONCandidates
    {
        public uint Candidate_Avatar_ID { get; set; }
        public string Comment { get; set; }
        public DbCandidateState State { get; set; }
    }
}
