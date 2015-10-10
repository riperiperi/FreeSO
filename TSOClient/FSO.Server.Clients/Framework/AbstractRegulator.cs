using FSO.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Server.Clients.Framework
{
    public abstract class AbstractRegulator
    {
        private Dictionary<string, RegulatorState> States = new Dictionary<string, RegulatorState>();
        public RegulatorState CurrentState;

        public event Callback<object> OnError;
        public event Callback<string, object> OnTransition;

        private List<ITransitionValidator> Validators = new List<ITransitionValidator>();
        public string DefaultState;

        private Thread BackgroundThread;
        private EventWaitHandle WaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        private Queue<IRegulatorDigestAction> Actions = new Queue<IRegulatorDigestAction>();

        public AbstractRegulator()
        {
            //A thread is used because of the way the UI engine works
            //E.g. Imagine the following scenario: you pressed login, we triggered a network request 
            //which then shows a error dialog.
            //The button press triggers in the update loop
            //The update loop locks the children array
            //We would end up blocking the UI while we do the network request
            //When we get the error and try to show the dialog, we would still be in the update loop
            //and we would get into deadlock.

            //Keeping network and network reaction logic inside the regulator
            //or regulator callback events forces us out of the update loop
            BackgroundThread = new Thread(new ThreadStart(Digest));
            BackgroundThread.Priority = ThreadPriority.Highest;
            BackgroundThread.Start();
        }

        private void Digest(){
            while (WaitHandle.WaitOne()){
                while(Actions.Count > 0){
                    var item = (IRegulatorDigestAction)Actions.Dequeue();
                    try
                    {
                        item.Handle(this);
                    }catch(Exception ex){
                    }
                }
                WaitHandle.Reset();
            }
        }

        public RegulatorEventHandler OnTransitionTo(string state)
        {
            var evt = new RegulatorEventHandler(this, null);

            this.OnTransition += (string newState, object data) =>
            {
                if (newState == state)
                {
                    evt.Invoke(data);
                }
            };

            return evt;
        }
        
        public void AsyncReset()
        {
            Async(new RegulatorResetAction());
        }

        public void AsyncTransition(string newState)
        {
            AsyncTransition(newState, null);
        }

        public void AsyncTransition(string newState, object data)
        {
            Async(new RegulatorTransitionAction(newState, data));
        }

        protected void ThrowErrorAndReset(object errorMessage)
        {
            ThrowError(errorMessage);
            AsyncReset();
        }

        protected void ThrowError(object errorMessage)
        {
            System.Diagnostics.Debug.WriteLine(errorMessage);
            if (this.OnError != null)
            {
                this.OnError(errorMessage);
            }
        }

        protected void ThrowError(Exception error)
        {
            this.ThrowError((object)error);
        }

        public void AsyncProcessMessage(object message)
        {
            Async(new RegulatorProcessMessageAction(message));
        }

        protected RegulatorState AddState(string name)
        {
            var state = new RegulatorState(name, this);
            States.Add(name, state);
            return state;
        }

        private void Async(IRegulatorDigestAction action){
            Actions.Enqueue(action);
            WaitHandle.Set();
        }

        protected abstract void OnBeforeTransition(RegulatorState oldState, RegulatorState newState, object data);
        protected abstract void OnAfterTransition(RegulatorState oldState, RegulatorState newState, object data);


        public bool SyncTransition(string newState)
        {
            return SyncTransition(newState, null);
        }

        private bool InTransition = false;

        public bool SyncTransition(string newState, object data)
        {
            if (!this.States.ContainsKey(newState))
            {
                return false;
            }

            lock (this)
            {
                if (InTransition)
                {
                    return false;
                }

                InTransition = true;
            }

            var oldState = this.CurrentState;
            var state = this.States[newState];

            if (oldState == state)
            {
                InTransition = false;
                return false;
            }

            try
            {
                foreach (var validator in Validators)
                {
                    if (validator.CanTransition(oldState.Name, state.Name) == false)
                    {
                        InTransition = false;
                        return false;
                    }
                }

                OnBeforeTransition(oldState, state, data);
                this.CurrentState = state;
                if (this.OnTransition != null)
                {
                    this.OnTransition(newState, data);
                }
                this.OnAfterTransition(oldState, state, data);
                return true;
            }
            catch (Exception ex)
            {
                this.ThrowError(ex);
            }
            finally
            {
                InTransition = false;
            }
            return false;
        }

        public void SyncProcessMessage(object message)
        {
            if (this.CurrentState != null)
            {
                this.CurrentState.ProcessMessage(message);
            }
        }

        public void SyncReset()
        {
            SyncTransition(DefaultState);
        }

        public void AddTransitionValidator(ITransitionValidator validator)
        {
            this.Validators.Add(validator);
        }
    }
    
    public interface IRegulatorDigestAction {
        void Handle(AbstractRegulator regulator);
    }

    public class RegulatorProcessMessageAction : IRegulatorDigestAction {
        private object Message;

        public RegulatorProcessMessageAction(object message){
            this.Message = message;
        }

        public void Handle(AbstractRegulator regulator)
        {
            regulator.SyncProcessMessage(Message);
        }
    }

    public class RegulatorResetAction : IRegulatorDigestAction
    {
        public void Handle(AbstractRegulator regulator)
        {
            regulator.SyncReset();
        }
    }

    public class RegulatorTransitionAction : IRegulatorDigestAction
    {
        public string NewState;
        public object Data;

        public RegulatorTransitionAction(string newState, object data)
        {
            this.NewState = newState;
            this.Data = data;
        }

        public void Handle(AbstractRegulator regulator)
        {
            regulator.SyncTransition(NewState, Data);
        }
    }

    public class RegulatorState
    {
        public string Name;
        private AbstractRegulator Regulator;
        private Dictionary<Type, RegulatorEventHandler> _OnDataHandlers = new Dictionary<Type, RegulatorEventHandler>();

        public RegulatorState(string name, AbstractRegulator regulator)
        {
            this.Name = name;
            this.Regulator = regulator;
        }

        public RegulatorState OnlyTransitionFrom(params string[] states)
        {

            this.Regulator.AddTransitionValidator(new TransitionFromValidator(states, this.Name));
            return this;
        }

        public RegulatorState Default()
        {
            this.Regulator.DefaultState = this.Name;
            return this;
        }

        public RegulatorState Transition()
        {
            this.Regulator.SyncTransition(this.Name);
            return this;
        }

        public RegulatorEventHandler OnData(Type dataType)
        {
            if (_OnDataHandlers.ContainsKey(dataType))
            {
                return _OnDataHandlers[dataType];
            }

            var result = new RegulatorEventHandler(this.Regulator, this);
            _OnDataHandlers.Add(dataType, result);
            return result;
        }

        public void ProcessMessage(object message)
        {
            if (message == null) { return; }
            var msgType = message.GetType();

            foreach (var type in _OnDataHandlers.Keys)
            {
                if (type.IsAssignableFrom(msgType))
                {
                    _OnDataHandlers[type].Invoke(message);
                    return;
                }
            }

            //Unhandled
        }
    }

    public interface ITransitionValidator
    {
        bool CanTransition(string oldState, string newState);
    }

    public class TransitionFromValidator : ITransitionValidator
    {
        private string[] From;
        private string To;

        public TransitionFromValidator(string[] from, string to)
        {
            this.From = from;
            this.To = to;
        }

        #region ITransitionValidator Members

        public bool CanTransition(string oldState, string newState)
        {
            if(newState == To){
                bool isOldStateValid = false;
                foreach(var state in From)
                {
                    if(oldState == state)
                    {
                        isOldStateValid = true;
                        break;
                    }
                }

                if(isOldStateValid == false)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }

    public class RegulatorEventHandler
    {
        private RegulatorCallback Callback;
        private Callback Callback2;
        private AbstractRegulator Regulator;
        private RegulatorState State;
        private string _TransitionTo;
        private bool _Error;

        public RegulatorEventHandler(AbstractRegulator regulator, RegulatorState state)
        {
            this.State = state;
            this.Regulator = regulator;
        }


        public RegulatorState InvokeCallback(Callback callback)
        {
            this.Callback2 = callback;
            return this.State;
        }

        public RegulatorState InvokeCallback(RegulatorCallback callback)
        {
            this.Callback = callback;
            return this.State;
        }

        public RegulatorState TransitionTo(string newState)
        {
            this._TransitionTo = newState;
            return this.State;
        }

        public RegulatorState Error()
        {
            this._Error = true;
            return this.State;
        }

        public void Invoke(object data)
        {
            if (this.Callback != null)
            {
                this.Callback(this.Regulator, data);
            }

            if (this.Callback2 != null)
            {
                this.Callback2();
            }

            if (this._TransitionTo != null)
            {
                this.Regulator.SyncTransition(this._TransitionTo, data);
            }

            if (this._Error)
            {
                throw new Exception("Message of invalid type received " + (data != null ? data.GetType().FullName : ""));
            }
        }
    }



    public delegate void RegulatorCallback(AbstractRegulator regulator, object data);
    public delegate void MessageReceived(object message);
}
