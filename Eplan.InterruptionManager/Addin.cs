using System;
using Eplan.EplApi.ApplicationFramework;
using Eplan.EplApi.Gui;

namespace Eplan.InterruptionManager
{
    public class Addin : IEplAddIn
    {
        public bool OnRegister(ref bool bLoadOnStart)
        {
            bLoadOnStart = true;
            return true;
        }

        public bool OnUnregister()
        {
            return true;
        }

        public bool OnInit()
        {
            return true;
        }

        public bool OnInitGui()
        {
            ContextMenu ctxMenu = new ContextMenu();

            ctxMenu.AddMenuItem(
                new ContextMenuLocation("Editor", "Ged"),
                "Interruption Manager: Sync Pins",
                "SyncInterruptionPinsAction",
                true,
                false
            );

            return true;
        }

        public bool OnExit()
        {
            return true;
        }
    }
}