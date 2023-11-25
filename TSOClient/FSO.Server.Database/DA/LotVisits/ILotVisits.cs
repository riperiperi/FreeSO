using System;
using System.Collections.Generic;

namespace FSO.Server.Database.DA.LotVisitors
{
    public interface ILotVisits
    {
        /// <summary>
        /// Records a new visit to a lot. A visit id is returned which can be used to record
        /// when the visit ends
        /// </summary>
        /// <param name="avatar_id"></param>
        /// <param name="visitor_type"></param>
        /// <param name="lot_id"></param>
        /// <returns></returns>
        int? Visit(uint avatar_id, DbLotVisitorType visitor_type, int lot_id);

        /// <summary>
        /// Records that a visit has ended
        /// </summary>
        /// <param name="visit_id"></param>
        void Leave(int visit_id);

        /// <summary>
        /// Updates the timestamp on active visits. This helps detect the difference between
        /// a long visit and an error where the visit did not get closed.
        /// 
        /// This also lets us calculate Top 100 without downtime as we can be inclusive
        /// of active tickets
        /// </summary>
        /// <param name="visit_ids"></param>
        void Renew(IEnumerable<int> visit_ids);

        /// <summary>
        /// Purge data older than the date given
        /// </summary>
        /// <param name="days"></param>
        void PurgeByDate(DateTime date);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        IEnumerable<DbLotVisit> StreamBetween(int shard_id, DateTime start, DateTime end);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        IEnumerable<DbLotVisitNhood> StreamBetweenPlusNhood(int shard_id, DateTime start, DateTime end);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        IEnumerable<DbLotVisitNhood> StreamBetweenOneNhood(uint nhood_id, DateTime start, DateTime end);


    }
}
