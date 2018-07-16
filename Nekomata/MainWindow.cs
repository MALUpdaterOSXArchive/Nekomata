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

namespace Nekomata
{
    public partial class MainWindow : Form
    {
        private AboutBox aboutbox;
        private Thread exportthread;
        private ListNormalizer listnormalizer;
        private NormalListToMALXML nltoMALXML;

        public MainWindow()
        {
            InitializeComponent();
            this.listnormalizer = new ListNormalizer();
            this.nltoMALXML = new NormalListToMALXML();
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
                this.exportthread = new Thread(() => BeginExport(usernameField.Text,selectedservice, selectedtype));
                this.exportthread.Start();
            }
            else
            {
                MessageBox.Show("You did not specify a username. Please specify one and try again","Export Failed");
            }
        }

        private void BeginExport(string username, Service listservice, EntryType selectedtype)
        {
            // Disable Export Button
            exportBtn.Invoke(new Action(delegate { exportBtn.Enabled = false; }));
            // Retrieve list
            progressBar1.Invoke(new Action(delegate { progressBar1.Maximum = 3;}));
            progressBar1.Invoke(new Action(delegate { progressBar1.Value = 0; }));
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
                progressBar1.Invoke(new Action(delegate { progressBar1.Value = progressBar1.Value + 1; }));
                nltoMALXML.ConvertNormalizedListToMAL(userlist, selectedtype, username, listservice);
                if (nltoMALXML.InvalidEntriesExist())
                {
                    this.Invoke(new Action(delegate
                    {
                        FailedWindow fdlg = new FailedWindow(nltoMALXML.faillist);
                        if (fdlg.ShowDialog() == DialogResult.OK)
                        {
                            GenerateXMLandSave();
                        }
                    }));
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
            exportBtn.Invoke(new Action(delegate { exportBtn.Enabled = true; }));
            progressBar1.Invoke(new Action(delegate { progressBar1.Value = 3; }));
            MessageBox.Show(message, "Export Failed");
        }
        
        private void GenerateXMLandSave()
        {
            String XML = nltoMALXML.GenerateXML();
            progressBar1.Invoke(new Action(delegate { progressBar1.Value = progressBar1.Value + 1; }));
            this.Invoke(new Action(delegate
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
                }
                nltoMALXML.cleanup();
                listnormalizer.cleanup();
            }));
            exportBtn.Invoke(new Action(delegate { exportBtn.Enabled = true; }));
            progressBar1.Invoke(new Action(delegate { progressBar1.Value = 3; }));
        }
    }
}
