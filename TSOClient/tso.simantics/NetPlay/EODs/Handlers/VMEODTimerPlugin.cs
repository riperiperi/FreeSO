using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.SimAntics.NetPlay.EODs.Model;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    class VMEODTimerPlugin : VMEODHandler
    {
        private VMEODClient PlayerClient;

        private short CurrentDisplayedSeconds;
        private short CurrentDisplayedMinutes;
        private bool IsRunning;
        private bool UpdatedAfterStop = true;
        private VMEODTimerPluginStates State;

        private int Tock;

        public VMEODTimerPlugin(VMEODServer server) : base(server)
        {
            BinaryHandlers["Timer_State_Change"] = StateChangeHandler;
            BinaryHandlers["Timer_IsRunning_Change"] = IsRunningChangeHandler;
            BinaryHandlers["Timer_Set"] = SetTimerHandler;
            PlaintextHandlers["Timer_Close"] = CloseHandler;
        }

        public override void Tick()
        {
            base.Tick();

            if (IsRunning)
            {
                Tock = 0;

                if (State.Equals(VMEODTimerPluginStates.Countdown))
                {
                    // get the registers
                    var args = PlayerClient.Invoker.Thread.TempRegisters;
                    if ((args[2] == 0) && (args[3] == 0))
                    {
                        IsRunning = false;
                        UpdatedAfterStop = true;
                        // get the updated minutes and seconds
                        CurrentDisplayedMinutes = args[2];
                        CurrentDisplayedSeconds = args[3];
                        PlayerClient.Send("Timer_Off", new byte[] { (byte)State });
                        PlayerClient.Send("Timer_Update", CurrentDisplayedMinutes + ":" + CurrentDisplayedSeconds);
                    }
                }
            }
            else if (!UpdatedAfterStop)
            {
                if (Tock == 0)
                {
                    PlayerClient.SendOBJEvent(new VMEODEvent((short)VMEODTimerEvents.Update));
                }
                Tock++;

                if (Tock == 5)
                {
                    UpdatedAfterStop = true;

                    // get the registers
                    var args = PlayerClient.Invoker.Thread.TempRegisters;

                    // get the final minutes and seconds
                    CurrentDisplayedMinutes = args[2];
                    CurrentDisplayedSeconds = args[3];

                    PlayerClient.Send("Timer_Update", CurrentDisplayedMinutes + ":" + CurrentDisplayedSeconds);
                }
            }
        }

        public override void OnConnection(VMEODClient client)
        {
            base.OnConnection(client);

            PlayerClient = client;

            // get the registers
            var args = client.Invoker.Thread.TempRegisters;

            // get the initial minutes and seconds
            CurrentDisplayedMinutes = args[2];
            CurrentDisplayedSeconds = args[3];

            // get the current running states
            IsRunning = (args[0] == 1) ? true : false;
            if (Enum.IsDefined(typeof(VMEODTimerPluginStates), args[1]))
                State = (VMEODTimerPluginStates)Enum.ToObject(typeof(VMEODTimerPluginStates), args[1]);
            else
                State = VMEODTimerPluginStates.Countdown;

            // show the client: @params - Byte[] { 0 if IsRunning or 1 if !IsRunning, 0 if State = Countdown 1 if State = Stopwatch, Minutes/256, Seconds }
            PlayerClient.Send("Timer_Show", new Byte[] { (byte)args[0], (byte)args[1], (byte)(CurrentDisplayedMinutes), (byte)CurrentDisplayedSeconds } );
        }

        public override void OnDisconnection(VMEODClient client)
        {
            base.OnDisconnection(client);
        }
        
        private void StateChangeHandler(string evt, byte[] newState, VMEODClient client)
        {
            if ((newState == null) || (newState.Length > 1) || (newState[0] > 1))
                return;

            switch (State)
            {
                case VMEODTimerPluginStates.Countdown:
                    {
                        // change to stopwatch
                        if (newState[0] == 1)
                        {
                            PlayerClient.SendOBJEvent(new VMEODEvent((short)VMEODTimerEvents.ToggleStopwatch));
                            State = VMEODTimerPluginStates.Stopwatch;
                        }
                        break;
                    }
                default:
                    {
                        // change to countdown
                        if (newState[0] == 0)
                        {
                            PlayerClient.SendOBJEvent(new VMEODEvent((short)VMEODTimerEvents.ToggleStopwatch));
                            State = VMEODTimerPluginStates.Countdown;
                        }
                        break;
                    }
            }
        }

        private void IsRunningChangeHandler(string evt, byte[] newRunningState, VMEODClient client)
        {
            if ((newRunningState == null) || (newRunningState.Length > 1) || (newRunningState[0] > 1))
                return;

            switch (IsRunning)
            {
                case true:
                    {
                        if (newRunningState[0] == 0)
                        {
                            IsRunning = false;
                            PlayerClient.SendOBJEvent(new VMEODEvent((short)VMEODTimerEvents.Pause));
                        }
                        break;
                    }
                default:
                    {
                        if (newRunningState[0] == 1)
                        {
                            IsRunning = true;
                            PlayerClient.SendOBJEvent(new VMEODEvent((short)VMEODTimerEvents.Start));
                            UpdatedAfterStop = false;
                        }
                        break;
                    }
            }
        }

        private void SetTimerHandler(string evt, byte[] newTime, VMEODClient client)
        {
            if ((newTime == null) || (newTime.Length > 2) || (newTime[0] > 99) || (newTime[1] > 59))
                return;
            
            CurrentDisplayedMinutes = (short)(newTime[0] * 256);
            CurrentDisplayedSeconds = newTime[1];

            PlayerClient.SendOBJEvent(new VMEODEvent((short)VMEODTimerEvents.SetTime, new short[] { (short)(CurrentDisplayedMinutes + CurrentDisplayedSeconds) } ));
        }

        private void CloseHandler(string evt, string msg, VMEODClient client)
        {
            Server.Disconnect(client);
        }
    }

    public enum VMEODTimerPluginStates : short
    {
        Countdown = 0,
        Stopwatch = 1
    }
    public enum VMEODTimerEvents : short
    {
        Update = 0,
        ToggleStopwatch = 1,
        SetTime = 2,
        Start = 3,
        Pause = 4
    }
}
