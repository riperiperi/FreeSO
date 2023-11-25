using System;
using System.Collections.Generic;

namespace FSO.Common.Utils
{
    public class StateMachine <STATES> where STATES : IConvertible
    {
        public STATES CurrentState { get; internal set; }
        private Dictionary<STATES, List<STATES>> LegalMoves;

        public event Callback<STATES, STATES> OnTransition;

        public StateMachine(STATES startState)
        {
            this.CurrentState = startState;
        }


        public bool TransitionTo(STATES state)
        {
            
            lock (CurrentState)
            {
                if (CurrentState.Equals(state))
                {
                    return true;
                }

                /*if (!LegalMoves.ContainsKey(CurrentState))
                {
                    return false;
                }
                if (!LegalMoves[CurrentState].Contains(state))
                {
                    return false;
                }*/

                var previousState = CurrentState;
                this.CurrentState = state;
                if (OnTransition != null)
                {
                    OnTransition(previousState, CurrentState);
                }
                return true;
            }
        }

        /*public StateMachine<STATES> AllowTransition(STATES from, STATES to)
        {
            if (!LegalMoves.ContainsKey(from))
            {
                LegalMoves.Add(from, new List<STATES>());
            }
            LegalMoves[from].Add(to);
            return this;
        }*/
    }
}
