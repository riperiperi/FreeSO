This project now uses a StartupPath variable, in order to minimize the project's total size. It means that
all data has been deleted from the 'bin\Debug' folder, and is loaded from the TSO game folder instead. In order
to change the path, one must change the line;

            //This should ideally be stored in the Windows Registry...
            GlobalSettings.Default.StartupPath = "C:\\Program Files\\Maxis\\The Sims Online\\TSOClient\\";

These are lines 70 and 71 in 'Game1.cs'.

This project also relies on SimsLib. It was deleted from the project's folder, to save space. It already exists in
'Other\tools\'. It is no longer included in the solution, so if you make any changes to SimsLib, you need to
recompile!