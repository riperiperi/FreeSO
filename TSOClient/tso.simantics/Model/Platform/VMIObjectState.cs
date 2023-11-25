namespace FSO.SimAntics.Model.Platform
{
    public interface VMIObjectState
    {
        ushort Wear { get; set; }
        void ProcessQTRDay(VM vm, VMEntity owner);
    }
}
