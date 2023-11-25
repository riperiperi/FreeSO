namespace FSO.Debug.Content.Preview
{
    interface IContentPreview
    {
        bool CanPreview(object value);
        void Preview(object value);
    }
}
