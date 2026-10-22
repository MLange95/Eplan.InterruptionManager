using System;
using System.Collections.Generic;
using Eplan.EplApi.DataModel;
using Eplan.EplApi.HEServices;

namespace Eplan.InterruptionManager
{
    public static class DtSync
    {
        public static string Run(Project project)
        {
            ErrorLog.Clear();

            DMObjectsFinder finder = new DMObjectsFinder(project);
            Page[] pages = finder.GetPages(new PagesFilter());

            List<Page> schematicPages = new List<Page>();
            int loopCount = 0;

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
                    loopCount++;
            }

            if (schematicPages.Count == 0 || loopCount == 0)
            {
                string warning = "Interruption Manager stopped, nothing was written.\n\n";
                if (schematicPages.Count == 0)
                    warning += "No &ESQUEMAS pages found.\n";
                if (loopCount == 0)
                    warning += "No &LAZOS pages found.\n";
                return warning;
            }

            int found = 0;
            int written = 0;
            int pageNumber = 0;

            using (UndoStep undo = new UndoManager().CreateUndoStep())
            {
                foreach (Page schematic in schematicPages)
                {
                    pageNumber++;
                    InterruptionPointsFilter filter = new InterruptionPointsFilter();
                    filter.Page = schematic;
                    InterruptionPoint[] points = finder.GetInterruptionPoints(filter);

                    int index = 0;
                    foreach (InterruptionPoint ip in points)
                    {
                        found++;
                        index++;
                        string where = "schematic page " + pageNumber + ", interruption " + index;

                        try
                        {
                            InterruptionHelper.UpdateInterruptionPinProperty(ip);
                            if (InterruptionHelper.WriteDt(ip))
                                written++;
                            else
                                ErrorLog.Add(where, InterruptionHelper.LastError);
                        }
                        catch (Exception ex)
                        {
                            ErrorLog.Add(where, ex.ToString());
                        }
                    }
                }
            }

            string dump = ErrorLog.Count > 0 ? ErrorLog.Save() : "(no errors)";

            return "Schematic pages: " + schematicPages.Count +
                "\nLoop pages: " + loopCount +
                "\nInterruption points found: " + found +
                "\nDT written: " + written +
                "\nErrors: " + ErrorLog.Count +
                "\nDump: " + dump;
        }
    }
}