using FSO.Files.Utils;
using System.IO;

namespace FSO.Files.Formats.IFF.Chunks
{
    public class SIMI : IffChunk
    {
        public uint Version;
        public short[] GlobalData;
        
        public short Unknown1;
        public int Unknown2;
        public int Unknown3;
        public int GUID1;
        public int GUID2;
        public int Unknown4;
        public int LotValue;
        public int ObjectsValue;
        public int ArchitectureValue;

        public SIMIBudgetDay[] BudgetDays;

        public int PurchaseValue 
        {
            get
            {
                return LotValue + ObjectsValue + (ArchitectureValue * 7) / 10;
            }
        }

        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.ReadUInt32(); //pad
                Version = io.ReadUInt32();
                string magic = io.ReadCString(4);
                var items = (Version > 0x3F) ? 0x40 : 0x20;

                GlobalData = new short[38];

                for (int i=0; i<items; i++)
                {
                    var dat = io.ReadInt16();
                    if (i < GlobalData.Length)
                        GlobalData[i] = dat;
                }

                Unknown1 = io.ReadInt16();
                Unknown2 = io.ReadInt32();
                Unknown3 = io.ReadInt32();
                GUID1 = io.ReadInt32();
                GUID2 = io.ReadInt32();
                Unknown4 = io.ReadInt32();
                LotValue = io.ReadInt32();
                ObjectsValue = io.ReadInt32();
                ArchitectureValue = io.ReadInt32();

                //short Unknown1 (0x7E1E, 0x702B)
                //int Unknown2 (2 on house 1, 1 on house 66)
                //int Unknown3 (0)
                //int GUID1
                //int GUID2 (changes on bulldoze)
                //int Unknown4 (0)
                //int LotValue
                //int ObjectsValue
                //int ArchitectureValue 

                //the sims tracked a sim's budget over the past few days of gameplay.
                //this drove the budget window, which never actually came of much use to anyone ever.

                BudgetDays = new SIMIBudgetDay[6];
                for (int i=0; i<6; i++)
                {
                    BudgetDays[i] = new SIMIBudgetDay(io);
                }
            }
        }

        public override bool Write(IffFile iff, Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.WriteInt32(0);
                io.WriteInt32(0x3E);
                io.WriteCString("IMIS", 4);
                var items = (Version > 0x3E) ? 0x40 : 0x20;

                GlobalData = new short[38];

                for (int i = 0; i < items; i++)
                {
                    if (i < GlobalData.Length)
                        io.WriteInt16(GlobalData[i]);
                    else
                        io.WriteInt16(0);
                }

                io.WriteInt16(Unknown1);
                io.WriteInt32(Unknown2);
                io.WriteInt32(Unknown3);
                io.WriteInt32(GUID1);
                io.WriteInt32(GUID2);
                io.WriteInt32(Unknown4);
                io.WriteInt32(LotValue);
                io.WriteInt32(ObjectsValue);
                io.WriteInt32(ArchitectureValue);

                for (int i = 0; i < 6; i++)
                {
                    BudgetDays[i].Write(io);
                }
            }
            return true;
        }

        public class SIMIBudgetDay
        {
            public int Valid;
            public int MiscIncome;
            public int JobIncome;

            public int ServiceExpense;
            public int FoodExpense;
            public int BillsExpense;

            public int MiscExpense;
            public int HouseholdExpense;
            public int ArchitectureExpense;

            public SIMIBudgetDay()
            {

            }

            public SIMIBudgetDay(IoBuffer io)
            {
                Valid = io.ReadInt32();
                if (Valid == 0) return;
                MiscIncome = io.ReadInt32();
                JobIncome = io.ReadInt32();
                ServiceExpense = io.ReadInt32();
                FoodExpense = io.ReadInt32();
                BillsExpense = io.ReadInt32();
                MiscExpense = io.ReadInt32();
                HouseholdExpense = io.ReadInt32();
                ArchitectureExpense = io.ReadInt32();
            }

            public void Write(IoWriter io)
            {
                io.WriteInt32(Valid);
                if (Valid == 0) return;
                io.WriteInt32(MiscIncome);
                io.WriteInt32(JobIncome);
                io.WriteInt32(ServiceExpense);
                io.WriteInt32(FoodExpense);
                io.WriteInt32(BillsExpense);
                io.WriteInt32(MiscIncome);
                io.WriteInt32(HouseholdExpense);
                io.WriteInt32(ArchitectureExpense);
            }
        }
    }
}
