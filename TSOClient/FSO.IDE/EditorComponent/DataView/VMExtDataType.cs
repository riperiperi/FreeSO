namespace FSO.IDE.EditorComponent.DataView
{
    public enum VMExtDataType : byte
    {
        StackObject, //object + stack frame
        Parameter, //object + stack frame
        Local, //object + stack frame
        Attributes, //object
        Temp, //object
        TempXL, //object
        ObjectData, //object
        PersonData, //object
        Motives, //object
        Globals
    }
}
