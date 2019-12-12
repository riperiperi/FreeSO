using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.UI;

namespace FSO.IDE.EditorComponent.Commands
{
    public class CommentModifyCommand : BHAVCommand
    {
        public PrimitiveBox Box;
        public string OldComment;
        public string NewComment;

        public CommentModifyCommand(PrimitiveBox box, string value)
        {
            Box = box;
            OldComment = box.TreeBox.Comment;
            NewComment = value;
        }

        public void NotifyGotos()
        {
            foreach (var prim in Box.Master.Primitives)
            {
                if (prim.Type == TREEBoxType.Goto)
                {
                    if (prim.TreeBox.TruePointer == Box.TreeBox.InternalID) prim.UpdateGotoLabel();
                }
            }
        }

        public override void Execute(BHAV bhav, UIBHAVEditor editor)
        {
            var tree = editor.GetSavableTree();
            Box.SetComment(NewComment);
            Box.ApplyBoxPositionCentered();
            NotifyGotos();
            Content.Content.Get().Changes.ChunkChanged(tree);
        }

        public override void Undo(BHAV bhav, UIBHAVEditor editor)
        {
            var tree = editor.GetSavableTree();
            Box.SetComment(OldComment);
            Box.ApplyBoxPositionCentered();
            NotifyGotos();
            Content.Content.Get().Changes.ChunkChanged(tree);
        }
    }
}
