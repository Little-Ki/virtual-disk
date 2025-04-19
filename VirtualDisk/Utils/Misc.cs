using System.Text.RegularExpressions;

namespace VirtualDisk.Utils
{
    public static class Misc
    {
        public static string? Match(string input, string pattern, int index, int in_group)
        {
            var match = Regex.Matches(input, pattern);

            if (match.Count < index + 1)
            {
                return null;
            }

            if (match[index].Groups.Count < in_group + 1)
            {
                return null;
            }

            return match[index].Groups[in_group].Value;
        }
    }
}
