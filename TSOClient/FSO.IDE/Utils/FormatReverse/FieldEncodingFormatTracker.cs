using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FSO.IDE.Utils.FormatReverse
{
    public partial class FieldEncodingFormatTracker : Form
    {
        public List<GuessedFieldEntry> Fields = new List<GuessedFieldEntry>();
        public int RepeatBaseNum;

        private Tuple<byte, byte, bool, long> BasePosition;

        // the bit reader
        public IffFieldEncode Io;

        public FieldEncodingFormatTracker()
        {
            InitializeComponent();
        }

        public void StartWithOBJM()
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "OBJM Data|*.bin";
            dialog.Title = "Select Object Module File.";
            FormsUtils.StaExecute(() =>
            {
                dialog.ShowDialog();
            });
            if (dialog.FileName == "") return;

            var stream = dialog.OpenFile();

            var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN);
            io.ReadUInt32(); //pad
            var version = io.ReadUInt32();

            var MjbO = io.ReadUInt32();

            var compressionCode = io.ReadByte();
            if (compressionCode != 1) throw new Exception("hey what!!");

            var iop = new IffFieldEncode(io);

            
            var table = new List<ushort>();
            while (io.HasMore)
            {
                var value = iop.ReadUInt16();
                if (value == 0) break;
                table.Add(value);
            }

            Io = iop;
            BasePosition = Io.MarkStream();
            RenderFields();
        }

        private void PrepareInput()
        {
            ScanFutureTop(16);
            DataList.TopIndex = DataList.Items.Count - 1;
        }

        public void ScanFutureTop(int allowedDepth)
        {
            //scan the future from the current point in the stream
            //if we run into one problem down a path, just don't follow it anymore

            //a "problem" in a path is when the compression is used "inefficiently"
            //eg. a 24 bit space is used just for the value 1
            //or a zero value is encoded in any other way than with one bit
            //the compressor wouldn't do something like this, so we can use this to guess the intended structure

            bool shortValid;
            bool shortFutureFlag = false;
            bool intValid;
            bool intFutureFlag = false;

            var save = Io.MarkStream();
            var shortResult = Io.DebugReadField(false);
            shortValid = !IsIncorrectCompressionChoice(shortResult, Io.widths);
            if (shortValid)
            {
                shortValid = ScanFutureRecursive(allowedDepth - 1);
                if (!shortValid) shortFutureFlag = true;
            }

            Io.RevertToMark(save);
            var intResult = Io.DebugReadField(true);
            intValid = !IsIncorrectCompressionChoice(intResult, Io.widths32);
            if (intValid)
            {
                intValid = ScanFutureRecursive(allowedDepth - 1);
                if (!intValid) intFutureFlag = true;
            }

            Io.RevertToMark(save);

            ShortPreview.Text = shortResult.Item1.ToString();
            ShortButton.Enabled = shortValid;

            IntPreview.Text = intResult.Item1.ToString();
            IntButton.Enabled = intValid;

            if (shortValid)
            {
                ShortErrors.Text = "No issues detected.";
            }
            else
            {
                ShortErrors.Text = "Detected issues:\n\n";
                ShortErrors.Text += (shortFutureFlag) ? "Invalid in future." : "Incorrect bit size.";
            }

            if (intValid)
            {
                IntErrors.Text = "No issues detected.";
            }
            else
            {
                IntErrors.Text = "Detected issues:\n\n";
                IntErrors.Text += (intFutureFlag) ? "Invalid in future." : "Incorrect bit size.";
            }

            var bitString = new StringBuilder();

            bitString.Append('>');

            for (int i = 0; i < 34; i++)
            {
                if (i == shortResult.Item2)
                {
                    bitString.Append('|');
                }
                if (i == intResult.Item2)
                {
                    bitString.Append(']');
                }
                bitString.Append(Io.ReadBits(1).ToString());
            }
            Io.RevertToMark(save);

            BitView.Text = bitString.ToString();
        }

        public bool ScanFutureRecursive(int remainingDepth)
        {
            if (remainingDepth == 0 || Io.StreamEnd) return true;
            //return true if at least one future path exists

            var save = Io.MarkStream();
            var shortResult = Io.DebugReadField(false);
            var shortValid = !IsIncorrectCompressionChoice(shortResult, Io.widths);
            if (shortValid)
            {
                shortValid = ScanFutureRecursive(remainingDepth - 1);
            }

            Io.RevertToMark(save);
            var intResult = Io.DebugReadField(true);
            var intValid = !IsIncorrectCompressionChoice(intResult, Io.widths32);
            if (intValid)
            {
                intValid = ScanFutureRecursive(remainingDepth - 1);
            }

            Io.RevertToMark(save);

            return shortValid || intValid;
        }

        public bool IsIncorrectCompressionChoice(Tuple<long, int> result, byte[] table)
        {
            //is there a better bit choice to compress this number?
            if (result.Item1 == 0)
            {
                if (result.Item2 != 0) return true; //zero encoded with non-zero bit width
                return false;
            }

            // otherwise we need to determine if there was a "more optimal" choice
            // what is the number of bits required to represent the result?

            var number = Math.Abs(result.Item1);
            if (result.Item1 >= 0) number += 1;
            var bits = (int)Math.Ceiling(Math.Log(number, 2)) + 1;

            var bestOption = table.FirstOrDefault(bitChoice => bitChoice >= bits);
            if (bestOption != result.Item2) return true;

            return false;
        }

        private void RepeatBase_ValueChanged(object sender, EventArgs e)
        {

        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.Title = "Save the format as a json file.";
            dialog.Filter = "JSON file format|*.json";
            dialog.DefaultExt = "json";
            dialog.AddExtension = true;
            FormsUtils.StaExecute(() =>
            {
                dialog.ShowDialog();
            });

            var save = new GuessedFormat()
            {
                RepeatBase = RepeatBaseNum,
                Fields = Fields
            };
            var json = JsonConvert.SerializeObject(save);
            File.WriteAllText(dialog.FileName, json);
        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "JSON file format|*.json";
            dialog.Title = "Select a JSON file.";
            FormsUtils.StaExecute(() =>
            {
                dialog.ShowDialog();
            });
            if (dialog.FileName == "") return;

            var json = File.ReadAllText(dialog.FileName);
            var save = JsonConvert.DeserializeObject<GuessedFormat>(json);

            Fields = save.Fields;
            RepeatBaseNum = save.RepeatBase;

            RenderFields();
        }

        private void RenderFields()
        {
            var beforeSelect = DataList.SelectedIndex;
            Io.RevertToMark(BasePosition);

            DataList.Items.Clear();
            foreach (var field in Fields)
            {
                RenderField(field);
            }

            PrepareInput();
            if (beforeSelect != -1 && DataList.Items.Count > 0)
            {
                DataList.SelectedIndex = Math.Max(0, Math.Min(Fields.Count - 1, beforeSelect));
            }
        }

        private void RenderField(GuessedFieldEntry entry)
        {
            var big = entry.FieldType == GuessedFieldType.Int;
            var result = Io.DebugReadField(big);
            entry.ReadValue = (int)result.Item1;
            entry.BitSize = result.Item2;

            var bad = IsIncorrectCompressionChoice(result, big ? Io.widths32 : Io.widths);

            DataList.Items.Add($"{ (bad ? "*" : "") }{ entry.FieldName ?? "?" }: { entry.FieldType.ToString() } = { entry.ReadValue } ({ entry.BitSize })");
        }

        private void ShortButton_Click(object sender, EventArgs e)
        {
            var field = new GuessedFieldEntry()
            {
                FieldType = GuessedFieldType.Short
            };
            Fields.Add(field);
            RenderField(field);
            PrepareInput();
        }

        private void IntButton_Click(object sender, EventArgs e)
        {
            var field = new GuessedFieldEntry()
            {
                FieldType = GuessedFieldType.Int
            };
            Fields.Add(field);
            RenderField(field);
            PrepareInput();
        }

        private void UnknownButton_Click(object sender, EventArgs e)
        {
            var field = new GuessedFieldEntry()
            {
                FieldType = GuessedFieldType.Unknown
            };
            Fields.Add(field);
            RenderField(field);
            PrepareInput();
        }

        private void LabelButton_Click(object sender, EventArgs e)
        {
            if (DataList.SelectedIndex == -1) return;
            Fields[DataList.SelectedIndex].FieldName = LabelTextBox.Text;
            RenderFields();
        }

        private void UndoButton_Click(object sender, EventArgs e)
        {
            if (DataList.SelectedIndex == -1) return;

            //remove everything including the selected index
            while (Fields.Count > DataList.SelectedIndex)
            {
                Fields.RemoveAt(DataList.SelectedIndex);
            }
            RenderFields();
        }

        private void DataList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DataList.SelectedIndex == -1)
            {
                LabelButton.Enabled = false;
                UndoButton.Enabled = false;
                return;
            }
            LabelButton.Enabled = true;
            UndoButton.Enabled = true;
            LabelTextBox.Text = Fields[DataList.SelectedIndex].FieldName ?? "";
        }
    }

    public class GuessedFormat
    {
        public int RepeatBase;
        public List<GuessedFieldEntry> Fields;
    }

    public enum GuessedFieldType
    {
        //if the value is 0 (bitless) we don't really have any way of determining the width of this field. 
        //we can guess this later in the process using repetition tools and user discrescion. (a 0 surrounded by shorts is likely to be a short, for example)
        Unknown = 0, 
        Short,
        Int
    }

    public class GuessedFieldEntry
    {
        public GuessedFieldType FieldType;
        public string FieldName;
        public int ReadValue;
        public int BitSize;
    }
}
