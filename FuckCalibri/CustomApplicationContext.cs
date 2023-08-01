
using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FuckCalibri {
    public class CustomApplicationContext : ApplicationContext {

        public static CustomApplicationContext Current { get; private set; }

        private NotifyIcon _trayIcon;

        private readonly MainForm _mainForm;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly OnenoteMemoryPatcher _onenoteMemoryPatcher;

        public CustomApplicationContext(MainForm mainForm, bool showMainWindow = false, bool silent = true) : base() {
            _mainForm = mainForm;

            Current = this;

            InitTrayIcon();

            _mainForm.Icon = _trayIcon!.Icon;

            if (showMainWindow)
                Show(null, EventArgs.Empty);

            _cancellationTokenSource = new CancellationTokenSource();

            _onenoteMemoryPatcher = new OnenoteMemoryPatcher();
            _onenoteMemoryPatcher.PatchingStateChanged += OnenoteMemoryPatcher_PatchingStateChanged;

            ThreadExit += CustomApplicationContext_ThreadExit;

            Task.Run(async () => {
                try {
                    await _onenoteMemoryPatcher.BeginAsync(_cancellationTokenSource.Token);
                }
                catch (TaskCanceledException) {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        return;

                    //TODO: Log
                    Trace.TraceError("修改失败");
                }
            });
        }

        private void OnenoteMemoryPatcher_PatchingStateChanged(object sender, PatchingStateChangedEventArgs e) {
            _mainForm.UpdatePatchingState(e.IsPatched, e.Message, e.ProcessName);
        }

        private void CustomApplicationContext_ThreadExit(object sender, EventArgs e) {
            _cancellationTokenSource.Cancel(false);
        }

        private void InitTrayIcon() {
            if (_trayIcon != null) return;

            _trayIcon = new NotifyIcon() {
                ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("关于(&A)", Show),
                    new MenuItem("-"),
                    new MenuItem("退出(&X)", Exit)
                }),
                Visible = true
            };

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FuckCalibri.Resources.fuckcalibri.ico")) {
                if (stream != null) {
                    _trayIcon.Icon = new Icon(stream);
                }
            }

            _trayIcon.MouseClick += Toggle;
        }

        void Show(object sender, EventArgs e) {
            if (!_mainForm.Visible) _mainForm.Show();

            _mainForm.Activate();
        }

        void Toggle(object sender, MouseEventArgs e) {
            if (e.Button != MouseButtons.Left && e.Clicks != 1)
                return;

            if (!_mainForm.Visible) {
                _mainForm.Show();
                _mainForm.Activate();
            }
            else
                _mainForm.Hide();
        }

        void Exit(object sender, EventArgs e) {
            if (_trayIcon != null)
                _trayIcon.Visible = false;

            if (_mainForm != null)
                _mainForm.Close();

            Application.Exit();
        }
    }
}
