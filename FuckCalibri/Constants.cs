
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FuckCalibri {
    internal static class Constants {

        public static class LaunchArguments {
            public const string Silent = "/silent";
            public const string Show = "/show";
        }

        public static readonly ReadOnlyDictionary<string, string> TargetProcessNames = new(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                { "onenote", "OneNote 2016" },
                { "onenoteim", "OneNote for Windows 10" }
            }
        );

        public static readonly string[] TargetModuleNames = new[] { "onmain.dll", "onmainim.dll", "onmainw32.dll" };
    }
}
