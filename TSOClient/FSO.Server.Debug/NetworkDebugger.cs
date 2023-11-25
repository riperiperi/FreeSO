using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using FSO.Server.Common;
using FSO.Server.Protocol.Voltron;
using tso.debug.network;
using FSO.Common.Serialization;
using Ninject;

namespace FSO.Server.Debug
{
    public partial class NetworkDebugger : Form, IServerDebugger, IPacketLogger
    {
        private Dictionary<string, RawPacketReference> Packets = new Dictionary<string, RawPacketReference>();
        private int PacketSequence = 0;
        private NetworkStash _Stash;
        private ISerializationContext Context;

        public NetworkDebugger(IKernel kernel)
        {
            Context = kernel.Get<ISerializationContext>();

            _Stash = new NetworkStash(Path.Combine(Directory.GetCurrentDirectory(), "debug-network-stash"));
            InitializeComponent();

            var packetTypes = Enum.GetNames(typeof(VoltronPacketType));
            foreach (var packetType in packetTypes)
            {
                var item = new ToolStripMenuItem();
                item.Text = packetType;
                item.Click += new EventHandler(createPacket_Click);
                btnCreate.DropDownItems.Add(item);
            }
        }

        void createPacket_Click(object sender, EventArgs e)
        {
            var index = btnCreate.DropDownItems.IndexOf((ToolStripItem)sender);
            if (index != -1)
            {
                var enumNames = Enum.GetNames(typeof(VoltronPacketType));
                var packetType = (VoltronPacketType)Enum.Parse(typeof(VoltronPacketType), (string)enumNames.GetValue(index));


                OnPacket(new Packet {
                    Type = PacketType.VOLTRON,
                    SubType = packetType.GetPacketCode(),
                    Direction = PacketDirection.OUTPUT,
                    Data = new byte[0]
                });
            }
        }

        private void RefreshStashMenu()
        {
            this.menuStash.DropDownItems.Clear();

            foreach (var stashItem in _Stash.Items)
            {
                ToolStripMenuItem item = new ToolStripMenuItem();
                item.Text = stashItem.Name;
                item.Click += new EventHandler(stash_item_Click);
                this.menuStash.DropDownItems.Add(item);
            }
        }

        void stash_item_Click(object sender, EventArgs e)
        {
            var index = menuStash.DropDownItems.IndexOf((ToolStripItem)sender);
            if (index != -1)
            {
                var stash = _Stash.Items[index];
                foreach (var item in stash.Packets)
                {
                    OnPacket(new Packet {
                        Type = item.Type,
                        SubType = item.SubType,
                        Data = item.Data,
                        Direction = item.Direction
                    });
                }
            }
        }



        private void NetworkInspector_Load(object sender, EventArgs e)
        {

        }

        private delegate void OnPacketDelegate(Packet packet);

        public void OnPacket(Packet packet)
        {
            try {
                this.BeginInvoke(new OnPacketDelegate(_OnPacket), new object[] { packet });
            }catch(Exception ex)
            {
            }
        }

        private void _OnPacket(Packet packet)
        {
            var dataItem = new RawPacketReference();
            dataItem.Packet = packet;
            dataItem.Sequence = PacketSequence;
            PacketSequence++;

            var listItem = new ListViewItem();
            if (packet.Direction == PacketDirection.OUTPUT)
            {
                listItem.ImageIndex = 0;
            }
            else
            {
                listItem.ImageIndex = 1;
            }

            listItem.Text = dataItem.Sequence + " - " + packet.GetPacketName();
            listItem.Name = Guid.NewGuid().ToString();
            Packets.Add(listItem.Name, dataItem);

            packetList.Items.Add(listItem);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            Packets.Clear();
            packetList.Items.Clear();
        }

        private void packetList_DoubleClick(object sender, EventArgs e)
        {
            foreach (ListViewItem item in packetList.SelectedItems)
            {
                ViewPacket((RawPacketReference)Packets[item.Name]);
            }
        }

        private void ViewPacket(RawPacketReference packet)
        {
            var tabPage = new TabPage();
            tabPage.Text = packet.Sequence + " - " + packet.Packet.GetPacketName();
            PacketView control = new PacketView(packet, tabPage, this, Context);
            control.Dock = DockStyle.Fill;
            tabPage.Controls.Add(control);

            this.tab.TabPages.Add(tabPage);
            this.tab.SelectedTab = tabPage;
        }

        public void Stash(params RawPacketReference[] packets)
        {
            var dialog = new StringDialog("Stash packets", "Your selected packets will be stored with the name below");
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _Stash.Add(dialog.Result.Value, packets);
            }
        }

        private void menuStash_DropDownOpening(object sender, EventArgs e)
        {
            RefreshStashMenu();
        }

        private void btnStash_Click(object sender, EventArgs e)
        {
            RawPacketReference[] packets = new RawPacketReference[packetList.SelectedItems.Count];

            for (int i = 0; i < packetList.SelectedItems.Count; i++)
            {
                var listItem = (ListViewItem)packetList.SelectedItems[i];
                var packet = Packets[listItem.Name];

                packets[i] = packet;
            }

            Stash(packets);
        }

        private void packetList_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            btnStash.Enabled = packetList.SelectedItems.Count > 0;
        }

        public IPacketLogger GetPacketLogger()
        {
            return this;
        }

        public List<ISocketServer> SocketServers = new List<ISocketServer>();

        public void AddSocketServer(ISocketServer server)
        {
            SocketServers.Add(server);
        }

        public List<ISocketSession> GetSessions()
        {
            var result = new List<ISocketSession>();
            foreach(var server in SocketServers)
            {
                result.AddRange(server.GetSocketSessions());
            }
            return result;
        }
    }


}
