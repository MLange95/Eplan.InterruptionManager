using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Eplan.InterruptionManager
{
    public static class ErrorLog
    {
        private static readonly List<string> Entries = new List<string>();

        public static void Clear()
        {
            Entries.Clear();
        }

        public static void Add(string where, string message)
        {
            Entries.Add(DateTime.Now.ToString("HH:mm:ss") + " | " + where + " | " + message);
        }

        public static int Count
        {
            get { return Entries.Count; }
        }

        public static string Save()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "InterruptionManager_dump.txt");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Interruption Manager - error dump " + DateTime.Now);
            sb.AppendLine("Errors: " + Entries.Count);
            sb.AppendLine();
            foreach (string e in Entries)
                sb.AppendLine(e);

            File.WriteAllText(path, sb.ToString());
            return path;
        }
    }
}