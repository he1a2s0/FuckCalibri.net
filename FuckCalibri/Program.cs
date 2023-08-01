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
                if (!silent)  //�����Ľ����� /silent ����ʱ����֪ͨǰ��Ľ���
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