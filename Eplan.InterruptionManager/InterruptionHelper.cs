using Eplan.EplApi.Base;
using Eplan.EplApi.DataModel;
using System;
using static Eplan.EplApi.DataModel.SymbolReference;

namespace Eplan.InterruptionManager
{
    public static class InterruptionHelper
    {
        public const string PROP_PIN_IDENT = "Siemens_AG.Function.Interruption_ConnectionPoint";

        public static string LastError = "";

        public static string GetConnectedPinDesignation(InterruptionPoint interruptionPoint)
        {
            if (interruptionPoint == null)
                return string.Empty;

            foreach (GraphicalConnection gc in interruptionPoint.GraphicalConnections)
            {
                var other = gc.StartConnectionPoint;
                if (other.Parent == interruptionPoint)
                    other = gc.EndConnectionPoint;

                Function device = other.Parent as Function;
                if (device == null)
                    continue;

                PropertyValue pv = device.Properties.FUNC_CONNECTIONDESIGNATION[other.Index + 1];
                if (pv != null && !pv.IsEmpty)
                    return pv.ToString().Trim();
            }

            return string.Empty;
        }

        public static bool UpdateInterruptionPinProperty(InterruptionPoint interruptionPoint)
        {
            if (interruptionPoint == null)
                return false;

            string pin = GetConnectedPinDesignation(interruptionPoint);
            if (string.IsNullOrEmpty(pin))
                return false;

            interruptionPoint.Properties[PROP_PIN_IDENT] = pin;
            return true;
        }

        public static string RawValue(InterruptionPoint interruptionPoint, string propIdent)
        {
            try
            {
                return interruptionPoint.Properties[propIdent].ToString().Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        public static bool WriteDt(InterruptionPoint interruptionPoint)
        {
            LastError = "";

            string location = RawValue(interruptionPoint, "Siemens_AG.Function.Interruption_Location");
            string prefix = RawValue(interruptionPoint, "Siemens_AG.Function.Interruption_Prefix");
            string identifier = RawValue(interruptionPoint, "Siemens_AG.Function.Interruption_Identifier");
            string pin = RawValue(interruptionPoint, PROP_PIN_IDENT);

            if (string.IsNullOrEmpty(prefix) || string.IsNullOrEmpty(identifier) || string.IsNullOrEmpty(pin))
            {
                LastError = "missing source value: prefix=" + prefix + " identifier=" + identifier + " pin=" + pin;
                return false;
            }

            string step = "";
            try
            {
                step = "build";
                FunctionBasePropertyList parts = new FunctionBasePropertyList();
                parts.DESIGNATION_LOCATION = location;
                parts.FUNC_PREFIX = prefix;
                parts.FUNC_CODE = identifier;
                parts.FUNC_COUNTER = pin;

                step = "assign";
                interruptionPoint.NameParts = parts;
            }
            catch (Exception ex)
            {
                LastError = step + ": " + ex.Message;
                return false;
            }

            return true;
        }
    }
}