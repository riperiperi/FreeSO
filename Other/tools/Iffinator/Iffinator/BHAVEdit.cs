using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Iffinator.Flash;

namespace Iffinator
{
    public partial class BHAVEdit : Form
    {
        private BHAV m_CurrentBHAV;
        private BHAVAnalyzer m_Analyzer;
        private List<IFFDecode> m_DecodedInstructions = new List<IFFDecode>();

        public BHAVEdit(Iff IffFile, BHAV CurrentBHAV)
        {
            InitializeComponent();

            m_CurrentBHAV = CurrentBHAV;
            m_Analyzer = new BHAVAnalyzer(IffFile);

            foreach (byte[] Instruction in m_CurrentBHAV.Instructions)
            {
                IFFDecode DecodedInstruction = new IFFDecode(Instruction);
                m_Analyzer.DecodeInstruction(ref DecodedInstruction);
                
                m_DecodedInstructions.Add(DecodedInstruction);
                LstInstructions.Items.Add(DecodedInstruction.OutStream.ToString());
            }
        }
    }
}
