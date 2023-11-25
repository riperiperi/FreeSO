using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using FSO.SimAntics;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay.EODs.Handlers;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Panels.EODs
{
    public class UIPizzaMakerEOD : UIEOD
    {
        public UIImage background;
        public UIScript Script;

        public Texture2D imageAnchovies { get; set; }
        public Texture2D imageCheese { get; set; }
        public Texture2D imageDough { get; set; }
        public Texture2D imageMushrooms { get; set; }
        public Texture2D imagePepperoni { get; set; }
        public Texture2D imageSauce { get; set; }
        public Texture2D imagePlayer { get; set; }

        public UIButton btnIngredient1 { get; set; }
        public UIButton btnIngredient2 { get; set; }
        public UIButton btnIngredient3 { get; set; }

        public UILabel labelStation1 { get; set; }
        public UILabel labelStation2 { get; set; }
        public UILabel labelStation3 { get; set; }
        public UILabel labelStation4 { get; set; }

        public UIImage PersonBG1;
        public UIImage PersonBG2;
        public UIImage PersonBG3;
        public UIImage PersonBG4;

        public UIVMPersonButton[] Players = new UIVMPersonButton[4];

        public VMEODPizzaState State;

        public UIPizzaMakerEOD(UIEODController controller) : base(controller)
        {
            var script = this.RenderScript("pizzamakereod.uis");
            Script = script;

            background = script.Create<UIImage>("background");
            AddAt(0, background);
            
            btnIngredient1.ButtonFrames = 3;
            btnIngredient2.ButtonFrames = 3;
            btnIngredient3.ButtonFrames = 3;

            btnIngredient1.Visible = false;
            btnIngredient2.Visible = false;
            btnIngredient3.Visible = false;

            btnIngredient1.OnButtonClick += (UIElement btn) => { SubmitIngredient(0); };
            btnIngredient2.OnButtonClick += (UIElement btn) => { SubmitIngredient(1); };
            btnIngredient3.OnButtonClick += (UIElement btn) => { SubmitIngredient(2); };

            PlaintextHandlers["pizza_show"] = P_Show;
            PlaintextHandlers["pizza_state"] = P_State;
            PlaintextHandlers["pizza_time"] = P_Time;
            PlaintextHandlers["pizza_result"] = P_Result;
            PlaintextHandlers["pizza_contrib"] = P_Contrib;
            PlaintextHandlers["pizza_hand"] = P_Hand;
            PlaintextHandlers["pizza_players"] = P_Players;

            PersonBG1 = script.Create<UIImage>("playerPos1");
            PersonBG2 = script.Create<UIImage>("playerPos2");
            PersonBG3 = script.Create<UIImage>("playerPos3");
            PersonBG4 = script.Create<UIImage>("playerPos4");
            PersonBG1.Texture = imagePlayer;
            PersonBG2.Texture = imagePlayer;
            PersonBG3.Texture = imagePlayer;
            PersonBG4.Texture = imagePlayer;
            Add(PersonBG1);
            Add(PersonBG2);
            Add(PersonBG3);
            Add(PersonBG4);

            labelStation1.Alignment = TextAlignment.Left;
            labelStation2.Alignment = TextAlignment.Left;
            labelStation3.Alignment = TextAlignment.Left;
            labelStation4.Alignment = TextAlignment.Left;

            EnterState(VMEODPizzaState.Lobby);
        }

        public void EnterState(VMEODPizzaState state)
        {
            State = state;
            btnIngredient1.Disabled = (state != VMEODPizzaState.Contribution);
            btnIngredient2.Disabled = (state != VMEODPizzaState.Contribution);
            btnIngredient3.Disabled = (state != VMEODPizzaState.Contribution);
            switch (state) {
                case VMEODPizzaState.Lobby:
                    P_Contrib("", "--\n--\n--\n--\n");
                    SetTip(Script.GetString("strNeed4Players")); break;
                case VMEODPizzaState.PhoneCall:
                    SetTip(Script.GetString("strWaitingForPhone")); break;
                case VMEODPizzaState.Contribution:
                    SetTip(Script.GetString("strWaitingForContribs")); break;
                case VMEODPizzaState.Bake:
                    SetTip(Script.GetString("strBaking")); break;
            }
        }

        public void SubmitIngredient(int id)
        {
            var buttons = new UIButton[] { btnIngredient1, btnIngredient2, btnIngredient3 };
            buttons[id].Visible = false; //button no more!
            foreach (var btn in buttons) btn.Disabled = true;
            Send("ingredient", id.ToString());
        }

        public void P_Show(string evt, string txt)
        {
            EODController.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 0,
                Expandable = false,
                Height = EODHeight.Tall,
                Length = EODLength.Full,
                Timer = EODTimer.Normal,
                Tips = EODTextTips.Short
            });
        }

        public void P_State(string evt, string txt)
        {
            EnterState((VMEODPizzaState)int.Parse(txt));
        }

        public void P_Players(string evt, string txt)
        {
            var split = txt.Split('\n');
            int[] items = new int[4];
            var labels = new UILabel[] { labelStation1, labelStation2, labelStation3, labelStation4 };
            for (int i=0; i<4; i++)
            {
                if (!int.TryParse(split[i], out items[i])) return;
                var avatar = (VMAvatar)EODController.Lot.vm.GetObjectById((short)items[i]);
                if (avatar == null)
                {
                    if (Players[i] != null)
                        Remove(Players[i]);
                    Players[i] = null;
                } else if (Players[i] == null || Players[i].Avatar != avatar)
                {
                    if (Players[i] != null)
                        Remove(Players[i]);
                    Players[i] = new UIVMPersonButton((VMAvatar)avatar, EODController.Lot.vm, true);
                    var bgs = new UIImage[] { PersonBG1, PersonBG2, PersonBG3, PersonBG4 };
                    Players[i].Position = bgs[i].Position + new Microsoft.Xna.Framework.Vector2(2, 2);
                    Add(Players[i]);
                }

                string caption = "";
                if (avatar == null)
                    caption = Script.GetString("strNoContributor");
                else
                {
                    if (i == 0) caption = Script.GetString("strBody") + avatar.GetPersonData(VMPersonDataVariable.BodySkill) / 100;
                    else if (i == 2) caption = Script.GetString("strCharisma") + avatar.GetPersonData(VMPersonDataVariable.CharismaSkill) / 100;
                    else caption = Script.GetString("strCooking") + avatar.GetPersonData(VMPersonDataVariable.CookingSkill) / 100;
                }
                labels[i].Caption = caption;
            }
        }

        public void P_Time(string evt, string txt)
        {
            int time = 0;
            int.TryParse(txt, out time);
            SetTime(time);
        }

        public void P_Result(string evt, string txt)
        {
            int resultVal = 1;
            int.TryParse(txt, out resultVal);
            if (resultVal > 1 && resultVal < 5) SetTip(Script.GetString("strResultSuccess"));
            else if (resultVal > 4 && resultVal < 8) SetTip(Script.GetString("strResultSuccessBonus"));
            else SetTip(Script.GetString("strResultFailure"));
        }

        public void P_Contrib(string evt, string txt)
        {
            var split = txt.Split('\n');
            labelStation1.Caption = GetIngredientName((split[0] == "--") ? null : new VMEODIngredientCard(split[0]));
            labelStation2.Caption = GetIngredientName((split[1] == "--") ? null : new VMEODIngredientCard(split[1]));
            labelStation3.Caption = GetIngredientName((split[2] == "--") ? null : new VMEODIngredientCard(split[2]));
            labelStation4.Caption = GetIngredientName((split[3] == "--") ? null : new VMEODIngredientCard(split[3]));
        }

        public void P_Hand(string evt, string txt)
        {
            var buttons = new UIButton[] { btnIngredient1, btnIngredient2, btnIngredient3 };
            var split = txt.Split('\n');
            for (int i = 0; i < 3; i++)
            {
                Texture2D tex = null;
                int btnFrame = 0;
                string caption = "";
                if (split[i] != "--")
                {
                    var card = new VMEODIngredientCard(split[i]);

                    switch (card.Type)
                    {
                        case VMEODIngredientType.Anchovies:
                            tex = imageAnchovies; break;
                        case VMEODIngredientType.Cheese:
                            tex = imageCheese; break;
                        case VMEODIngredientType.Dough:
                            tex = imageDough; break;
                        case VMEODIngredientType.Mushrooms:
                            tex = imageMushrooms; break;
                        case VMEODIngredientType.Pepperoni:
                            tex = imagePepperoni; break;
                        case VMEODIngredientType.Sauce:
                            tex = imageSauce; break;
                    }

                    btnFrame = (2 - ((int)card.Size));
                    caption = GetIngredientName(card);
                }
                if (tex == null) buttons[i].Visible = false;
                else
                {
                    buttons[i].Texture = tex;
                    buttons[i].Visible = true;
                    buttons[i].ButtonFrame = btnFrame;
                    buttons[i].Tooltip = caption;
                }
            }
        }

        public string GetIngredientName(VMEODIngredientCard card)
        {
            if (card == null) return Script.GetString("strNoContributor");
            var prefix = GameFacade.Strings.GetString("204", (10-(int)card.Size).ToString());

            int suffixN = 7;
            switch (card.Type)
            {
                case VMEODIngredientType.Dough:
                    suffixN = 3; break;
                case VMEODIngredientType.Sauce:
                    suffixN = 6; break;
                case VMEODIngredientType.Cheese:
                    suffixN = 2; break;
                case VMEODIngredientType.Anchovies:
                    suffixN = 1; break;
                case VMEODIngredientType.Mushrooms:
                    suffixN = 4; break;
                case VMEODIngredientType.Pepperoni:
                    suffixN = 5; break;
            }
            var suffix = GameFacade.Strings.GetString("204", suffixN.ToString());
            return prefix + suffix;
        }

        public override void OnClose()
        {
            CloseInteraction();
            base.OnClose();
        }
    }
}
