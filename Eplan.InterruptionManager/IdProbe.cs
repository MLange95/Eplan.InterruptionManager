using System;
using System.Text;
using Eplan.EplApi.DataModel;
using Eplan.EplApi.HEServices;

namespace Eplan.InterruptionManager
{
    public static class IdProbe
    {
        public static string Run(Project project)
        {
            DMObjectsFinder finder = new DMObjectsFinder(project);
            StringBuilder sb = new StringBuilder();
            int n = 0;

            foreach (InterruptionPoint ip in finder.GetInterruptionPoints(new InterruptionPointsFilter()))
            {
                n++;
                if (n > 20)
                    break;

                string id;
                try
                {
                    id = ip.Properties.PROPUSER_DBOBJECTID.ToString();
                }
                catch (Exception ex)
                {
                    id = "error: " + ex.Message;
                }
                sb.AppendLine(id);
            }

            return "First interruptions (DB object ID):\n" + sb.ToString();
        }
    }
}