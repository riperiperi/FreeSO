using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using GonzoNet;
using GonzoNet.Encryption;

namespace PDChat
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            GonzoNet.Logger.OnMessageLogged += new MessageLoggedDelegate(Logger_OnMessageLogged);
        }

        /// <summary>
        /// Messages logged by GonzoNet.
        /// </summary>
        /// <param name="Msg">The message that was logged.</param>
        private void Logger_OnMessageLogged(LogMessage Msg)
        {
            Debug.WriteLine(Msg.Message);
        }

        /// <summary>
        /// Received characters from login server.
        /// </summary>
        private void NetworkController_OnReceivedCharacters()
        {
            LblStatus.Invoke(new MethodInvoker(delegate { LblStatus.Text = "Received characters..."; }));

            if (NetworkFacade.Avatars.Count == 0)
            {
                MessageBox.Show("You need to create a character before using this chat client!");
                NetworkFacade.Client.Disconnect();

                return;
            }

            PictureBox1.Invoke(new MethodInvoker(delegate 
            { 
                PictureBox1.Visible = true;
                PictureBox2.Visible = true;
                PictureBox3.Visible = true;

                LblUsername.Visible = false;
                LblPassword.Visible = false;

                BtnLogin.Visible = false;
                TxtUsername.Visible = false;
                TxtPassword.Visible = false;
            }));

            switch (NetworkFacade.Avatars.Count)
            {
                case 1:
                    LblName1.Invoke(new MethodInvoker(delegate
                    {
                        LblName1.Visible = true;
                        LblName1.Text = NetworkFacade.Avatars[0].Name;
                        LblName1.Location = new Point(this.Width / 2, LblName1.Location.Y);

                        PictureBox1.Image = NetworkFacade.Avatars[0].Thumbnail;
                        PictureBox1.Location = new Point(this.Width / 2, PictureBox1.Location.Y);

                        BtnChat1.Visible = true;
                        BtnChat1.Location = new Point((this.Width - 20) / 2, BtnChat1.Location.Y);
                    }));
                    break;
                case 2:
                    LblName1.Invoke(new MethodInvoker(delegate
                    {
                        //Enabling LblName3 and PictureBox3 here because it looks prettier...

                        LblName1.Visible = true;
                        LblName1.Text = NetworkFacade.Avatars[0].Name;

                        LblName3.Visible = true;
                        LblName3.Text = NetworkFacade.Avatars[1].Name;

                        PictureBox1.Image = NetworkFacade.Avatars[0].Thumbnail;
                        PictureBox3.Image = NetworkFacade.Avatars[1].Thumbnail;

                        BtnChat1.Visible = true;
                        BtnChat3.Visible = true;
                    }));
                    break;
                case 3:
                    LblName1.Invoke(new MethodInvoker(delegate
                    {
                        LblName1.Visible = true;
                        LblName1.Text = NetworkFacade.Avatars[0].Name;

                        LblName2.Visible = true;
                        LblName2.Text = NetworkFacade.Avatars[1].Name;

                        LblName3.Visible = true;
                        LblName3.Text = NetworkFacade.Avatars[2].Name;

                        PictureBox1.Image = NetworkFacade.Avatars[0].Thumbnail;
                        PictureBox2.Image = NetworkFacade.Avatars[1].Thumbnail;
                        PictureBox3.Image = NetworkFacade.Avatars[2].Thumbnail;

                        BtnChat1.Visible = true;
                        BtnChat2.Visible = true;
                        BtnChat3.Visible = true;
                    }));
                    break;
            }
        }

        /// <summary>
        /// User clicked button to log in.
        /// </summary>
        private void BtnLogin_Click(object sender, EventArgs e)
        {
            LoginArgsContainer Args = new LoginArgsContainer();

            if (TxtPassword.Text != "" && TxtUsername.Text != "")
            {
                NetworkFacade.Client = new NetworkClient(GlobalSettings.Default.LoginServerIP, 2106, 
                    GonzoNet.Encryption.EncryptionMode.AESCrypto);
                NetworkFacade.Client.OnConnected += new OnConnectedDelegate(NetworkController.Client_OnConnected);

                Args.Username = TxtUsername.Text.ToUpper();
                Args.Password = TxtPassword.Text.ToUpper();

                SaltedHash Hash = new SaltedHash(new SHA512Managed(), Args.Username.Length);
                byte[] HashBuf = Hash.ComputePasswordHash(Args.Username.ToUpper(), Args.Password.ToUpper());
                Args.Enc = new AESEncryptor(Convert.ToBase64String(HashBuf));
                Args.Client = NetworkFacade.Client;

                PlayerAccount.Username = TxtUsername.Text;

                LblStatus.Invoke(new MethodInvoker(delegate{ LblStatus.Visible = true; }));
                LblStatus.Invoke(new MethodInvoker(delegate { LblStatus.Text = "Connecting..."; }));

                NetworkController.OnReceivedCharacters += new OnReceivedCharactersDelegate(NetworkController_OnReceivedCharacters);

                NetworkFacade.Client.Connect(Args);
            }
            else
                MessageBox.Show("Please enter a username and password!");
        }

        /// <summary>
        /// Resets this form's controls.
        /// </summary>
        private void Reset()
        {
            PictureBox1.Invoke(new MethodInvoker(delegate
            {
                PictureBox1.Visible = false;
                PictureBox2.Visible = false;
                PictureBox3.Visible = false;

                BtnChat1.Visible = false;
                BtnChat2.Visible = false;
                BtnChat3.Visible = false;

                LblName1.Visible = false;
                LblName2.Visible = false;
                LblName2.Visible = false;

                LblUsername.Visible = true;
                LblPassword.Visible = true;

                BtnLogin.Visible = true;
                TxtUsername.Visible = true;
                TxtPassword.Visible = true;

                LblStatus.Text = "Connecting...";
                LblStatus.Visible = false;
            }));
        }

        #region Chat buttons

        private void BtnChat1_Click(object sender, EventArgs e)
        {
            if (NetworkFacade.Cities.Count == 0)
            {
                MessageBox.Show("No cityserver online! Disconnecting...");

                Reset();
                //This doesn't work here, because this is a callback. TODO: Find a work around...
                //NetworkFacade.Client.Disconnect();

                return;
            }

            NetworkController.Reconnect();
            ChatFrm Chat = new ChatFrm(NetworkFacade.Avatars[0]);
            Chat.Show();

            this.Close();
        }

        private void BtnChat2_Click(object sender, EventArgs e)
        {
            if (NetworkFacade.Cities.Count == 0)
            {
                MessageBox.Show("No cityserver online!");

                Reset();
                //This doesn't work here, because this is a callback. TODO: Find a work around...
                //NetworkFacade.Client.Disconnect();

                return;
            }

            NetworkController.Reconnect();
            //Because BtnChat2 is disabled unless the user has 3 avatars,
            //it means BtnChat2 will always represent NetworkFacade.Avatars[1].
            ChatFrm Chat = new ChatFrm(NetworkFacade.Avatars[1]);
            Chat.Show();

            this.Close();
        }

        private void BtnChat3_Click(object sender, EventArgs e)
        {
            if (NetworkFacade.Cities.Count == 0)
            {
                MessageBox.Show("No cityserver online! Disconnecting...");

                Reset();
                ////This doesn't work here, because this is a callback. TODO: Find a work around...
                //NetworkFacade.Client.Disconnect();

                return;
            }

            NetworkController.Reconnect();
            ChatFrm Chat;

            if (NetworkFacade.Avatars.Count == 2)
            {
                Chat = new ChatFrm(NetworkFacade.Avatars[1]);
                Chat.Show();
            }
            else
            {
                Chat = new ChatFrm(NetworkFacade.Avatars[2]);
                Chat.Show();
            }

            this.Close();
        }

        #endregion
    }
}
