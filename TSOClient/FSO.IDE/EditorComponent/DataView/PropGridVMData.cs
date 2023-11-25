using FSO.IDE.EditorComponent.UI;
using FSO.SimAntics;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Engine.Scopes;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace FSO.IDE.EditorComponent.DataView
{
    public class PropGridVMData : ICustomTypeDescriptor
    {
        EditorScope Scope;
        VMEntity Object;
        VMStackFrame Frame;
        UIBHAVEditor Editor;

        public PropGridVMData(EditorScope scope, VMEntity ent, VMStackFrame frame, UIBHAVEditor editor)
        {
            Scope = scope;
            Object = ent;
            Frame = frame;
            Editor = editor;
        }

        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        public string GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        public PropertyDescriptorCollection GetProperties()
        {
            return TypeDescriptor.GetProperties(this, true);
        }

        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            List<PropertyDescriptor> newProps = new List<PropertyDescriptor>();

            newProps.Add(new VMDataPropertyDescriptor("Stack Object ID", "The stack object.", attributes,
                VMExtDataType.StackObject, 0, Object, Frame, Editor));

            var objDat = Scope.GetVarScopeDataNames(VMVariableScope.Parameters);
            foreach (var entry in objDat)
                newProps.Add(new VMDataPropertyDescriptor(entry.Name, entry.Description, attributes,
                    VMExtDataType.Parameter, entry.Value, Object, Frame, Editor));

            objDat = Scope.GetVarScopeDataNames(VMVariableScope.Local);
            foreach (var entry in objDat)
                newProps.Add(new VMDataPropertyDescriptor(entry.Name, entry.Description, attributes,
                    VMExtDataType.Local, entry.Value, Object, Frame, Editor));

            objDat = Scope.GetVarScopeDataNames(VMVariableScope.MyObjectAttributes);
            foreach (var entry in objDat)
                newProps.Add(new VMDataPropertyDescriptor(entry.Name, entry.Description, attributes,
                    VMExtDataType.Attributes, entry.Value, Object, Frame, Editor));

            if (Object is VMAvatar)
            {
                objDat = Scope.GetVarScopeDataNames(VMVariableScope.MyPersonData);
                foreach (var entry in objDat)
                    newProps.Add(new VMDataPropertyDescriptor(entry.Name, entry.Description, attributes,
                        VMExtDataType.PersonData, entry.Value, Object, Frame, Editor));

                objDat = Scope.GetVarScopeDataNames(VMVariableScope.MyMotives);
                foreach (var entry in objDat)
                    newProps.Add(new VMDataPropertyDescriptor(entry.Name, entry.Description, attributes,
                        VMExtDataType.Motives, entry.Value, Object, Frame, Editor));
            }

            objDat = Scope.GetVarScopeDataNames(VMVariableScope.MyObject);
            foreach (var entry in objDat)
                newProps.Add(new VMDataPropertyDescriptor(entry.Name, entry.Description, attributes, 
                    VMExtDataType.ObjectData, entry.Value, Object, Frame, Editor));

            objDat = Scope.GetVarScopeDataNames(VMVariableScope.Temps);
            foreach (var entry in objDat)
                newProps.Add(new VMDataPropertyDescriptor(entry.Name, entry.Description, attributes,
                    VMExtDataType.Temp, entry.Value, Object, Frame, Editor));

            objDat = Scope.GetVarScopeDataNames(VMVariableScope.TempXL);
            foreach (var entry in objDat)
                newProps.Add(new VMDataPropertyDescriptor(entry.Name, entry.Description, attributes,
                    VMExtDataType.TempXL, entry.Value, Object, Frame, Editor));

            objDat = Scope.GetVarScopeDataNames(VMVariableScope.Global);
            foreach (var entry in objDat)
                newProps.Add(new VMDataPropertyDescriptor(entry.Name, entry.Description, attributes,
                    VMExtDataType.Globals, entry.Value, Object, Frame, Editor));

            return new PropertyDescriptorCollection(newProps.ToArray());
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }
    }
}
