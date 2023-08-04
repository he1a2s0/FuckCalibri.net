using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FuckCalibri {
    public partial class MainForm : Form {

        private readonly CheckBox[] _checkBoxes;
        public MainForm() {
            InitializeComponent();

            _checkBoxes = new CheckBox[] { checkBox1, checkBox2 };
            for (var i = 0; i < _checkBoxes.Length; i++) {
                if (i > Constants.TargetProcessNames.Count - 1) break;

                var processNamePair = Constants.TargetProcessNames.ElementAt(i);
                _checkBoxes[i].Text = processNamePair.Value;
                _checkBoxes[i].Tag = processNamePair.Key;
            }
        }

        protected override void DefWndProc(ref Message m) {
            base.DefWndProc(ref m);

            if (SingleInstanceHelper.ReactToNotification && SingleInstanceHelper.IsMutextMessage(m.Msg)) {
                if (!Visible)
                    Show();
                Activate();
            }
        }

        protected override void OnClosing(CancelEventArgs e) {
            e.Cancel = true;
            if (Visible)
                Hide();
        }

        private readonly Dictionary<string, bool> _patchedProcessNames = Constants.TargetProcessNames.ToDictionary(_ => _.Key, _ => false, StringComparer.OrdinalIgnoreCase);

        internal int UpdatePatchingState(bool patched, string message, string processName) {
            if (InvokeRequired) {
                var returnValue = Invoke(UpdatePatchingState, patched, message, processName);
                return returnValue == null ? 0 : int.Parse(returnValue.ToString());
            }

            _patchedProcessNames[processName] = patched;

            var total = _patchedProcessNames.Count;
            var numOfSuccessful = _patchedProcessNames.Count(_ => _.Value);

            var checkBox = GetCheckBox(processName);
            if (checkBox != null) {
                checkBox.Checked = patched;
                checkBox.ForeColor = numOfSuccessful == total ? Color.Red :
                numOfSuccessful == 0 ? Color.Gray : Color.DimGray;
            }

            return numOfSuccessful;
        }

        private CheckBox GetCheckBox(string processName) {
            return _checkBoxes.FirstOrDefault(_ => processName.Equals(Convert.ToString(_.Tag), StringComparison.OrdinalIgnoreCase));
        }
    }
}