using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.SimAntics.NetPlay.EODs.Model;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODGameshowBuzzerPlayerPlugin : VMEODAbstractGameshowBuzzerPlugin
    {
        internal double SessionStamp { get; set; }
        internal double SearchStamp { get; set; }
        internal bool MyBuzzerEnabled { get; set; }
        internal VMEODClient MyClient { get; private set; }
        internal short MyScore { get; private set; }

        internal string AvatarName
        {
            get { return MyClient.Avatar?.Name ?? ""; }
        }

        public VMEODGameshowBuzzerPlayerPlugin(VMEODServer server) : base(server)
        {
            BinaryHandlers["Buzzer_Player_Buzzed"] = PlayerBuzzedHandler;
            SimanticsHandlers[(short)VMEODGameshowPlayerPluginEvents.Sync_Callback] = SyncCallbackHandler;
        }

        public override void OnConnection(VMEODClient client)
        {
            if (client.Avatar != null)
            {
                EODType = VMEODGameshowBuzzerPluginType.Player;
                MyBuzzerEnabled = false;
                // get the score from the object's via tempregisters
                MyScore = client.Invoker.Thread.TempRegisters[0];
                MyClient = client;
                MyClient.Send("BuzzerEOD_Init", new byte[] { (byte)VMEODGameshowBuzzerPluginType.Player });

                // update the UI to match their buzzer object's score
                MyClient.Send("Buzzer_Player_Score", BitConverter.GetBytes(MyScore));

                SyncEvent += PlayerSyncHandler;
            }
            base.OnConnection(client);
        }
        public override void OnDisconnection(VMEODClient client)
        {
            SessionStamp = 0;
            SearchStamp = 0;
            SyncEvent -= PlayerSyncHandler;
            base.OnDisconnection(client);
        }

        /// <summary>
        /// Changes the score on the game objet and in the player's UIEOD
        /// </summary>
        /// <param name="newScore"></param>
        internal void ChangeMyScore(short newScore, bool immediately)
        {
            MyScore = newScore;

            // execute Simantics event to display new score on this object
            if (immediately)
                Controller.SendOBJEvent(new VMEODEvent((short)VMEODGameshowPlayerPluginEvents.Change_My_Score, new short[] { MyScore }));

            // execute Client event to display new score in UIEOD
            MyClient.Send("Buzzer_Player_Score", BitConverter.GetBytes(MyScore));
        }
        /// <summary>
        /// Updates the player's UIEOD to reflect their new score
        /// </summary>
        /// <param name="pointsAwarded">Points from Host's GlobalScore awarded for correct answer</param>
        internal void ExecuteCorrectAnswer(short pointsAwarded)
        {
            ChangeMyScore((short)Math.Min(MyScore + pointsAwarded, VMEODAbstractGameshowBuzzerPlugin.MAX_SCORE), false);
            ReactToMyJudgment(1);
        }
        /// <summary>
        /// Updates the player's UIEOD to reflect their new score
        /// </summary>
        /// <param name="pointsDeducted">Points from Host's GlobalScore deducted for incorrect answer; 0 if points are not set to deduct from wrong answer (host set option)</param>
        internal void ExecuteIncorrectAnswer(short pointsDeducted)
        {
            ChangeMyScore((short)Math.Max(MyScore - pointsDeducted, 0), false);
            ReactToMyJudgment(0);
        }
        /// <summary>
        /// Another player answered a question and the host indicated if they were correct or incorrect
        /// </summary>
        /// <param name="were_they_correct"></param>
        /// <param name="opponentName"></param>
        internal void ExecuteOthersAnswer(short were_they_correct, string opponentName)
        {
            string evt = "Buzzer_Player_Other_Correct";
            if (were_they_correct == 0)
                evt = "Buzzer_Player_Other_Incorrect";
            // execute client event to update UI for other's answer
            Send(evt, opponentName);
            ReactToOtherJudgment(were_they_correct);
        }
        /// <summary>
        /// Another player answered a question and the host indicated if they were correct or incorrect
        /// </summary>
        /// <param name="were_they_correct"></param>
        /// <param name="opponentName"></param>
        internal void ExecuteAnswererDisconnect()
        {
            // execute client event to update UI for other's answer
            Send("Buzzer_Player_Other_Incorrect", "");
            Controller.SendOBJEvent(new VMEODEvent((short)VMEODGameshowPlayerPluginEvents.React_To_Others_Judgment, new short[] { 0 })); ;
        }
        /// <summary>
        /// This player buzzed, whether first or not quite first.
        /// </summary>
        /// <param name="was_I_First"></param>
        internal void ActivateMyBuzzer(short was_I_First)
        {
            // execute Simantics event to animate Sim for their buzzer being activated;
            Controller.SendOBJEvent(new VMEODEvent((short)VMEODGameshowPlayerPluginEvents.Activate_My_Buzzer, new short[] { was_I_First }));

            // execute client event to update UI reflecting that this player did or did not buzz first
            Send("BuzzerEOD_Buzzed", BitConverter.GetBytes(was_I_First) );
        }
        /// <summary>
        /// Another player buzzed.
        /// </summary>
        internal void ActivateOtherBuzzer()
        {
            // immedately execute Simantics event to animate Sim for other's buzzer, but only if they don't have an Activate_My_Buzzer event queued
            Controller.SendOBJEvent(new VMEODEvent((short)VMEODGameshowPlayerPluginEvents.Activate_Others_Buzzer));

            // execute client event to update UI reflecting that this player did not buzz first
            Send("BuzzerEOD_Buzzed", new byte[] { 0 });
        }
        internal void Send(string evt, string args)
        {
            MyClient.Send(evt, args);
        }
        internal void Send(string evt, byte[] args)
        {
            MyClient.Send(evt, args);
        }
        /// <summary>
        /// Send Simantics event to have Avatar react winning or losing the game; 2 = winner, 1 = loser
        /// </summary>
        /// <param name="did_I_win">1 = no, 2 = yes</param>
        /// <param name="winnerName">Avatar name of winner</param>
        internal void DeclareWinner(short did_I_win, string winnerName)
        {
            // Update EOUID to display winning avatar's name
            Send("Buzzer_Player_Win", winnerName);

            // Avatars react to winning or not winning; if this is the winner then execute immediately
            if (did_I_win == 2)
                Controller.SendOBJEvent(new VMEODEvent((short)VMEODGameshowPlayerPluginEvents.Declare_Winner, new short[] { did_I_win }));
            else
                EnqueueEvent(new VMEODEvent((short)VMEODGameshowPlayerPluginEvents.Declare_Winner, new short[] { did_I_win }));
        }
        /// <summary>
        /// Update UIEOD to notify player if their answer was correct. Immedaitely send Simantics event to have Avatar react accordingly.
        /// </summary>
        /// <param name="was_I_correct"></param>
        private void ReactToMyJudgment(short was_I_correct)
        {
            // immediately execute Simantics event to animate Sim for correct answer
            Controller.SendOBJEvent(new VMEODEvent((short)VMEODGameshowPlayerPluginEvents.React_To_My_Judgment, new short[] { was_I_correct }));

            // execute client event to update UI for correct answer
            Send("BuzzerEOD_Answer", new byte[] { (byte)was_I_correct });
        }
        /// <summary>
        /// Send Simantics event to have Avatar react to another player having answered a question correctly or incorrectly
        /// </summary>
        /// <param name="were_they_correct"></param>
        private void ReactToOtherJudgment(short were_they_correct)
        {
            // execute Simantics event to animate Sim for other's answer
            EnqueueEvent(new VMEODEvent((short)VMEODGameshowPlayerPluginEvents.React_To_Others_Judgment, new short[] { were_they_correct }));
        }
        #region events
        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt">VMEODGameshowPlayerPluginEvents.Sync_Callback (100)</param>
        /// <param name="controller"></param>
        private void SyncCallbackHandler(short evt, VMEODClient controller)
        {
            SyncHandler(this);
        }
        /// <summary>
        /// I pressed my buzzer.
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="data"></param>
        /// <param name="client"></param>
        private void PlayerBuzzedHandler(string evt, byte[] data, VMEODClient client)
        {
            if (MyBuzzerEnabled)
                PlayerBuzzedEventHandler(this);
        }

        private void PlayerSyncHandler(VMEODGameshowBuzzerPlayerPlugin plugin)
        {
            Controller.SendOBJEvent(new VMEODEvent((short)VMEODGameshowPlayerPluginEvents.Change_My_Score, new short[] { MyScore }));
        }
        #endregion
    }

    public enum VMEODGameshowPlayerPluginEvents: short
    {
        Activate_My_Buzzer = 1, // accompanied with argument: was_I_first?
        React_To_My_Judgment = 2, // accompanied with argument: was_I_correct?
        Activate_Others_Buzzer = 3,
        React_To_Others_Judgment = 4, // accompanied with argument: were_they_correct?
        Change_My_Score = 5, // new score in temp0
        Declare_Winner = 6, // temp0 = 1 if I'm the winner, 0 if I'm not
        Sync_Callback = 100
    }
}
