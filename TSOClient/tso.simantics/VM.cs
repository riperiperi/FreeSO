using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.files.formats.iff.chunks;
using tso.simantics.engine;
using Microsoft.Xna.Framework;
using tso.content;

namespace tso.simantics
{
    public class VM
    {
        private const long TickInterval = 33 * TimeSpan.TicksPerMillisecond;

        public VMContext Context { get; internal set; }
        public List<VMEntity> Entities = new List<VMEntity>();
        public short[] GlobalState;

        private object ThreadLock;
        //This is a hash set to avoid duplicates which would cause objects to get multiple ticks per VM tick **/
        private HashSet<VMThread> ActiveThreads = new HashSet<VMThread>();
        private HashSet<VMThread> IdleThreads = new HashSet<VMThread>();
        private List<VMStateChangeEvent> ThreadEvents = new List<VMStateChangeEvent>();

        private Dictionary<short, VMEntity> ObjectsById = new Dictionary<short, VMEntity>();
        //This will need to be an int or long when a server is introduced **/
        private short ObjectId = 1;

        public VM(VMContext context){
            context.VM = this;
            ThreadLock = this;
            this.Context = context;
        }


        public VMEntity GetObjectById(short id){
            if (ObjectsById.ContainsKey(id)){
                return ObjectsById[id];
            }
            return null;
        }

        public void Init(){
            Context.Globals = Content.Get().WorldObjectGlobals.Get("global.iff");
            GlobalState = new short[33];
        }

        public void ThreadIdle(VMThread thread){
            ThreadEvents.Add(new VMStateChangeEvent {
                NewState = VMThreadState.Idle,
                Thread = thread
            });
        }

        public void ThreadActive(VMThread thread){
            ThreadEvents.Add(new VMStateChangeEvent
            {
                NewState = VMThreadState.Active,
                Thread = thread
            });
        }

        private long LastTick = 0;
        public void Update(GameTime time){
            if (LastTick == 0 || (time.TotalRealTime.Ticks - LastTick) >= TickInterval){
                Tick(time);
            }
        }
        private void Tick(GameTime time)
        {
            lock (ThreadLock){

                foreach (var evt in ThreadEvents){
                    switch (evt.NewState){
                        case VMThreadState.Idle:
                            evt.Thread.State = VMThreadState.Idle;
                            IdleThreads.Add(evt.Thread);
                            ActiveThreads.Remove(evt.Thread);
                            break;
                        case VMThreadState.Active:
                            evt.Thread.State = VMThreadState.Active;
                            IdleThreads.Remove(evt.Thread);
                            ActiveThreads.Add(evt.Thread);
                            break;
                    }
                }
                ThreadEvents.Clear();

                LastTick = time.TotalRealTime.Ticks;
                foreach (var thread in ActiveThreads){
                    thread.Tick();
                }
            }
        }

        public void AddEntity(VMEntity entity){
            this.Entities.Add(entity);
            entity.Init(Context);
            entity.ObjectID = ObjectId++;
            ObjectsById.Add(entity.ObjectID, entity);
        }

        private static Dictionary<BHAV, VMRoutine> _Assembled = new Dictionary<BHAV, VMRoutine>();
        public VMRoutine Assemble(BHAV bhav){
            lock (_Assembled)
            {
                if (_Assembled.ContainsKey(bhav))
                {
                    return _Assembled[bhav];
                }
                var routine = VMTranslator.Assemble(this, bhav);
                _Assembled.Add(bhav, routine);
                return routine;
            }
        }
    }

    public class VMStateChangeEvent
    {
        public VMThread Thread;
        public VMThreadState NewState;
    }
    //VMThreadState
}
