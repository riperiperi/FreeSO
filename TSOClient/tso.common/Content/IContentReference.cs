namespace FSO.Common.Content
{
    public interface IContentReference <T> : IContentReference
    {
        T Get();
    }

    public interface IContentReference
    {
        object GetGeneric();
        object GetThrowawayGeneric();
    }
}
