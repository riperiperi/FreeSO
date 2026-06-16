namespace FSO.UpdateBuilder
{
    struct ParsedVersion(int major, int minor, int patch, string suffix)
    {
        public readonly int Major = major;
        public readonly int Minor = minor;
        public readonly int Patch = patch;
        public readonly string Suffix = suffix;

        public readonly ParsedVersion Next(int majorTarget, ConventionalCommitsBump bump)
        {
            if (majorTarget > Major)
            {
                return new ParsedVersion(majorTarget, 0, 0, Suffix);
            }
            else if (bump == ConventionalCommitsBump.Major)
            {
                return new ParsedVersion(Major + 1, 0, 0, Suffix);
            }
            else if (bump == ConventionalCommitsBump.Minor)
            {
                return new ParsedVersion(Major, Minor + 1, 0, Suffix);
            }
            else
            {
                return new ParsedVersion(Major, Minor, Patch + 1, Suffix);
            }
        }

        public readonly ParsedVersion WithSuffix(string suffix)
        {
            return new ParsedVersion(Major, Minor, Patch, suffix);
        }

        public static ParsedVersion? Parse(string text)
        {
            // Format v1.2.3 or v1.2.3-suffix

            if (text[0] == 'v')
            {
                var dotSplit = text.Substring(1).Split('.');

                if (dotSplit.Length == 3)
                {
                    if (int.TryParse(dotSplit[0], out int major) && int.TryParse(dotSplit[1], out int minor))
                    {
                        string patchString = dotSplit[2];
                        int dashIndex = patchString.IndexOf('-');

                        int patch;
                        string suffix;
                        if (dashIndex == -1)
                        {
                            if (!int.TryParse(patchString, out patch))
                            {
                                return null;
                            }

                            suffix = "";
                        }
                        else
                        {
                            if (!int.TryParse(patchString.Substring(0, dashIndex), out patch))
                            {
                                return null;
                            }

                            suffix = patchString.Substring(dashIndex + 1);
                        }

                        return new ParsedVersion(major, minor, patch, suffix);
                    }
                }
            }

            return null;
        }

        public readonly override string ToString()
        {
            return Suffix.Length > 0 ? $"v{Major}.{Minor}.{Patch}-{Suffix}" : $"v{Major}.{Minor}.{Patch}";
        }
    }
}
