using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using GonzoNet;
using PDChat.Sims;

namespace PDChat
{
    public partial class ChatFrm : Form
    {
        Sim m_CurrentSim;

        public ChatFrm(Sim Avatar)
        {
            InitializeComponent();

            m_CurrentSim = Avatar;
        }
    }
}
