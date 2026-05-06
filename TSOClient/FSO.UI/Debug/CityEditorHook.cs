namespace FSO.Client.Debug
{
    /// <summary>
    /// Set by FSO.CityEditor at boot so the running client knows to enter
    /// city-editor mode (skip login, jump straight to a city, auto-enable
    /// the map painter). Mirrors the IDEHook pattern used by Volcanic.
    /// </summary>
    public static class CityEditorHook
    {
        public static ICityEditorInjector Editor;

        public static void SetEditor(ICityEditorInjector editor)
        {
            Editor = editor;
        }

        public static bool IsActive => Editor != null;
    }

    public interface ICityEditorInjector
    {
        // Called once the city is loaded and the renderer is ready.
        // Future commits will plug tool-palette / save events through here.
        void OnCityReady();
    }
}