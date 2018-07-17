using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;
/* FailedWindow.cs
 * This dialog shows the list of titles that couldn't be exported.
 * 
 * Copyright (c) 2018 MAL Updater OS X Group, a division of Moy IT Solutions
 * Licensed under MIT License
 */
namespace Nekomata
{
    public partial class FailedWindow : Form
    {
        public List<ListEntry> faillist;

        public FailedWindow(List<ListEntry> flist)
        {
            InitializeComponent();
            this.faillist = flist;
            faileditems.Items.Clear();
            foreach (ListEntry entry in faillist)
            {
                faileditems.Items.Add(entry.title);
            }
        }

        private void continueBtn_Click(object sender, EventArgs e)
        {
            // Dismiss dialog
            DialogResult = DialogResult.OK;
            Close();
        }

        private void savefailedbtn_Click(object sender, EventArgs e)
        {
            string json = JsonConvert.SerializeObject(faillist);
            SaveFileDialog savedialog = new SaveFileDialog();
            savedialog.Filter = "Javascript Object Notation file (*.json)|*.json";
            savedialog.RestoreDirectory = true;
            if (savedialog.ShowDialog() == DialogResult.OK)
            {
                StreamWriter jsonfile = new StreamWriter(savedialog.OpenFile());
                StringReader jsonReader = new StringReader(json);
                if (jsonfile != null) {
                    string line;
                    while ((line = jsonReader.ReadLine()) != null)
                    {
                        jsonfile.WriteLine(line);
                    }
                    jsonfile.Close();
                }
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}
