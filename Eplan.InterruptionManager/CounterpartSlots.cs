using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Eplan.EplApi.DataModel;
using Eplan.EplApi.HEServices;

namespace Eplan.InterruptionManager
{
    public class SlotItem
    {
        public string Dt;
        public string Rack;
        public string Potential;
        public char Row;
        public int Column;
        public string Location;
        public string Prefix;
        public string Identifier;
        public string Pin;
    }

    public class Slot
    {
        public SlotItem Item;
        public double X;
        public double Y;
    }

    public static class CounterpartSlots
    {
        // Column X positions measured on the loop frame (mm)
        private static readonly Dictionary<int, double> ColumnX = new Dictionary<int, double>
        {
            {1, 88}, {2, 148}, {3, 208}, {4, 268}, {5, 324}, {6, 388}, {7, 444}, {8, 508}, {9, 568}
        };

        // Row Y positions measured on the loop frame (mm). Rows go from A to I.
        private static readonly Dictionary<char, double> RowY = new Dictionary<char, double>
        {
            {'I', 344}, {'H', 320}, {'G', 296}, {'F', 272}, {'E', 248},
            {'D', 224}, {'C', 200}, {'B', 176}, {'A', 152}
        };

        public static List<SlotItem> Collect(Project project, List<string> skipped)
        {
            DMObjectsFinder finder = new DMObjectsFinder(project);
            Page[] pages = finder.GetPages(new PagesFilter());
            List<SlotItem> items = new List<SlotItem>();

            foreach (Page page in pages)
            {
                string docType = "";
                try
                {
                    docType = page.Properties.DESIGNATION_DOCTYPE.ToString();
                }
                catch
                {
                }

                if (!docType.Contains("ESQUEMAS"))
                    continue;

                InterruptionPointsFilter filter = new InterruptionPointsFilter();
                filter.Page = page;

                foreach (InterruptionPoint ip in finder.GetInterruptionPoints(filter))
                {
                    string rack = InterruptionHelper.RawValue(ip, "Siemens_AG.Function.Interruption_Location");
                    string prefix = InterruptionHelper.RawValue(ip, "Siemens_AG.Function.Interruption_Prefix");
                    string ident = InterruptionHelper.RawValue(ip, "Siemens_AG.Function.Interruption_Identifier");
                    string pin = InterruptionHelper.RawValue(ip, InterruptionHelper.PROP_PIN_IDENT);
                    string mounting = InterruptionHelper.RawValue(ip, "Siemens_AG.Function.Interruption_Mounting");
                    string potential = ReadPotential(ip);
                    string dt = "+" + rack + "-" + prefix + ident + pin;

                    Match m = Regex.Match(mounting.Trim().ToUpperInvariant(), @"^([A-Z])(\d{1,2})$");
                    if (!m.Success || !RowY.ContainsKey(m.Groups[1].Value[0]))
                    {
                        string text = dt + "  mounting '" + mounting + "'";
                        skipped.Add(text);
                        ErrorLog.Add("schematic " + dt, "bad mounting '" + mounting + "'");
                        continue;
                    }

                    items.Add(new SlotItem
                    {
                        Dt = dt,
                        Rack = rack,
                        Potential = potential,
                        Row = m.Groups[1].Value[0],
                        Column = int.Parse(m.Groups[2].Value),
                        Location = rack,
                        Prefix = prefix,
                        Identifier = ident,
                        Pin = pin
                    });
                }
            }

            return items;
        }

        public static List<Slot> ComputeSlots(List<SlotItem> list)
        {
            List<Slot> result = new List<Slot>();

            foreach (var rowGroup in list.GroupBy(i => i.Row))
            {
                double y = RowY[rowGroup.Key];
                List<SlotItem> rowItems = rowGroup.OrderBy(i => i.Column).ThenBy(i => i.Dt).ToList();
                bool overflow = rowItems.Any(i => i.Column < 1 || i.Column > 9);

                if (!overflow)
                {
                    // One X per column; several in the same cell are offset slightly
                    foreach (var cell in rowItems.GroupBy(i => i.Column))
                    {
                        List<SlotItem> c = cell.ToList();
                        for (int k = 0; k < c.Count; k++)
                        {
                            double offset = c.Count == 1 ? 0 : (k - (c.Count - 1) / 2.0) * 8;
                            result.Add(new Slot { Item = c[k], X = ColumnX[c[k].Column] + offset, Y = y });
                        }
                    }
                }
                else
                {
                    // Overflow: spread all items of this row evenly across columns 1..9
                    int n = rowItems.Count;
                    double left = ColumnX[1];
                    double right = ColumnX[9];
                    for (int k = 0; k < n; k++)
                    {
                        double x = n == 1 ? (left + right) / 2 : left + k * (right - left) / (n - 1);
                        result.Add(new Slot { Item = rowItems[k], X = x, Y = y });
                    }
                }
            }

            return result;
        }

        private static string ReadPotential(InterruptionPoint ip)
        {
            string value = "";
            try
            {
                value = ip.Properties[24810].ToString();
            }
            catch
            {
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                try
                {
                    value = ip.Properties.INTERRUPTIONPOINT_DESCRIPTIONAUTOMATIC.ToString();
                }
                catch
                {
                }
            }

            value = value.Trim();
            int at = value.LastIndexOf('@');
            if (at >= 0)
                value = value.Substring(at + 1);
            value = value.Trim().TrimEnd(';').Trim();

            return string.IsNullOrEmpty(value) ? "(empty)" : value;
        }
    }
}