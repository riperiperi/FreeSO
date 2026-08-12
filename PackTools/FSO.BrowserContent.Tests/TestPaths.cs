namespace FSO.BrowserContent.Tests;

internal static class TestPaths
{
    /// <summary>
    /// Resolves PackTools/examples regardless of cwd (repo root, PackTools, or bin/).
    /// </summary>
    public static string ExamplesDirectory
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "PackTools", "examples");
                if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "pet-rock.json")))
                    return candidate;

                candidate = Path.Combine(dir.FullName, "examples");
                if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "pet-rock.json")))
                    return candidate;

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException(
                "Could not locate PackTools/examples/pet-rock.json from " + AppContext.BaseDirectory);
        }
    }

    public static string PetRockJson => Path.Combine(ExamplesDirectory, "pet-rock.json");
}
