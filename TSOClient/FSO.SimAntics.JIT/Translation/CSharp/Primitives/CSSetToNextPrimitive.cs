using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.JIT.Translation.CSharp.Engine;
using FSO.SimAntics.JIT.Translation.Model;
using FSO.SimAntics.JIT.Translation.Primitives;
using FSO.SimAntics.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.JIT.Translation.CSharp.Primitives
{
    public class CSSetToNextPrimitive : AbstractTranslationPrimitive
    {
        public CSSetToNextPrimitive(BHAVInstruction instruction, byte index) : base(instruction, index)
        {
        }

        public override bool CanYield => false;
        public override PrimitiveReturnType ReturnType => PrimitiveReturnType.NativeStatementTrueFalse;

        public override List<string> CodeGen(TranslationContext context)
        {
            var csContext = (CSTranslationContext)context;
            var csClass = csContext.CurrentClass;
            var codeResult = new List<string>();
            var operand = GetOperand<VMSetToNextOperand>();

            if (operand.SearchType == VMSetToNextSearchType.ClosestHouse) return Line($"_bResult = false; //set to next closest house not yet impl");

            codeResult.Add($"{{ //set to next ({operand.SearchType.ToString()})");
            codeResult.Add($"var targetValue = {CSScopeMemory.GetExpression(csContext, operand.TargetOwner, operand.TargetData, false)};");

            //re-evaluation of what this actually does:
            //tries to find the next object id (from the previous) that meets a specific condition.
            //the previous object id is supplied via the target variable
            //
            //we should take the first result with object id > targetValue.

            if (operand.SearchType == VMSetToNextSearchType.PartOfAMultipartTile)
            {
                codeResult.Add($"var objPointer = context.VM.GetObjectById(targetValue);");
                codeResult.Add($"var result = VMSetToNext.MultitilePart(context, objPointer, targetValue);");
                codeResult.Add($"_bResult = result != 0;");
                codeResult.Add($"if (_bResult) {CSScopeMemory.SetStatement(csContext, operand.TargetOwner, operand.TargetData, "=", "result", false)}");
                codeResult.Add($"}}");
            }
            else if (operand.SearchType == VMSetToNextSearchType.ObjectAdjacentToObjectInLocal)
            {
                codeResult.Add($"var objPointer = context.VM.GetObjectById(targetValue);");
                codeResult.Add($"var result = VMSetToNext.AdjToLocal(context, objPointer, {operand.Local});");
                codeResult.Add($"_bResult = result != null;");
                codeResult.Add($"if (_bResult) {CSScopeMemory.SetStatement(csContext, operand.TargetOwner, operand.TargetData, "=", "result.ObjectID", false)}");
                codeResult.Add($"}}");
            }
            else if (operand.SearchType == VMSetToNextSearchType.Career)
            {
                codeResult.Add($"var result = Content.Content.Get().Jobs.SetToNext(targetValue);");
                codeResult.Add($"_bResult = result >= 0;");
                codeResult.Add($"if (_bResult) {CSScopeMemory.SetStatement(csContext, operand.TargetOwner, operand.TargetData, "=", "result", false)}");
                codeResult.Add($"}}");
            }
            else if (operand.SearchType == VMSetToNextSearchType.NeighborId)
            {
                codeResult.Add($"var result = Content.Content.Get().Neighborhood.SetToNext(targetValue);");
                codeResult.Add($"_bResult = result >= 0;");
                codeResult.Add($"if (_bResult) {CSScopeMemory.SetStatement(csContext, operand.TargetOwner, operand.TargetData, "=", "result", false)};");
                codeResult.Add($"}}");
            }
            else if (operand.SearchType == VMSetToNextSearchType.NeighborOfType)
            {
                codeResult.Add($"var result = Content.Content.Get().Neighborhood.SetToNext(targetValue, {operand.GUID});");
                codeResult.Add($"_bResult = result >= 0;");
                codeResult.Add($"if (_bResult) {CSScopeMemory.SetStatement(csContext, operand.TargetOwner, operand.TargetData, "=", "result", false)};");
                codeResult.Add($"}}");
            }
            else
            {

                //if we've cached the search type, use that instead of all objects
                switch (operand.SearchType)
                {
                    case VMSetToNextSearchType.ObjectOnSameTile:
                        codeResult.Add($"var objPointer = context.VM.GetObjectById(targetValue) ?? context.Caller;");
                        codeResult.Add($"var entities = context.VM.Context.ObjectQueries.GetObjectsAt(objPointer.Position);");
                        break;
                    case VMSetToNextSearchType.Person:
                    case VMSetToNextSearchType.FamilyMember:
                        codeResult.Add($"var entities = context.VM.Context.ObjectQueries.Avatars;");
                        break;
                    case VMSetToNextSearchType.ObjectOfType:
                        codeResult.Add($"var entities = context.VM.Context.ObjectQueries.GetObjectsByGUID({operand.GUID});");
                        break;
                    case VMSetToNextSearchType.ObjectWithCategoryEqualToSP0:
                        csClass.UseParams = true;
                        codeResult.Add($"var entities = context.VM.Context.ObjectQueries.GetObjectsByCategory(args[0]);");
                        break;
                    default:
                        codeResult.Add($"var entities = context.VM.Entities;");
                        break;
                }
                codeResult.Add($"_bResult = false;");
                codeResult.Add($"if (entities != null) {{");

                bool loop = (operand.SearchType == VMSetToNextSearchType.ObjectOnSameTile);

                codeResult.Add($"var ind = (entities.Count < 4)?0:VM.FindNextIndexInObjList(entities, targetValue);");
                codeResult.Add($"for (int i = ind; i < entities.Count; i++) {{");
                codeResult.Add($"var tempObj = entities[i];");

                string found = null;
                switch (operand.SearchType)
                { //manual search types
                    case VMSetToNextSearchType.NonPerson:
                        found = "tempObj is VMGameObject";
                        break;
                    case VMSetToNextSearchType.FamilyMember:
                        found = "(context.VM.TS1State.CurrentFamily?.FamilyGUIDs?.Contains(((VMAvatar)tempObj).Object.OBJ.GUID) ?? false)";
                        break;
                    default:
                        //set to next object, or cached search.
                        break;
                }

                if (found != null) codeResult.Add($"if (tempObj.ObjectID > targetValue && {found}) {{");
                else codeResult.Add($"if (tempObj.ObjectID > targetValue) {{");
                codeResult.Add($"{CSScopeMemory.SetStatement(csContext, operand.TargetOwner, operand.TargetData, "=", "tempObj.ObjectID", false)}");
                codeResult.Add($"_bResult = true; break;");
                codeResult.Add($"}}"); //end if

                codeResult.Add($"}}"); //end loop
                
                if (loop)
                {
                    codeResult.Add($"if (!_bResult) {{");
                    codeResult.Add($"VMEntity first = entities.FirstOrDefault();");
                    codeResult.Add($"_bResult = first != null && entities.Contains(objPointer);");
                    codeResult.Add($"if (_bResult) {CSScopeMemory.SetStatement(csContext, operand.TargetOwner, operand.TargetData, "=", "first.ObjectID", false)}");
                    codeResult.Add($"}}");
                    //loop around
                }

                codeResult.Add($"}}"); //end "entities != null"

                codeResult.Add($"}} //end set to next"); //end primitive scope
            }

            return codeResult;
        }
    }
}
