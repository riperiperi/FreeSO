using FSO.Server.Framework.Gluon;
using FSO.Server.Framework.Voltron;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Framework.Aries
{
    public class Sessions : ISessions
    {
        private HashSet<IAriesSession> _Sessions;
        private HashSet<IGluonSession> _GluonSessions;
        private ISessionProxy _All;

        private Dictionary<object, SessionGroup> _Groups = new Dictionary<object, SessionGroup>();

        public Sessions(){
            _Sessions = new HashSet<IAriesSession>();
            _GluonSessions = new HashSet<IGluonSession>();
            _All = new EnumerableSessionProxy(_Sessions);
        }

        public IVoltronSession GetByAvatarId(uint id)
        {
            return (IVoltronSession)_Sessions.FirstOrDefault(x => {
                return x is IVoltronSession && ((IVoltronSession)x).AvatarId == id;
            });
        }

        public T UpgradeSession<T>(IAriesSession session) where T : AriesSession
        {
            var newSession = ((AriesSession)session).UpgradeSession<T>();
            Remove(session);
            Add(newSession);
            return newSession;
        }

        public ISessionGroup GetOrCreateGroup(object id){
            lock (_Groups)
            {
                if (_Groups.ContainsKey(id))
                {
                    return _Groups[id];
                }

                var newGroup = new SessionGroup();
                _Groups.Add(id, newGroup);
                return newGroup;
            }
        }

        public ISessionProxy All(){
            return _All;
        }

        public IEnumerable<IGluonSession> GluonSessions
        {
            get { return _GluonSessions; }
        }

        public IEnumerable<IAriesSession> RawSessions
        {
            get { return _Sessions; }
        }

        public void Add(IAriesSession session){
            if (session is IGluonSession)
            {
                _GluonSessions.Add((IGluonSession)session);
            }
            else
            {
                _Sessions.Add(session);
            }
        }

        public void Remove(IAriesSession session){
            if(session is IGluonSession)
            {
                _GluonSessions.Remove((IGluonSession)session);
            }
            else
            {
                _Sessions.Remove(session);
            }
        }
    }

    public class SessionGroup : EnumerableSessionProxy, ISessionGroup {
        private HashSet<IAriesSession> _Sessions;
        private Func<bool, IAriesSession> Criteria;

        public SessionGroup() : base(){
            _Sessions = new HashSet<IAriesSession>();
            SetSource(_Sessions);
        }
        
        public void Enroll(IAriesSession session){
            _Sessions.Add(session);
        }

        public void UnEnroll(IAriesSession session){
            _Sessions.Remove(session);
        }
    }

    public class EnumerableSessionProxy : ISessionProxy
    {
        public IEnumerable<IAriesSession> Sessions;

        public EnumerableSessionProxy(IEnumerable<IAriesSession> sessions){
            this.Sessions = sessions;
        }

        public EnumerableSessionProxy()
        {
        }

        public void SetSource(IEnumerable<IAriesSession> source)
        {
            this.Sessions = source;
        }

        public void Broadcast(params object[] messages){
            //TODO: Make this more efficient
            foreach(var session in Sessions){
                session.Write(messages);
            }
        }
    }
}
