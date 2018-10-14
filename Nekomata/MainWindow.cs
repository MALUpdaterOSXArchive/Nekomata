/* MainWindow.cs
 * This is the main application window.
 * 
 * Copyright (c) 2018 MAL Updater OS X Group, a division of Moy IT Solutions
 * Licensed under MIT License
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using NekomataCore;

namespace Nekomata
{
    public partial class MainWindow : Form
    {
        private AboutBox aboutbox;
        private Thread exportthread;
        private ListNormalizer listnormalizer;
        private NormalListToMALXML nltoMALXML;
        private TitleIDConverter titleidconverter;
        private Settingsdlg settings;

        public MainWindow()
        {
            InitializeComponent();
            this.listnormalizer = new ListNormalizer();
            this.nltoMALXML = new NormalListToMALXML();
            this.titleidconverter = new TitleIDConverter();
            this.listnormalizer.tconverter = this.titleidconverter;
            this.nltoMALXML.tconverter = this.titleidconverter;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Create window handle
            if (!this.IsHandleCreated)
            {
                this.CreateControl();
            }
            WinSparkle.win_sparkle_set_appcast_url("https://updates.malupdaterosx.moe/nekomata/nekomata.xml");
            //start automatic update checks.
            WinSparkle.win_sparkle_init();

        }

        private void aboutNekomataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.aboutbox == null)
            {
                aboutbox = new AboutBox();
            }
            aboutbox.ShowDialog();
        }

        private void exportBtn_Click(object sender, EventArgs e)
        {
            if (usernameField.Text.Length > 0)
            {
                Service selectedservice = Service.Kitsu;
                if (anilistRadioBtn.Checked)
                {
                    selectedservice = Service.AniList;
                }
                else if (kitsuRadioBtn.Checked)
                {
                    selectedservice = Service.Kitsu;
                }
                EntryType selectedtype = EntryType.Anime;
                if (animeRadioBtn.Checked)
                {
                    selectedtype = EntryType.Anime;
                }
                else
                {
                    selectedtype = EntryType.Manga;
                }
                this.exportthread = new Thread(() => BeginExport(usernameField.Text, selectedservice, selectedtype));
                this.exportthread.Start();
            }
            else
            {
                MessageBox.Show("You did not specify a username. Please specify one and try again", "Export Failed");
            }
        }

        private void BeginExport(string username, NekomataCore.Service listservice, NekomataCore.EntryType selectedtype)
        {
            // Create window handle
            if (!this.IsHandleCreated)
            {
                this.CreateControl();
            }
            // Disable Export Button
            Utility.SafeInvoke(exportBtn, new Action(delegate { exportBtn.Enabled = false; }), false);
            // Retrieve list
            Utility.SafeInvoke(progressBar1, new Action(delegate { progressBar1.Maximum = 3; progressBar1.Value = 0; }), false);
            List<ListEntry> userlist;
            switch (listservice)
            {
                case Service.AniList:
                    userlist = listnormalizer.RetrieveAniListList(selectedtype, username);
                    break;
                case Service.Kitsu:
                    userlist = listnormalizer.RetrieveKitsuList(selectedtype, username);
                    break;
                default:
                    ErrorOut("Invalid Service Selected. Please select a valid service and try again.");
                    return;
            }
            if (!listnormalizer.erroredout)
            {
                Utility.SafeInvoke(progressBar1, new Action(delegate { progressBar1.Value = progressBar1.Value + 1; }), true);
                nltoMALXML.ConvertNormalizedListToMAL(userlist, selectedtype, username, listservice);
                if (nltoMALXML.InvalidEntriesExist())
                {
                    Utility.SafeInvoke(this, new Action(delegate
                    {
                        FailedWindow fdlg = new FailedWindow(nltoMALXML.faillist);
                        if (fdlg.ShowDialog() == DialogResult.OK)
                        {
                            GenerateXMLandSave();
                        }
                    }), true);
                }
                else
                {
                    GenerateXMLandSave();
                }
            }
            else
            {
                ErrorOut("Couldn't retrieve your list. Please check your username and try again");
                listnormalizer.cleanup();
                return;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void ErrorOut(string message)
        {
            Utility.SafeInvoke(exportBtn, new Action(delegate { exportBtn.Enabled = true; }), false);
            Utility.SafeInvoke(progressBar1, new Action(delegate { progressBar1.Value = 3; }), false);
            MessageBox.Show(message, "Export Failed");
        }

        private void GenerateXMLandSave()
        {
            this.setupdateonimport();
            String XML = nltoMALXML.GenerateXML();
            Utility.SafeInvoke(progressBar1, new Action(delegate { progressBar1.Value = progressBar1.Value + 1; }), false);
            Utility.SafeInvoke(this, new Action(delegate
            {
                SaveFileDialog savedialog = new SaveFileDialog();
                savedialog.Filter = "Extended Markup Language file (*.xml)|*.xml";
                savedialog.RestoreDirectory = true;
                if (savedialog.ShowDialog() == DialogResult.OK)
                {
                    StreamWriter xmlfile = new StreamWriter(savedialog.OpenFile());
                    StringReader xmlReader = new StringReader(XML);
                    if (xmlfile != null)
                    {
                        string line;
                        while ((line = xmlReader.ReadLine()) != null)
                        {
                            xmlfile.WriteLine(line);
                        }
                        xmlfile.Close();

                    }
                    this.showexportsuccess();
                }
                nltoMALXML.cleanup();
                listnormalizer.cleanup();
            }), true);
            Utility.SafeInvoke(exportBtn, new Action(delegate { exportBtn.Enabled = true; }), false);
            Utility.SafeInvoke(progressBar1, new Action(delegate { progressBar1.Value = 3; }), false);
        }

        private void checkForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //System.Diagnostics.Process.Start("https://malupdaterosx.moe/nekomata/");
            WinSparkle.win_sparkle_check_update_with_ui();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://malupdaterosx.moe/donate/");
        }

        private void setupdateonimport()
        {
            nltoMALXML.updateonimportcurrent = Properties.Settings.Default.updateonimportcurrent;
            nltoMALXML.updateonimportcompleted = Properties.Settings.Default.updateonimportcompleted;
            nltoMALXML.updateonimportonhold = Properties.Settings.Default.updateonimportonhold;
            nltoMALXML.updateonimportdropped = Properties.Settings.Default.updateonimportdropped;
            nltoMALXML.updateonimportplanned = Properties.Settings.Default.updateonimportplanned;
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.settings == null)
            {
                this.settings = new Settingsdlg();
            }
            this.settings.Show();
        }

        private void showexportsuccess()
        {
            DialogResult result = MessageBox.Show("If you find this program helpful, please donate so we can continue developing it. Do you want to view the donation page now?", "Export Successful", MessageBoxButtons.YesNo);
            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                System.Diagnostics.Process.Start("https://malupdaterosx.moe/donate/");
            }
        }


    }
    public static class Utility
    {
        public static void SafeInvoke(this Control uiElement, Action updater, bool forceSynchronous)
        {
            if (uiElement == null)
            {
                throw new ArgumentNullException("uiElement");
            }

            if (uiElement.InvokeRequired)
            {
                if (forceSynchronous)
                {
                    uiElement.Invoke((Action)delegate { SafeInvoke(uiElement, updater, forceSynchronous); });
                }
                else
                {
                    uiElement.BeginInvoke((Action)delegate { SafeInvoke(uiElement, updater, forceSynchronous); });
                }
            }
            else
            {
                if (uiElement.IsDisposed)
                {
                    throw new ObjectDisposedException("Control is already disposed.");
                }

                updater();
            }
        }
    }
}
