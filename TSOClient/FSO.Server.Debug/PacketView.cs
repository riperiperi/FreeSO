using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Be.Windows.Forms;
using Mina.Core.Buffer;
using System.IO;
using tso.debug.network;
using FSO.Server.Common;
using FSO.Server.Protocol.Voltron;
using FSO.Server.Debug.PacketAnalyzer;
using FSO.Server.Protocol.Aries;
using FSO.Server.Protocol.Voltron.Packets;
using FSO.Common.Serialization;

namespace FSO.Server.Debug
{
    public partial class PacketView : UserControl
    {
        private RawPacketReference Packet;
        private TabPage Tab;
        private DynamicByteProvider BytesProvider;
        private NetworkDebugger Inspector;

        private object ParsedPacket;
        private ISerializationContext Context;

        public PacketView(RawPacketReference packet, TabPage tab, NetworkDebugger inspector, ISerializationContext context)
        {
            this.Tab = tab;
            this.Packet = packet;
            this.Inspector = inspector;
            this.Context = context;

            InitializeComponent();

            BytesProvider = new DynamicByteProvider(packet.Packet.Data);
            
            hex.ByteProvider = BytesProvider;
            hex.StringViewVisible = true;

            splitContainer1.Panel2Collapsed = true;


            
        }

        private void PacketView_Load(object sender, EventArgs e)
        {
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            ((TabControl)this.Tab.Parent).TabPages.Remove(Tab);
        }

        private void btnStash_Click(object sender, EventArgs e)
        {
            var toStash = new RawPacketReference
            {
                Packet = new Packet
                {
                    Data = GetBytes(),
                    Direction = this.Packet.Packet.Direction,
                    Type = this.Packet.Packet.Type,
                    SubType = this.Packet.Packet.SubType
                },
                Sequence = this.Packet.Sequence
            };
            this.Inspector.Stash(toStash);
        }

        private void btnTools_Click(object sender, EventArgs e)
        {
            Analyze();
            splitContainer1.Panel2Collapsed = !splitContainer1.Panel2Collapsed;
        }


        private void ParsePacket()
        {
            /** Can we parse it? **/
            try
            {
                var packet = Packet.Packet;
                if(packet.Type == PacketType.VOLTRON)
                {
                    var voltronClazz = VoltronPackets.GetByPacketCode((ushort)packet.SubType);
                    if(voltronClazz != null)
                    {
                        IVoltronPacket parsed = (IVoltronPacket)Activator.CreateInstance(voltronClazz);
                        //TODO: VoltronContext
                        parsed.Deserialize(IoBuffer.Wrap(GetBytes()), Context);

                        this.ParsedPacket = parsed;
                        this.parsedInspetor.SelectedObject = ParsedPacket;
                    }
                }else if(packet.Type == PacketType.ARIES)
                {
                    var ariesClazz = AriesPackets.GetByPacketCode(packet.SubType);
                    if(ariesClazz != null)
                    {
                        IAriesPacket parsed = (IAriesPacket)Activator.CreateInstance(ariesClazz);
                        parsed.Deserialize(IoBuffer.Wrap(GetBytes()), Context);
                        this.ParsedPacket = parsed;
                        this.parsedInspetor.SelectedObject = ParsedPacket;
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void Analyze()
        {
            analyzeResults.Items.Clear();
            var analyzers = new IPacketAnalyzer[]{
                new PascalStringPacketAnalyzer(),
                new ByteCountPacketAnalyzer(),
                new ConstantsPacketAnalyzer(),
                new VariableLengthStringAnalyzer(),
                new ContentPacketAnalyzer()
            };

            var data = GetBytes();

            foreach (var analyzer in analyzers)
            {
                var results = analyzer.Analyze(data);
                if (results != null)
                {
                    foreach (var result in results)
                    {
                        analyzeResults.Items.Add(result);
                    }
                }
            }
        }


        private List<ISocketSession> AllSessions = new List<ISocketSession>();

        private void menuSend_DropDownOpening(object sender, EventArgs e)
        {
            /** Get the list of sessions **/
            menuSend.DropDownItems.Clear();
            AllSessions.Clear();

            foreach (var session in Inspector.GetSessions())
            {
                var label = session.ToString();

                var menuItem = new ToolStripMenuItem();
                menuItem.Text = label;
                menuItem.Click += new EventHandler(sendSession_Click);

                menuSend.DropDownItems.Add(menuItem);

                AllSessions.Add(session);
            }
        }

        void sendSession_Click(object sender, EventArgs e)
        {
            var index = menuSend.DropDownItems.IndexOf((ToolStripItem)sender);
            if (index != -1)
            {
                var session = AllSessions[index];

                switch (Packet.Packet.Type)
                {
                    case PacketType.VOLTRON:
                        var packet = new VariableVoltronPacket((ushort)Packet.Packet.SubType, GetBytes());
                        session.Write(packet);
                        break;
                }

            }
        }



        private byte[] GetBytes()
        {
            BytesProvider.ApplyChanges();
            byte[] allBytes = new byte[BytesProvider.Bytes.Count];

            for (int i = 0; i < allBytes.Length; i++)
            {
                allBytes[i] = BytesProvider.Bytes[i];
            }

            return allBytes;
        }

        private void analyzeResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (analyzeResults.SelectedItem != null)
            {
                PacketAnalyzerResult item = (PacketAnalyzerResult)analyzeResults.SelectedItem;
                hex.Select(item.Offset, item.Length);
            }
        }

        private void btnParse_Click(object sender, EventArgs e)
        {
            ParsePacket();
        }

        private void btnImportBytes_Click(object sender, EventArgs e)
        {
            var result = importBrowser.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                var bytes = File.ReadAllBytes(importBrowser.FileName);
                BytesProvider.InsertBytes(BytesProvider.Length, bytes);
            }
        }

        private void btnExportByteArray_Click(object sender, EventArgs e)
        {
            var bytes = GetBytes();

            var str = new StringBuilder();
            str.Append("new byte[]{");

            for (var i = 0; i < bytes.Length; i++)
            {
                if (i > 0)
                {
                    str.Append(", ");
                }
                str.Append("0x" + (bytes[i].ToString("X2")));
            }

            str.Append("}");

            Clipboard.SetText(str.ToString());
        }
    }
}
