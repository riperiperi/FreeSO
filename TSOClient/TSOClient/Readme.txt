This project now uses a StartupPath variable, in order to minimize the project's total size. It means that
all data has been deleted from the 'bin\Debug' folder, and is loaded from the TSO game folder instead. In order
to change the path, one must change the line;

            //This should ideally be stored in the Windows Registry...
            GlobalSettings.Default.StartupPath = "C:\\Program Files\\Maxis\\The Sims Online\\TSOClient\\";

These are lines 70 and 71 in 'Game1.cs'.

This project also relies on SimsLib. It was deleted from the project's folder, to save space. It already exists in
'Other\tools\'.
In addition to SimsLib, this project relies on Bass.NET. It has to be installed from Bass24.Net.zip in order for the project to compile.
Then in order to debug the project (run it), bass.dll has to be extracted from bass24.zip to bin\Debug, in addition to copying over the Lua scripts from the LuaScripts folder.