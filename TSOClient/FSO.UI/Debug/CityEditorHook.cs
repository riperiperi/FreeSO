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

        /// <summary>
        /// Optional absolute path to a city directory containing the seven
        /// PNG layers. When set, the editor screen loads from here instead
        /// of the default city_0100 (Alphaville). Set by FSO.CityEditor's
        /// Program.cs from a CLI arg before the screen initializes.
        /// </summary>
        public static string RequestedCityPath;

        /// <summary>
        /// Optional explicit city ID to load. Defaults to 100 (Alphaville)
        /// when RequestedCityPath is null.
        /// </summary>
        public static int RequestedCityId = 100;

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