using System.Text.RegularExpressions;

namespace Eplan.InterruptionManager
{
    public static class MountingParser
    {
        // "D5" -> row "D", column 5. Returns false if the format isn't recognized.
        public static bool TryParse(string mounting, out string row, out int column)
        {
            row = null;
            column = 0;

            if (string.IsNullOrEmpty(mounting))
                return false;

            Match m = Regex.Match(mounting.Trim().ToUpperInvariant(), @"^([A-J])(\d{1,2})$");
            if (!m.Success)
                return false;

            row = m.Groups[1].Value;
            column = int.Parse(m.Groups[2].Value);
            return column >= 1 && column <= 9;
        }
    }
}