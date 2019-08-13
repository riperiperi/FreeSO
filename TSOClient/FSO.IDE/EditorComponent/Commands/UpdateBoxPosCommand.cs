using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.UI;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.IDE.EditorComponent.Commands
{
    public class UpdateBoxPosCommand : BHAVCommand
    {
        PrimitiveBox Box;
        public Point BeforePos;
        public Point BeforeSize;
        public Point AfterPos;
        public Point AfterSize;

        public UpdateBoxPosCommand(PrimitiveBox box)
        {
            Box = box;
            BeforePos = new Point(box.TreeBox.X, box.TreeBox.Y);
            BeforeSize = new Point(box.TreeBox.Width, box.TreeBox.Height);
            AfterPos = box.Position.ToPoint();
            AfterSize = new Point(box.Width, box.Height);
        }

        public override void Execute(BHAV bhav, UIBHAVEditor editor)
        {
            var tree = editor.GetSavableTree();
            Box.TreeBox.X = (short)AfterPos.X;
            Box.TreeBox.Y = (short)AfterPos.Y;
            Box.TreeBox.Width = (short)AfterSize.X;
            Box.TreeBox.Height = (short)AfterSize.Y;
            Content.Content.Get().Changes.ChunkChanged(tree);
        }

        public override void Undo(BHAV bhav, UIBHAVEditor editor)
        {
            var tree = editor.GetSavableTree();
            Box.TreeBox.X = (short)BeforePos.X;
            Box.TreeBox.Y = (short)BeforePos.Y;
            Box.TreeBox.Width = (short)BeforeSize.X;
            Box.TreeBox.Height = (short)BeforeSize.Y;
            Box.ApplyBoxPositionCentered();
            Content.Content.Get().Changes.ChunkChanged(tree);
        }
    }
}
