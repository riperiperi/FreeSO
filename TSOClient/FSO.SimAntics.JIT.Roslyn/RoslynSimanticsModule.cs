using FSO.Content;
using FSO.Files.Formats.IFF;
using FSO.SimAntics.JIT.Runtime;
using FSO.SimAntics.JIT.Translation.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.SimAntics.JIT.Roslyn
{
    public class RoslynSimanticsModule
    {
        public static string[] ScriptReferenceNames = new string[]
        {
            "FSO.Common.dll",
            "FSO.Files.dll",
            "FSO.Content.dll",
            "FSO.HIT.dll",
            "FSO.LotView.dll",
            "FSO.SimAntics.dll",
            "FSO.SimAntics.JIT.dll",
            "FSO.Vitaboy.dll",
            "FSO.Vitaboy.Engine.dll",

            typeof(object).Assembly.Location, //mscorlib
            typeof(LinkedList<string>).Assembly.Location, //System
            typeof(System.Linq.Enumerable).Assembly.Location, //System.Core
            /*
            "Microsoft.CSharp",
            "System",
            "System.Core",
            "System.Data",
            "System.Data.DataSetExtensions",
            "System.Net.Http",
            "System.Xml",
            "System.Xml.Linq"
            */
        };

        private static List<MetadataReference> ScriptReferences = new List<MetadataReference>();
        private static bool LoadedRefs;

        public static List<MetadataReference> GetReferences()
        {
            lock (ScriptReferences)
            {
                if (!LoadedRefs)
                {
                    ScriptReferences.AddRange(ScriptReferenceNames.Select(name => MetadataReference.CreateFromFile(name)));
                    LoadedRefs = true;
                }
                return ScriptReferences;
            }
        }

        public event Action OnReady;
        public RoslynSimanticsContext Context;
        public GameIffResource File;
        public SimAnticsModule Module;
        public Task<SimAnticsModule> ModuleTask;
        public string Name;
        public string FilePath;

        private bool IsGlobal;
        private bool IsSemiGlobal;

        public RoslynSimanticsModule(RoslynSimanticsContext context, GameIffResource file)
        {
            Context = context;
            File = file;
            if (file is GameGlobalResource)
            {
                if (file.MainIff.Filename == "global.iff") IsGlobal = true;
                else IsSemiGlobal = true;
            }
            Name = CSTranslationContext.FormatName(File.MainIff.Filename.Replace(".iff", ""));
            FilePath = Path.Combine(Context.CacheDirectory, Name);
        }

        public Task<SimAnticsModule> GetModuleAsync()
        {
            if (ModuleTask != null) return ModuleTask;
            lock (this)
            {
                if (ModuleTask != null) return ModuleTask;
                //make the task
                ModuleTask = Task.Run(CompileOrLoad);
                return ModuleTask;
            }
        }

        public async Task<SimAnticsModule> CompileOrLoad()
        {
            var path = FilePath + ".dll";
            try
            {
                Module = FindModuleInAssembly(Assembly.LoadFrom(Path.GetFullPath(path)));
            }
            catch
            {
                // could not load module, try compile it.
                Module = await CompileModule();
            }
            if (Module == null) { }
            return Module;
        }

        public SimAnticsModule FindModuleInAssembly(Assembly assembly)
        {
            var modules = assembly.GetTypes().Where(t => t.IsClass && t.IsSubclassOf(typeof(SimAnticsModule)));
            if (modules.Count() != 1) throw new Exception($"SimAntics JIT modules should be one per assembly. This assembly has {modules.Count()}.");
            var module = modules.First();
            var inst = (SimAnticsModule)Activator.CreateInstance(module);

            //ensure this module is not out of date
            if (inst.JITVersion != CSTranslator.JITVersion)
                throw new Exception($"Module outdated - expected {CSTranslator.JITVersion}, got {inst.JITVersion}.");
            if (inst.SourceHash != File.MainIff.ExecutableHash)
                throw new Exception($"Object executable hash mismatch - expected {File.MainIff.ExecutableHash}, got {inst.SourceHash}.");
            var semiHash = File.SemiGlobal?.MainIff?.ExecutableHash ?? 0;
            if (inst.SourceSemiglobalHash != semiHash)
                throw new Exception($"Semiglobal executable hash mismatch - expected {semiHash}, got {inst.SourceSemiglobalHash}.");
            var globHash = Content.Content.Get().WorldObjectGlobals.Get("global").Resource.MainIff.ExecutableHash;
            if (inst.SourceGlobalHash != globHash)
                throw new Exception($"Global executable hash mismatch - expected {globHash}, got {inst.SourceGlobalHash}.");

            inst.Init();
            return inst;
        }
        
        public async Task<SimAnticsModule> CompileModule()
        {
            var translator = new CSTranslator();
            var objIff = File.MainIff;
            var refs = GetReferences().ToList();

            if (!IsGlobal)
            {
                var global = await Context.GetGlobal();
                translator.Context.GlobalRes = global.File;
                translator.Context.GlobalModule = global.Module;
                refs.Add(MetadataReference.CreateFromFile(global.FilePath + ".dll"));
            } else {
                translator.Context.GlobalRes = File;
            }

            var sg = File.SemiGlobal;
            if (sg != null)
            {
                var sgModule = await Context.GetSemiglobal(sg);
                translator.Context.SemiGlobalRes = sg;
                translator.Context.SemiGlobalModule = sgModule.Module;
                refs.Add(MetadataReference.CreateFromFile(sgModule.FilePath + ".dll"));
            }

            translator.Context.ObjectRes = File;

            // create the cs source
            Console.WriteLine($"Translating {objIff.Filename}");
            var objText = translator.TranslateIff(objIff);

            // compile it into an assembly with roslyn
            var options = new CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: Context.Debug ? OptimizationLevel.Debug : OptimizationLevel.Release,
                moduleName: translator.Context.NamespaceName,
                platform: Platform.AnyCpu,
                warningLevel: 0);

            if (Context.Debug)
            {
                using (var files = System.IO.File.Open(FilePath + ".cs", System.IO.FileMode.Create))
                {
                    using (var writer = new System.IO.StreamWriter(files))
                    {
                        writer.Write(objText);
                    }
                }
            }
            var file = CSharpSyntaxTree.ParseText(objText, new CSharpParseOptions(), Path.GetFullPath(FilePath + ".cs"), Encoding.UTF8);

            var compiler = CSharpCompilation.Create(translator.Context.NamespaceName, options: options, references: refs, syntaxTrees: new List<SyntaxTree>() { file });
            // save the assembly to disk for later use
            var emitResult = compiler.Emit(FilePath + ".dll", Context.Debug ? (FilePath + ".pdb") : null, Context.Debug ? (FilePath + ".xml") : null);
            
            // load the assembly
            if (!emitResult.Success) return null;
            try
            {
                var assembly = Assembly.LoadFile(Path.GetFullPath(FilePath + ".dll"));
                return FindModuleInAssembly(assembly);
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
