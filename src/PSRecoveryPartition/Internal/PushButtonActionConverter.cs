using System;
using System.Collections.Generic;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Normalizes a friendly free-form <c>-PushButtonAction</c> string into the
    /// strongly typed <see cref="WindowsRecoveryPushButtonAction"/> value.
    /// </summary>
    internal static class PushButtonActionConverter
    {
        private static readonly IDictionary<string, WindowsRecoveryPushButtonAction> Map =
            new Dictionary<string, WindowsRecoveryPushButtonAction>(StringComparer.OrdinalIgnoreCase)
        {
            { "reset",            WindowsRecoveryPushButtonAction.Reset },
            { "refresh",          WindowsRecoveryPushButtonAction.Refresh },
            { "factoryreset",     WindowsRecoveryPushButtonAction.FactoryReset },
            { "factory",          WindowsRecoveryPushButtonAction.FactoryReset },
            { "advancedstartup",  WindowsRecoveryPushButtonAction.AdvancedStartup },
            { "advanced",         WindowsRecoveryPushButtonAction.AdvancedStartup },
            { "boottore",         WindowsRecoveryPushButtonAction.BootToRE },
            { "winre",            WindowsRecoveryPushButtonAction.BootToRE },
            { "re",               WindowsRecoveryPushButtonAction.BootToRE }
        };

        public static bool TryConvert(string value, out WindowsRecoveryPushButtonAction result)
        {
            result = default(WindowsRecoveryPushButtonAction);
            if (string.IsNullOrWhiteSpace(value)) { return false; }

            string normalized = new string(value.Trim().ToCharArray());
            var compact = normalized.Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty);

            return Map.TryGetValue(compact, out result);
        }

        public static WindowsRecoveryPushButtonAction Convert(string value)
        {
            WindowsRecoveryPushButtonAction result;
            if (!TryConvert(value, out result))
            {
                throw new ArgumentException(
                    "Unsupported push-button action '" + value +
                    "'. Supported values: Reset, Refresh, FactoryReset, AdvancedStartup, BootToRE.",
                    "value");
            }
            return result;
        }
    }
}
