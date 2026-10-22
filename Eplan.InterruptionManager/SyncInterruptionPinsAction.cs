using System;
using System.Windows.Forms;
using Eplan.EplApi.ApplicationFramework;
using Eplan.EplApi.DataModel;
using Eplan.EplApi.HEServices;

namespace Eplan.InterruptionManager
{
    public class SyncInterruptionPinsAction : IEplAction
    {
        public bool OnRegister(ref string Name, ref int Ordinal)
        {
            Name = "SyncInterruptionPinsAction";
            Ordinal = 10;
            return true;
        }

        public bool Execute(ActionCallingContext ctx)
        {
            SelectionSet selection = new SelectionSet();
            Project project = selection.GetCurrentProject(true);
            if (project == null)
            {
                MessageBox.Show("Open a project first.", "Interruption Manager");
                return false;
            }

            MessageBox.Show(DtSync.Run(project), "Interruption Manager");
            MessageBox.Show(CounterpartPlacer.Place(project), "Placement");
            return true;
        }

        public void GetActionProperties(ref ActionProperties actionProps)
        {
        }
    }
}