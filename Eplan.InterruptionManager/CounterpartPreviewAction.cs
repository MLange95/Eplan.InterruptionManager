using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Eplan.EplApi.ApplicationFramework;
using Eplan.EplApi.DataModel;
using Eplan.EplApi.HEServices;

namespace Eplan.InterruptionManager
{
    public class CounterpartPreviewAction : IEplAction
    {
        public bool OnRegister(ref string Name, ref int Ordinal)
        {
            Name = "CounterpartPreviewAction";
            Ordinal = 11;
            return true;
        }

        public bool Execute(ActionCallingContext ctx)
        {
            SelectionSet selection = new SelectionSet();
            Project project = selection.GetCurrentProject(true);
            if (project == null)
            {
                MessageBox.Show("Open a project first.", "Counterpart Preview");
                return false;
            }

            DMObjectsFinder finder = new DMObjectsFinder(project);
            Page[] pages = finder.GetPages(new PagesFilter());

            List<Page> schematicPages = new List<Page>();
            int loopPages = 0;

            foreach (Page page in pages)
            {
                string docType = "";
                try
                {
                    docType = page.Properties.DESIGNATION_DOCTYPE.ToString().Trim();
                }
                catch
                {
                    docType = "";
                }

                if (docType.Contains("ESQUEMAS"))
                    schematicPages.Add(page);
                else if (docType.Contains("LAZOS"))
                    loopPages++;
            }

            // Group schematic interruptions by rack | potential | mounting place
            Dictionary<string, int> groups = new Dictionary<string, int>();
            int total = 0;

            foreach (Page schematic in schematicPages)
            {
                InterruptionPointsFilter filter = new InterruptionPointsFilter();
                filter.Page = schematic;
                InterruptionPoint[] points = finder.GetInterruptionPoints(filter);

                foreach (InterruptionPoint ip in points)
                {
                    total++;
                    string rack = InterruptionHelper.RawValue(ip, "Siemens_AG.Function.Interruption_Location");
                    string mount = InterruptionHelper.RawValue(ip, "Siemens_AG.Function.Interruption_Mounting");
                    string potential = ReadPotential(ip);

                    string row;
                    int col;
                    string cell = MountingParser.TryParse(mount, out row, out col) ? row + col : "(bad: " + mount + ")";
                    string key = rack + " | " + potential + " | " + cell;
                    if (groups.ContainsKey(key))
                        groups[key]++;
                    else
                        groups[key] = 1;
                }
            }

            StringBuilder report = new StringBuilder();
            report.AppendLine("Schematic pages: " + schematicPages.Count);
            report.AppendLine("Loop pages: " + loopPages);
            report.AppendLine("Interruptions: " + total);
            report.AppendLine();
            report.AppendLine("Rack | Potential | Mounting : count");

            foreach (KeyValuePair<string, int> pair in groups)
                report.AppendLine(pair.Key + " : " + pair.Value);

            MessageBox.Show(report.ToString(), "Counterpart Preview");
            return true;
        }

        private static string ReadPotential(InterruptionPoint ip)
        {
            try
            {
                string value = ip.Properties.INTERRUPTIONPOINT_DESCRIPTIONAUTOMATIC.ToString().Trim();
                int at = value.LastIndexOf('@');
                if (at >= 0)
                    value = value.Substring(at + 1);
                value = value.Trim().TrimEnd(';').Trim();
                return string.IsNullOrEmpty(value) ? "(empty)" : value;
            }
            catch
            {
                return "(error)";
            }
        }

        public void GetActionProperties(ref ActionProperties actionProps)
        {
        }
    }
}