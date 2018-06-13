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
    public partial class MainWindow : Form
    {
        private AboutBox aboutbox;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void aboutNekomataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.aboutbox == null)
            {
                aboutbox = new AboutBox();
            }
            aboutbox.ShowDialog();
        }
    }
}
