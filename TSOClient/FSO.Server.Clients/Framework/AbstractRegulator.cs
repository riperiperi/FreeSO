using FSO.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public AbstractRegulator()
        {
        }

        /*public void ReceivePackets(VoltronProtocolClient client)
        {
            client.OnPacket += (IVoltronPacket packet, VoltronProtocolClient client2) =>
            {
                this.ProcessMessage(packet);
            };
        }*/

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

        public void AddTransitionValidator(ITransitionValidator validator)
        {
            this.Validators.Add(validator);
        }

        public bool Transition(string newState)
        {
            return Transition(newState, null);
        }

        public bool Transition(string newState, object data)
        {
            if (!this.States.ContainsKey(newState))
            {
                return false;
            }

            var oldState = this.CurrentState;
            var state = this.States[newState];

            if (oldState == state)
            {
                return false;
            }

            try
            {
                foreach (var validator in Validators)
                {
                    if (validator.CanTransition(oldState.Name, state.Name) == false)
                    {
                        return false;
                    }
                }

                OnBeforeTransition(oldState, state, data);
                this.CurrentState = state;
                this.OnAfterTransition(oldState, state, data);
                if (this.OnTransition != null)
                {
                    this.OnTransition(newState, data);
                }
                return true;
            }
            catch (Exception ex)
            {
                this.ThrowError(ex);
            }
            return false;
        }

        protected void Reset()
        {
            Transition(DefaultState);
        }

        protected void ThrowErrorAndReset(object errorMessage)
        {
            ThrowError(errorMessage);
            Reset();
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


        protected void ProcessMessage(object message)
        {
            if (this.CurrentState != null)
            {
                this.CurrentState.ProcessMessage(message);
            }
        }

        protected RegulatorState AddState(string name)
        {
            var state = new RegulatorState(name, this);
            States.Add(name, state);
            return state;
        }


        protected abstract void OnBeforeTransition(RegulatorState oldState, RegulatorState newState, object data);
        protected abstract void OnAfterTransition(RegulatorState oldState, RegulatorState newState, object data);
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
            foreach (var state in states)
            {
                this.Regulator.AddTransitionValidator(new FromToTransitionValidator(state, this.Name));
            }
            return this;
        }

        public RegulatorState Default()
        {
            this.Regulator.DefaultState = this.Name;
            return this;
        }

        public RegulatorState Transition()
        {
            this.Regulator.Transition(this.Name);
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

    public class FromToTransitionValidator : ITransitionValidator
    {
        private string From;
        private string[] To;

        public FromToTransitionValidator(string from, params string[] to)
        {
            this.From = from;
            this.To = to;
        }

        #region ITransitionValidator Members

        public bool CanTransition(string oldState, string newState)
        {
            if (oldState == From)
            {
                bool canTransitino = false;
                for (int i = 0; i < To.Length; i++)
                {
                    if (To[i] == newState)
                    {
                        canTransitino = true;
                        break;
                    }
                }
                return canTransitino;
            }
            else
            {
                return true;
            }
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
                this.Regulator.Transition(this._TransitionTo, data);
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
