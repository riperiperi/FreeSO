﻿using FSO.SimAntics.Engine;

namespace FSO.IDE.EditorComponent.OperandForms.DataProviders
{
    public abstract class OpValueBoundsProvider : OpDataProvider
    {
        public abstract int[] GetBounds(EditorScope scope, VMPrimitiveOperand op);
    }

    public class OpStaticValueBoundsProvider : OpValueBoundsProvider
    {
        int[] Bounds;
        public OpStaticValueBoundsProvider(int min, int max)
        {
            Bounds = new int[] { min, max };
        }

        public override int[] GetBounds(EditorScope scope, VMPrimitiveOperand op)
        {
            return Bounds;
        }
    }
}
