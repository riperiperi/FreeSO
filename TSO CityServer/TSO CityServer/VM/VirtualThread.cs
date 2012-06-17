using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using SimsLib.IFF;

namespace TSO_CityServer.VM
{
    [Serializable()]
    public enum VThreadState
    {
        Initializing,
        Main,
        Sleeping,
        IdleForInput
    }

    /// <summary>
    /// A virtual thread can run the code for a single object at a time.
    /// </summary>
    [Serializable()]
    public class VirtualThread
    {
        private Stack<IFFDecode> m_ExecutionStack;

        private SimulationObject m_CurrentObject;
        private OBJf m_CurrentObjectFunctions;
        private BHAV m_CurrentFunction;

        private int m_InitFuncID, m_MainFuncID;

        private VThreadState m_CurrentState = VThreadState.Initializing;
        private VThreadState m_LastState = VThreadState.Initializing;

        /// <summary>
        /// The current state of this virtual thread.
        /// </summary>
        public VThreadState CurrentState
        {
            get { return m_CurrentState; }
        }

        /// <summary>
        /// The last state of this virtual thread.
        /// </summary>
        public VThreadState LastState
        {
            get { return m_LastState; }
        }

        /// <summary>
        /// The current object run by this thread.
        /// </summary>
        public SimulationObject SimObject
        {
            get { return m_CurrentObject; }
        }

        /// <summary>
        /// Has the VM queued this thread in an updatequeue to be sent to the server?
        /// This only happens whenever this thread's state has changed.
        /// </summary>
        public bool HasBeenChecked = false;

        //How many ticks to sleep for.
        //Should be decreased every tick when sleeping.
        private int m_SleepCounter = 0;

        public VirtualThread(SimulationObject ObjectToRun)
        {
            m_CurrentObject = ObjectToRun;

            RunInitFunction();
        }

        /// <summary>
        /// Finds and runs the init() function in this object's container (IFF)
        /// if the current object has one, or skips straight to the main function.
        /// </summary>
        private void RunInitFunction()
        {
            m_ExecutionStack = new Stack<IFFDecode>(m_CurrentObject.Master.InitialStackSize);

            //Initialize the stack-size from the OBJD and check to see if the IFF has
            //an OBJf with the same ID as the OBJD.
            foreach (OBJf ObjFunctions in m_CurrentObject.ObjectContainer.OBJfs)
            {
                if (ObjFunctions.ID == m_CurrentObject.Master.ChunkID)
                    m_CurrentObjectFunctions = ObjFunctions;
            }

            if (m_CurrentObjectFunctions == null)
            {
                //OBJD doesn't link to an init function.
                m_CurrentState = VThreadState.Main;
                m_MainFuncID = m_CurrentObject.Master.MainFuncID;

                foreach (BHAV Function in m_CurrentObject.ObjectContainer.BHAVs)
                {
                    if (Function.ChunkID == m_CurrentObject.Master.MainFuncID)
                        m_CurrentFunction = Function;
                }

                if (m_CurrentObject.Master.InitialStackSize <= m_CurrentFunction.NumInstructions)
                {
                    for (int i = m_CurrentObject.Master.InitialStackSize; i > 0; i--)
                        m_ExecutionStack.Push(new IFFDecode(m_CurrentFunction.Instructions[i]));
                }
                else
                {
                    for (int i = m_CurrentFunction.NumInstructions; i > 0; i--)
                        m_ExecutionStack.Push(new IFFDecode(m_CurrentFunction.Instructions[i]));
                }
            }
            else //An associated OBJF was found, which links to an init() function!
            {
                int GuardFuncID = m_CurrentObjectFunctions.FunctionIDs[0].GuardFuncID;
                m_InitFuncID = m_CurrentObjectFunctions.FunctionIDs[0].FunctionID;
                m_MainFuncID = m_CurrentObjectFunctions.FunctionIDs[1].FunctionID;

                foreach (BHAV Function in m_CurrentObject.ObjectContainer.BHAVs)
                {
                    if (Function.ChunkID == GuardFuncID)
                    {
                        m_CurrentFunction = Function;
                        break;
                    }
                }

                if (m_CurrentObject.Master.InitialStackSize <= m_CurrentFunction.NumInstructions)
                {
                    for (int i = m_CurrentObject.Master.InitialStackSize; i > 0; i--)
                        m_ExecutionStack.Push(new IFFDecode(m_CurrentFunction.Instructions[i]));
                }
                else
                {
                    for(int i = m_CurrentFunction.NumInstructions; i > 0; i--)
                        m_ExecutionStack.Push(new IFFDecode(m_CurrentFunction.Instructions[i]));
                }
            }
        }

        /// <summary>
        /// Ticks the current thread by popping one instruction off the execution stack.
        /// </summary>
        public void Tick()
        {
            if (m_CurrentState != VThreadState.Sleeping && m_CurrentState != VThreadState.IdleForInput)
            {
                IFFDecode P = m_ExecutionStack.Pop();
                int Op = P.Operand(0); //Operation code.
                int Param1, Param2 = 0;

                switch (Op)
                {
                    case 0: //Sleep
                        m_SleepCounter = P.Operand(4);
                        m_LastState = m_CurrentState;
                        m_CurrentState = VThreadState.Sleeping;
                        HasBeenChecked = false;
                        break;
                    case 17: //Idle for input
                        Param1 = P.Operand(4);
                        Param2 = P.Operand(6);

                        if (Param2 != 0)
                        {
                            m_LastState = m_CurrentState;
                            m_CurrentState = VThreadState.IdleForInput;
                            HasBeenChecked = false;
                        }
                        else
                        {
                            if (m_CurrentState != VThreadState.Sleeping)
                            {
                                m_SleepCounter = Param1;
                                m_LastState = m_CurrentState;
                                m_CurrentState = VThreadState.Sleeping;
                                HasBeenChecked = false;
                            }
                            else
                                m_SleepCounter += Param1;
                        }
                        break;
                }
            }
            else
            {
                if (m_SleepCounter != 0)
                    m_SleepCounter--;
                else
                    m_CurrentState = m_LastState;
            }
        }

        public void GetObjectData(SerializationInfo Info, StreamingContext Context)
        {
            Info.AddValue("State", m_CurrentState);
        }
    }
}
