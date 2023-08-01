using System;
using System.Linq;
using System.Windows.Forms;

namespace FuckCalibri {
    internal static class Program {
        [STAThread]
        static void Main() {
            var cmdLineArgs = Environment.GetCommandLineArgs();
            var silent = cmdLineArgs.Length > 0 && cmdLineArgs.Contains(Constants.LaunchArguments.Silent, StringComparer.OrdinalIgnoreCase);

            SingleInstanceHelper.Check(typeof(Program), silent);
            if (!SingleInstanceHelper.IsSingleInstance) {
                if (!silent)  //后来的进程以 /silent 启动时，不通知前面的进程
                    SingleInstanceHelper.Notify();

                Application.Exit();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var showMainWindow = cmdLineArgs.Length > 0 && cmdLineArgs.Contains(Constants.LaunchArguments.Show, StringComparer.OrdinalIgnoreCase);

            Application.Run(new CustomApplicationContext(new MainForm(), showMainWindow, silent));
        }
    }
}