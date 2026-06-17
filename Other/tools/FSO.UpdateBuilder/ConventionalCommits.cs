using LibGit2Sharp;
using System.Text;
using System.Text.RegularExpressions;

namespace FSO.UpdateBuilder
{
    internal enum ConventionalCommitsBump
    {
        Patch = 0,
        Minor,
        Major
    }

    internal static class ConventionalCommits
    {
        private static Regex ConventionalCommitsRegex = new Regex("^(build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test){1}(\\([\\w\\-\\.]+\\))?(!)?: (([\\w .,!&/~()-])+)([\\s\\S]*)");

        private static bool SkipType(string type)
        {
            switch (type)
            {
                case "build":
                case "chore":
                case "ci":
                case "docs":
                case "style":
                case "test":
                    return true;
            }

            return false;
        }

        private static ConventionalCommitsBump TypeBump(string type, string breaking)
        {
            ConventionalCommitsBump bump = type switch
            {
                "feat" => ConventionalCommitsBump.Minor,
                _ => ConventionalCommitsBump.Patch
            };

            if (breaking == "!")
            {
                bump += 1;
            }

            return bump;
        }

        public static void TestParse(string msg)
        {
            var results = ConventionalCommitsRegex.Match(msg);
        }

        public static bool AddToChangelog(StringBuilder changelog, ref ConventionalCommitsBump bump, Commit commit)
        {
            var results = ConventionalCommitsRegex.Match(commit.Message);

            var lines = commit.Message.Split('\n');

            if (results.Success)
            {
                var type = results.Groups[1].Value;
                var scope = results.Groups[2].Value;
                var breaking = results.Groups[3].Value;
                var message = results.Groups[4].Value;
                var description = results.Groups[6].Value;

                if (SkipType(type))
                {
                    return false;
                }

                ConventionalCommitsBump newBump = TypeBump(type, breaking);

                if (newBump > bump)
                {
                    bump = newBump;
                }
            }
            else
            {
                // Not a conventional commit. Doesn't really do anything special.
                // If it's similar to a merge commit, ignore it.

                if (lines[0].StartsWith("Merge branch "))
                {
                    return false;
                }
            }

            changelog.AppendLine($"- {lines[0]}");

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                if (line.Length == 0)
                {
                    continue;
                }

                changelog.AppendLine($"  {line}");
            }

            return true;
        }
    }
}
