namespace FSO.LotView.Model
{
    public class WorldObjectRenderInfo
    {
        public WorldObjectRenderLayer Layer = WorldObjectRenderLayer.STATIC;
        public int DynamicCounter;
        public int DynamicRemoveCycle;
    }

    public enum WorldObjectRenderLayer
    {
        STATIC,
        DYNAMIC
    }
}
