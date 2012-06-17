using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace PDPatcher
{
    public partial class MsgBox : Form
    {
        private Point m_MouseOffset;
        private ImageList m_BtnYesImgList, m_BtnNoImgList, m_BtnOKImgList;
        //Download-threads must wait for this to be set in order to start redownloading
        //files that weren't fully downloaded.
        public ManualResetEvent ProceedEvent = new ManualResetEvent(false);

        //Should incorrect files be redownloaded or not?
        public bool RedownloadFiles = false;

        //Is this messagebox informing the user about files that needed to be 
        //redownloaded?
        private bool m_IncorrectDownload = false;

        /// <summary>
        /// This constructor can construct a form asking the user if it wants to quit,
        /// or an about-messagebox.
        /// </summary>
        /// <param name="Quit">Set to true to construct a form asking the user if it wants to quit.</param>
        public MsgBox(bool Quit)
        {
            InitializeComponent();

            Bitmap BackgroundBitmap = Properties.Resources.E2B14588WinGen;
            BackgroundBitmap.MakeTransparent(Color.FromArgb(255, 0, 255));

            this.TransparencyKey = Color.Gray;
            this.BackColor = Color.Gray;
            this.BackgroundImage = BackgroundBitmap;
            this.MouseDown += new MouseEventHandler(MsgBox_MouseDown);
            this.MouseMove += new MouseEventHandler(MsgBox_MouseMove);

            LblInformation.BackColor = Color.Transparent;

            //Turns the form into a messagebox asking the user if it wants to quit the application.
            if (Quit)
            {
                BtnYes.Visible = true;
                BtnNo.Visible = true;

                m_BtnYesImgList = new ImageList();
                m_BtnYesImgList.ImageSize = new Size(64, 34);
                m_BtnNoImgList = new ImageList();
                m_BtnNoImgList.ImageSize = new Size(64, 34);

                Bitmap BtnBitmap = Properties.Resources.e2b66db8GZBtn;
                BtnBitmap.MakeTransparent(Color.FromArgb(255, 0, 255));

                m_BtnYesImgList.Images.AddStrip(BtnBitmap);
                m_BtnNoImgList.Images.AddStrip(BtnBitmap);

                BtnYes.BackColor = Color.Transparent;
                BtnYes.Image = m_BtnYesImgList.Images[1];
                BtnYes.FlatStyle = FlatStyle.Flat;
                BtnYes.FlatAppearance.BorderSize = 0;
                BtnYes.FlatAppearance.MouseOverBackColor = Color.Transparent;
                BtnYes.FlatAppearance.MouseDownBackColor = Color.Transparent;

                BtnNo.BackColor = Color.Transparent;
                BtnNo.Image = m_BtnYesImgList.Images[1];
                BtnNo.FlatStyle = FlatStyle.Flat;
                BtnNo.FlatAppearance.BorderSize = 0;
                BtnNo.FlatAppearance.MouseOverBackColor = Color.Transparent;
                BtnNo.FlatAppearance.MouseDownBackColor = Color.Transparent;

                BtnYes.MouseEnter += new EventHandler(BtnYes_MouseEnter);
                BtnYes.MouseLeave += new EventHandler(BtnYes_MouseLeave);
                BtnYes.Click += new EventHandler(BtnYes_Click);

                BtnNo.MouseEnter += new EventHandler(BtnNo_MouseEnter);
                BtnNo.MouseLeave += new EventHandler(BtnNo_MouseLeave);
                BtnNo.Click += new EventHandler(BtnNo_Click);

                LblInformation.Text = "Are you sure you want to quit?";
            }
            //Turns the form into an about-messagebox.
            else
            {
                BtnOK.Visible = true;
                LblInformation.Location = new Point(15, 40);
                LblInformation.Text = "Project Dollhouse Update Utility v. 0.2\n" +
                    "Written by Mats 'Afr0' Vederhus, 2012";

                m_BtnOKImgList = new ImageList();
                m_BtnOKImgList.ImageSize = new Size(64, 34);

                Bitmap BtnBitmap = Properties.Resources.e2b66db8GZBtn;
                BtnBitmap.MakeTransparent(Color.FromArgb(255, 0, 255));

                m_BtnOKImgList.Images.AddStrip(BtnBitmap);

                BtnOK.BackColor = Color.Transparent;
                BtnOK.Image = m_BtnOKImgList.Images[1];
                BtnOK.FlatStyle = FlatStyle.Flat;
                BtnOK.FlatAppearance.BorderSize = 0;
                BtnOK.FlatAppearance.MouseOverBackColor = Color.Transparent;
                BtnOK.FlatAppearance.MouseDownBackColor = Color.Transparent;

                BtnOK.MouseEnter += new EventHandler(BtnOK_MouseEnter);
                BtnOK.MouseLeave += new EventHandler(BtnOK_MouseLeave);
                BtnOK.Click += new EventHandler(BtnOK_Click);
            }
        }

        /// <summary>
        /// Constructs a form that informs the user about an error condition.
        /// Mostly used for handling network-related errors.
        /// </summary>
        /// <param name="ErrorMessage">The errormessage to display.</param>
        public MsgBox(string ErrorMessage)
        {
            InitializeComponent();

            Bitmap BackgroundBitmap = Properties.Resources.E2B14588WinGen;
            BackgroundBitmap.MakeTransparent(Color.FromArgb(255, 0, 255));

            this.TransparencyKey = Color.Gray;
            this.BackColor = Color.Gray;
            this.BackgroundImage = BackgroundBitmap;
            this.MouseDown += new MouseEventHandler(MsgBox_MouseDown);
            this.MouseMove += new MouseEventHandler(MsgBox_MouseMove);

            LblInformation.BackColor = Color.Transparent;
            LblInformation.Location = new Point(15, 40);
            LblInformation.Text = ErrorMessage;

            BtnOK.Visible = true;
            m_BtnOKImgList = new ImageList();
            m_BtnOKImgList.ImageSize = new Size(64, 34);

            Bitmap BtnBitmap = Properties.Resources.e2b66db8GZBtn;
            BtnBitmap.MakeTransparent(Color.FromArgb(255, 0, 255));

            m_BtnOKImgList.Images.AddStrip(BtnBitmap);

            BtnOK.BackColor = Color.Transparent;
            BtnOK.Image = m_BtnOKImgList.Images[1];
            BtnOK.FlatStyle = FlatStyle.Flat;
            BtnOK.FlatAppearance.BorderSize = 0;
            BtnOK.FlatAppearance.MouseOverBackColor = Color.Transparent;
            BtnOK.FlatAppearance.MouseDownBackColor = Color.Transparent;

            BtnOK.MouseEnter += new EventHandler(BtnOK_MouseEnter);
            BtnOK.MouseLeave += new EventHandler(BtnOK_MouseLeave);
            BtnOK.Click += new EventHandler(BtnOK_Click);
        }

        /// <summary>
        /// Constructs a form that asks the user whether or not to redownload files that weren't
        /// properly downloaded. If the user answers no, the update is rolled back.
        /// </summary>
        public MsgBox()
        {
            InitializeComponent();

            Bitmap BackgroundBitmap = Properties.Resources.E2B14588WinGen;
            BackgroundBitmap.MakeTransparent(Color.FromArgb(255, 0, 255));

            this.TransparencyKey = Color.Gray;
            this.BackColor = Color.Gray;
            this.BackgroundImage = BackgroundBitmap;
            this.MouseDown += new MouseEventHandler(MsgBox_MouseDown);
            this.MouseMove += new MouseEventHandler(MsgBox_MouseMove);

            LblInformation.BackColor = Color.Transparent;
            LblInformation.Text = "One or more files were \nincorrectly downloaded - \nwant to try again?";

            BtnYes.Visible = true;
            BtnNo.Visible = true;

            m_BtnYesImgList = new ImageList();
            m_BtnYesImgList.ImageSize = new Size(64, 34);
            m_BtnNoImgList = new ImageList();
            m_BtnNoImgList.ImageSize = new Size(64, 34);

            Bitmap BtnBitmap = Properties.Resources.e2b66db8GZBtn;
            BtnBitmap.MakeTransparent(Color.FromArgb(255, 0, 255));

            m_BtnYesImgList.Images.AddStrip(BtnBitmap);
            m_BtnNoImgList.Images.AddStrip(BtnBitmap);

            BtnYes.BackColor = Color.Transparent;
            BtnYes.Image = m_BtnYesImgList.Images[1];
            BtnYes.FlatStyle = FlatStyle.Flat;
            BtnYes.FlatAppearance.BorderSize = 0;
            BtnYes.FlatAppearance.MouseOverBackColor = Color.Transparent;
            BtnYes.FlatAppearance.MouseDownBackColor = Color.Transparent;

            BtnNo.BackColor = Color.Transparent;
            BtnNo.Image = m_BtnYesImgList.Images[1];
            BtnNo.FlatStyle = FlatStyle.Flat;
            BtnNo.FlatAppearance.BorderSize = 0;
            BtnNo.FlatAppearance.MouseOverBackColor = Color.Transparent;
            BtnNo.FlatAppearance.MouseDownBackColor = Color.Transparent;

            m_IncorrectDownload = true;

            BtnYes.MouseEnter += new EventHandler(BtnYes_MouseEnter);
            BtnYes.MouseLeave += new EventHandler(BtnYes_MouseLeave);
            BtnYes.Click += new EventHandler(BtnYes_Click);

            BtnNo.MouseEnter += new EventHandler(BtnNo_MouseEnter);
            BtnNo.MouseLeave += new EventHandler(BtnNo_MouseLeave);
            BtnNo.Click += new EventHandler(BtnNo_Click);
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void BtnOK_MouseEnter(object sender, EventArgs e)
        {
            BtnOK.Image = m_BtnOKImgList.Images[2];
        }

        private void BtnOK_MouseLeave(object sender, EventArgs e)
        {
            BtnOK.Image = m_BtnOKImgList.Images[1];
        }

        private void BtnYes_Click(object sender, EventArgs e)
        {
            if (m_IncorrectDownload)
            {
                RedownloadFiles = true;
                this.DialogResult = DialogResult.Yes;
            }
            else
            {
                //TODO: Shutdown network-sybsystem and filehandling.
                //TODO: Delete backup files.

                Application.Exit();
            }
        }

        private void BtnYes_MouseEnter(object sender, EventArgs e)
        {
            BtnYes.Image = m_BtnYesImgList.Images[2];
        }

        private void BtnYes_MouseLeave(object sender, EventArgs e)
        {
            BtnYes.Image = m_BtnYesImgList.Images[1];
        }

        private void BtnNo_Click(object sender, EventArgs e)
        {
            if (m_IncorrectDownload)
            {
                RedownloadFiles = false;
                this.DialogResult = DialogResult.No;
            }
            else
            {
                this.Close();
            }
        }

        private void BtnNo_MouseEnter(object sender, EventArgs e)
        {
            BtnNo.Image = m_BtnNoImgList.Images[2];
        }

        private void BtnNo_MouseLeave(object sender, EventArgs e)
        {
            BtnNo.Image = m_BtnNoImgList.Images[1];
        }

        /// <summary>
        /// The form was informed that the user moved the mouse.
        /// </summary>
        private void MsgBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Point MousePosition = Control.MousePosition;
                MousePosition.Offset(m_MouseOffset);
                this.Location = MousePosition;
            }
        }

        /// <summary>
        /// A mousebutton was pressed down while the mouse was within
        /// the region of this form.
        /// </summary>
        private void MsgBox_MouseDown(object sender, MouseEventArgs e)
        {
            m_MouseOffset = new Point(-e.X, -e.Y);
        }
    }
}
