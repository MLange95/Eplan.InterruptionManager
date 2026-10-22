using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eplan.EplApi.Base;
using Eplan.EplApi.DataModel;
using Eplan.EplApi.HEServices;

namespace Eplan.InterruptionManager
{
    public static class CounterpartPlacer
    {
        public const string MacroPath =
            @"C:\Users\Public\EPLAN\Data\Macros\Siemens AG\Circuitos logica vital\LoopInterruption.ema";

        // The macro's insertion point is 4 mm above the T-node, so insert 4 mm higher than the row line
        private const double InsertOffsetY = 4;

        public static string Place(Project project)
        {
            List<string> skipped = new List<string>();
            List<SlotItem> items = CounterpartSlots.Collect(project, skipped);

            DMObjectsFinder finder = new DMObjectsFinder(project);
            List<Page> loopPages = new List<Page>();
            foreach (Page p in finder.GetPages(new PagesFilter()))
            {
                try
                {
                    if (p.Properties.DESIGNATION_DOCTYPE.ToString().Contains("LAZOS"))
                        loopPages.Add(p);
                }
                catch
                {
                }
            }

            StringBuilder log = new StringBuilder();
            List<KeyValuePair<InterruptionPoint, SlotItem>> pending =
                new List<KeyValuePair<InterruptionPoint, SlotItem>>();
            int macros = 0;

            using (UndoStep undo = new UndoManager().CreateUndoStep())
            {
                // Pass 1: place every macro and remember the interruption inside each one
                foreach (var group in items.GroupBy(i => i.Rack + " | " + i.Potential))
                {
                    string rack = group.First().Rack;
                    string potential = group.First().Potential;

                    Page loop = FindLoopPage(loopPages, rack, potential);
                    if (loop == null)
                    {
                        log.AppendLine("No loop page found for " + group.Key);
                        ErrorLog.Add("placement " + group.Key, "no loop page found");
                        continue;
                    }

                    foreach (Slot s in CounterpartSlots.ComputeSlots(group.ToList()))
                    {
                        string where = "placement " + s.Item.Dt + " (" + group.Key + ")";
                        try
                        {
                            Insert insert = new Insert();
                            StorableObject[] inserted = insert.WindowMacro(MacroPath, 0, loop,
                                new PointD(s.X, s.Y + InsertOffsetY), Insert.MoveKind.Absolute);
                            macros++;

                            if (inserted != null)
                            {
                                foreach (StorableObject obj in inserted)
                                {
                                    InterruptionPoint cp = obj as InterruptionPoint;
                                    if (cp != null)
                                        pending.Add(new KeyValuePair<InterruptionPoint, SlotItem>(cp, s.Item));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ErrorLog.Add(where, ex.ToString());
                        }
                    }
                }

                // Pass 2: write the DTs only after everything is placed,
                // so later insertions can't blank the earlier ones
                foreach (var pair in pending)
                {
                    try
                    {
                        FunctionBasePropertyList parts = new FunctionBasePropertyList();
                        parts.DESIGNATION_LOCATION = pair.Value.Location;
                        parts.FUNC_PREFIX = pair.Value.Prefix;
                        parts.FUNC_CODE = pair.Value.Identifier;
                        parts.FUNC_COUNTER = pair.Value.Pin;
                        pair.Key.NameParts = parts;
                    }
                    catch (Exception ex)
                    {
                        ErrorLog.Add("naming " + pair.Value.Dt, ex.ToString());
                    }
                }
            }

            string dump = ErrorLog.Count > 0 ? ErrorLog.Save() : "(no errors)";

            StringBuilder result = new StringBuilder();
            result.AppendLine("Macros placed: " + macros);
            result.AppendLine("DTs written: " + pending.Count);
            result.AppendLine("Skipped (bad mounting): " + skipped.Count);
            result.Append(log.ToString());
            result.AppendLine("Errors: " + ErrorLog.Count);
            result.AppendLine("Dump: " + dump);
            return result.ToString();
        }

        private static Page FindLoopPage(List<Page> loopPages, string rack, string potential)
        {
            foreach (Page p in loopPages)
            {
                string desc = "";
                try
                {
                    desc = p.Properties[11011].ToString();
                }
                catch
                {
                }

                if (desc.Contains(rack) && desc.Contains(potential))
                    return p;
            }
            return null;
        }
    }
}