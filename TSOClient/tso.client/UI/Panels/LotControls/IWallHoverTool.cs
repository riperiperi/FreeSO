namespace FSO.Client.UI.Panels.LotControls
{
    /// Build-mode tool that highlights a wall under the cursor.
    public interface IWallHoverTool
    {
        ArchitectureHit Hover { get; }
    }
}
