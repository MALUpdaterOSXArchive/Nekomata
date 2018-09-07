using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nekomata
{
    public partial class Settingsdlg : Form
    {
        public Settingsdlg()
        {
            InitializeComponent();
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            
        }

        private void current_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.updateonimportcurrent = current.Checked;
            Properties.Settings.Default.Save();
        }

        private void completed_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.updateonimportcompleted = completed.Checked;
            Properties.Settings.Default.Save();
        }

        private void onhold_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.updateonimportonhold = onhold.Checked;
            Properties.Settings.Default.Save();
        }

        private void dropped_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.updateonimportdropped = dropped.Checked;
            Properties.Settings.Default.Save();
        }

        private void planned_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.updateonimportplanned = planned.Checked;
            Properties.Settings.Default.Save();
        }
    }
}
