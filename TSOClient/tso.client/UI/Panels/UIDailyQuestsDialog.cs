using System;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.Server.Clients;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels
{
    // Daily-quest tracker dialog. Triggered (v1) by the /quests chat
    // command. Fetches today's three quests from /userapi/quests/today on
    // open, lets the player claim completed quests immediately rather
    // than waiting for the nightly cron payout.
    //
    // Visual primitives only — no new .uis script, no new pixel art.
    // Inherits TSO's standard dialog chrome via UIDialog so it looks
    // native to the rest of the game. See edenso_server_data/
    // design_daily_quests_v1.md.
    public class UIDailyQuestsDialog : UIDialog
    {
        private const int DIALOG_WIDTH = 440;
        private const int DIALOG_HEIGHT = 340;
        private const int ROW_HEIGHT = 80;
        private const int ROW_START_Y = 50;

        private readonly uint _avatarId;
        private readonly ApiClient _api;
        private readonly QuestRow[] _rows = new QuestRow[3];
        private readonly UILabel _emptyMessage;
        private readonly UILabel _footer;

        public UIDailyQuestsDialog(uint avatarId)
            : base(UIDialogStyle.Standard | UIDialogStyle.Close, true)
        {
            _avatarId = avatarId;
            _api = new ApiClient(ApiClient.CDNUrl ?? GlobalSettings.Default.GameEntryUrl);

            Caption = "Daily Quests";
            SetSize(DIALOG_WIDTH, DIALOG_HEIGHT);

            // Three empty rows; populated by Refresh().
            for (int i = 0; i < _rows.Length; i++)
            {
                _rows[i] = new QuestRow();
                _rows[i].Position = new Vector2(24, ROW_START_Y + i * ROW_HEIGHT);
                _rows[i].OnClaim = OnClaimClicked;
                _rows[i].Visible = false;
                Add(_rows[i]);
            }

            // Shown when the cron hasn't rolled today's quests yet, or
            // when the request fails. Hidden once data lands.
            _emptyMessage = new UILabel();
            _emptyMessage.Caption = "Loading…";
            _emptyMessage.Position = new Vector2(32, 70);
            _emptyMessage.Size = new Vector2(DIALOG_WIDTH - 64, 30);
            Add(_emptyMessage);

            _footer = new UILabel();
            _footer.Caption = ComposeFooter();
            _footer.Position = new Vector2(32, DIALOG_HEIGHT - 40);
            _footer.Size = new Vector2(DIALOG_WIDTH - 64, 20);
            Add(_footer);

            CloseButton.OnButtonClick += btn => UIScreen.RemoveDialog(this);

            Refresh();
        }

        // Fetch today's quests + repaint rows. Re-callable; safe to invoke
        // after a Claim too so the dialog reflects the post-claim state.
        public void Refresh()
        {
            _emptyMessage.Caption = "Loading…";
            _emptyMessage.Visible = true;
            foreach (var r in _rows) r.Visible = false;

            _api.GetDailyQuests(_avatarId, list =>
            {
                if (list == null || list.quests == null || list.quests.Count == 0)
                {
                    _emptyMessage.Caption =
                        "No quests for today yet. Check back after midnight UTC.";
                    return;
                }
                _emptyMessage.Visible = false;
                for (int i = 0; i < _rows.Length; i++)
                {
                    if (i < list.quests.Count)
                    {
                        _rows[i].Set(list.quests[i]);
                        _rows[i].Visible = true;
                    }
                    else
                    {
                        _rows[i].Visible = false;
                    }
                }
            });
        }

        // Footer line — countdown to next midnight UTC + reminder that
        // rewards land in the inbox if not claimed inline.
        private static string ComposeFooter()
        {
            var now = DateTime.UtcNow;
            var nextReset = now.Date.AddDays(1);
            var remain = nextReset - now;
            return $"Resets in {remain.Hours}h {remain.Minutes}m. " +
                   "Unclaimed rewards also land in your inbox.";
        }

        // Recompose footer each tick so the countdown stays live. Cheap
        // string assignment, no allocations matter at 60Hz.
        public override void Update(UpdateState state)
        {
            base.Update(state);
            _footer.Caption = ComposeFooter();
        }

        private void OnClaimClicked(byte slot)
        {
            _api.ClaimDailyQuest(_avatarId, slot, result =>
            {
                // Pop a small confirmation either way — successful claim
                // shows the reward, failures just refresh silently so the
                // user sees the correct state (e.g. someone else / the cron
                // already paid it out).
                if (result != null)
                {
                    UIAlert.Alert(
                        "Quest Reward",
                        $"§{result.reward:N0} simoleons added.\n" +
                        $"New balance: §{result.new_balance:N0}",
                        true);
                }
                Refresh();
            });
        }

        // One quest row. Title + reward on top line, progress bar below,
        // claim button bottom-right when completed-but-unclaimed.
        // Status text right of the bar shows "X / Y" or "Completed".
        private class QuestRow : UIContainer
        {
            private readonly UILabel _title;
            private readonly UILabel _reward;
            private readonly UIProgressBar _bar;
            private readonly UILabel _status;
            private readonly UIButton _claim;
            private byte _slot;

            public Action<byte> OnClaim;

            public QuestRow()
            {
                _title = new UILabel
                {
                    Position = new Vector2(0, 0),
                    Size = new Vector2(260, 18)
                };
                Add(_title);

                _reward = new UILabel
                {
                    Position = new Vector2(280, 0),
                    Size = new Vector2(100, 18)
                };
                Add(_reward);

                _bar = new UIProgressBar();
                _bar.Position = new Vector2(0, 26);
                _bar.SetSize(260, 22);
                _bar.MinValue = 0;
                _bar.MaxValue = 100;
                _bar.Value = 0;
                Add(_bar);

                _status = new UILabel
                {
                    Position = new Vector2(0, 50),
                    Size = new Vector2(260, 18)
                };
                Add(_status);

                _claim = new UIButton();
                _claim.Caption = "Claim";
                _claim.Position = new Vector2(280, 28);
                _claim.Visible = false;
                _claim.OnButtonClick += btn => OnClaim?.Invoke(_slot);
                Add(_claim);
            }

            public void Set(ApiDailyQuest q)
            {
                _slot = q.slot;
                _title.Caption = q.description;
                _reward.Caption = $"Reward: §{q.reward:N0}";

                _bar.MaxValue = Math.Max(q.target, (ulong)1);
                _bar.Value = Math.Min(q.progress, q.target);

                if (q.claimed)
                {
                    _status.Caption = "Reward claimed ✓";
                    _claim.Visible = false;
                }
                else if (q.completed)
                {
                    _status.Caption = "Completed — claim your reward!";
                    _claim.Visible = true;
                }
                else
                {
                    _status.Caption = $"{q.progress:N0} / {q.target:N0}";
                    _claim.Visible = false;
                }
            }
        }
    }
}