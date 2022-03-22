using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using Microsoft.Win32.TaskScheduler;
using MySql.Data.MySqlClient;
using Sigmasoft.Application.Domain;
using Sigmasoft.Application.Helper;
using Sigmasoft.Application.Services;
using Action = System.Action;
using Task = Microsoft.Win32.TaskScheduler.Task;

namespace Client.Dekstop
{
    public partial class MainForm : Form
    {
        //Entity
        private DbConfig source;
        private DbConfig destination;
        private SheduleConfig shdConfig;
        private SSHConfig sshConfig;
        private SSHLocalServer sshLocalServer;
        private SSHLocalServer sshLocalServerBounding;
        //Config File
        private string configSrc = Directory.GetCurrentDirectory() + "\\config\\configSrc.xml";
        private string configDest = Directory.GetCurrentDirectory() + "\\config\\configDest.xml";
        private string configSheduler = Directory.GetCurrentDirectory() + "\\config\\configTaskSheduler.xml";
        //Config File SSH
        private string configSSH = Directory.GetCurrentDirectory() + "\\config\\configSSH.xml";
        private string configLocalServer = Directory.GetCurrentDirectory() + "\\config\\configLocalServer.xml";
        private string configLocalServerBouding = Directory.GetCurrentDirectory() + "\\config\\configLocalServerBounding.xml";
        //Service
        private DbManagerService dbManager;
        //Log file
        private const string FILE_LOG = "\\logs\\file.log";

        string _currentTableName = "";
        int _totalRowsInCurrentTable = 0;
        int _totalRowsInAllTables = 0;
        int _currentRowIndexInCurrentTable = 0;
        int _currentRowIndexInAllTable = 0;
        int _totalTables = 0;
        int _currentTableIndex = 0;

        public MainForm()
        {
            InitializeComponent();
            //DB init
            source = new DbConfig();
            destination = new DbConfig();
            shdConfig = new SheduleConfig();

            //SSH init
            sshConfig = new SSHConfig();
            sshLocalServer = new SSHLocalServer();
            sshLocalServerBounding = new SSHLocalServer();

            //DATABASE Bindning
            this.bindingSource.DataSource = source;
            this.bindingDestination.DataSource = destination;
            this.bindingShedule.DataSource = shdConfig;

            //SSH binding
            this.bindingSsh.DataSource = sshConfig;
            this.bindingHost.DataSource = sshLocalServer;
            this.bindingBounding.DataSource = sshLocalServerBounding;

            //Service DB Manager
            MySqlBackup mb = new MySqlBackup();
            this.dbManager= new DbManagerService();

            this.dbManager.DbBackup.ExportProgressChanged += ExportProgressChange;

            timer1.Interval = 50;

        }

        private void ExportProgressChange(object sender, ExportProgressArgs e)
        {
            _currentRowIndexInAllTable = (int)e.CurrentRowIndexInAllTables;
            _currentRowIndexInCurrentTable = (int)e.CurrentRowIndexInCurrentTable;
            _currentTableIndex = e.CurrentTableIndex;
            _currentTableName = e.CurrentTableName;
            _totalRowsInAllTables = (int)e.TotalRowsInAllTables;
            _totalRowsInCurrentTable = (int)e.TotalRowsInCurrentTable;
            _totalTables = e.TotalTables;
            //LblReportProgress.Text = $"{e.CurrentTableIndex}";
        }

        private void BtnValid_Click(object sender, EventArgs e)
        {
            try
            {
                var dbSrc = (DbConfig) this.bindingSource.DataSource;
                var dbDest = (DbConfig)this.bindingDestination.DataSource;
                var sheduler = (SheduleConfig) this.bindingShedule.DataSource;

                var sshCfg = (SSHConfig)this.bindingSsh.DataSource;
                var sshLocal = (SSHLocalServer)this.bindingHost.DataSource;
                var sshBouding = (SSHLocalServer)this.bindingBounding.DataSource;


                if (Directory.Exists(Directory.GetCurrentDirectory() + "\\config"))
                {
                    if (System.IO.File.Exists(configSrc))
                    {
                        File.Delete(configSrc);

                        SaveConfig(dbSrc, configSrc);
                        
                    }

                    if (File.Exists(configDest))
                    {
                        File.Delete(configDest);

                        SaveConfig(dbDest, configDest);

                    }

                    if (File.Exists(configSheduler))
                    {
                        File.Delete(configSheduler);

                        SaveConfig(sheduler, configSheduler);
                    }
                    
                    if (File.Exists(configSSH))
                    {
                        File.Delete(configSSH);

                        SaveConfig(sshCfg, configSSH);
                    }
                    
                    if (File.Exists(configLocalServer))
                    {
                        File.Delete(configLocalServer);

                        SaveConfig(sshLocal, configLocalServer);
                    }
                    
                    if (File.Exists(configLocalServerBouding))
                    {
                        File.Delete(configLocalServerBouding);

                        SaveConfig(sshBouding, configLocalServerBouding);
                    }
                }
                else
                { 
                    Directory.CreateDirectory("config");

                    SaveConfig(dbSrc, configSrc);
                    SaveConfig(dbDest, configDest);
                    SaveConfig(sheduler, configSheduler);

                    //SSH Config
                    SaveConfig(this.sshConfig, configSSH);
                    SaveConfig(this.sshLocalServer, configLocalServer);
                    SaveConfig(this.sshLocalServerBounding, configLocalServerBouding);
                }

                this.readOnlyTextControls(this.groupBox1);
                this.readOnlyTextControls(this.groupBox2);
                this.readOnlyTextControls(this.groupBox3);
                this.readOnlyTextControls(this.groupBox4);
                this.readOnlyTextControls(this.groupBox5);

            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        private void SaveConfig(Object config, string fileConfig)
        {
            XmlSerializer serializer = new XmlSerializer(config.GetType());

            FileStream fs = new FileStream(fileConfig, FileMode.Append);

            serializer.Serialize(fs, config);

            fs.Close();
        }

        private void ReadConfig(Object dbSource, BindingSource bindingSource, string fileConfig)
        {
            if (File.Exists(fileConfig))
            {
                XmlSerializer reader = new XmlSerializer(dbSource.GetType());
                StreamReader file = new StreamReader(fileConfig);
                bindingSource.DataSource = (Object)reader.Deserialize(file);
                file.Close();

                this.readOnlyTextControls(this.groupBox1);
                this.readOnlyTextControls(this.groupBox2);
                this.readOnlyTextControls(this.groupBox3);
                this.readOnlyTextControls(this.groupBox4);
                this.readOnlyTextControls(this.groupBox5);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            ReadConfig(this.source, this.bindingSource, configSrc);
            ReadConfig(this.destination, this.bindingDestination, configDest);
            ReadConfig(this.shdConfig, this.bindingShedule, configSheduler);

            //SSH
            ReadConfig(this.sshConfig, this.bindingSsh, configSSH);
            ReadConfig(this.sshLocalServer, this.bindingHost, configLocalServer);
            ReadConfig(this.sshLocalServerBounding, this.bindingBounding, configLocalServerBouding);

            //Load log file
            ReadFile(FILE_LOG);

            BtnStop.Enabled = false;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ReadFileInDirectory();
        }

        private void readOnlyTextControls(GroupBox groupBox)
        {

            foreach (var control in groupBox.Controls)
            {
                if (control is TextBox)
                {
                    var txtBox = (TextBox) control;
                    txtBox.ReadOnly = true;
                }
                else if(control is MaskedTextBox)
                {
                    var maskedText = (MaskedTextBox)control;
                    maskedText.ReadOnly = true;
                }
            }
        }

        private void ClearField(GroupBox groupBox)
        {
            foreach (var control in groupBox.Controls)
            {
                if (control is TextBox)
                {
                    var txtBox = (TextBox)control;
                    txtBox.Text = String.Empty;
                    txtBox.ReadOnly = false;
                }
                else if (control is MaskedTextBox)
                {
                    var maskedText = (MaskedTextBox)control;
                    maskedText.Text = String.Empty;
                    maskedText.ReadOnly = false;
                }
            }
        }

        private void WriteField(GroupBox groupBox)
        {
            foreach (var control in groupBox.Controls)
            {
                if (control is TextBox)
                {
                    var txtBox = (TextBox)control;
                    txtBox.ReadOnly = false;
                }
                else if (control is MaskedTextBox)
                {
                    var maskedText = (MaskedTextBox)control;
                    maskedText.ReadOnly = false;
                }
            }
        }

        private void ReadFile(string file)
        {
            try
            {
                List<Object> fileSource = new List<Object>();

                string path = Directory.GetCurrentDirectory() + "\\asset\\icons8_high_risk_16.png";

                foreach (string line in File.ReadLines(Directory.GetCurrentDirectory() + file))
                {
                    fileSource.Add(new { _ = Bitmap.FromFile(path), Message = line });
                }

                DGridViewLog.DataSource = fileSource.ToList();
            }
            catch (Exception e)
            {
               Log.WriteToFile(e.Message);

               this.ReadFile(FILE_LOG);
            }
        }

        private void ReadFileInDirectory()
        {
            try
            {
                List<Object> fileDirectory = new List<object>();

                var scheduler = (SheduleConfig)this.bindingShedule.DataSource;

                string path = scheduler.FilePath;

                string iconPath = Directory.GetCurrentDirectory() + "\\asset\\icons8_database_export_16.png";

                foreach (var file in Directory.GetFiles(path, "*.sql"))
                {
                    fileDirectory.Add(new {_ = Bitmap.FromFile(iconPath) , Fichier = file });
                }

                DGViewDbHistory.DataSource = fileDirectory.ToList();
            }
            catch (Exception e)
            {
                Log.WriteToFile(e.Message);

                this.ReadFile(FILE_LOG);
            }
        }

        private void BtnGetPath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fBrowserDialog = new FolderBrowserDialog();

            fBrowserDialog.ShowDialog();

            if (!string.IsNullOrEmpty(fBrowserDialog.SelectedPath))
            {
                var sheduler = (SheduleConfig)this.bindingShedule.DataSource;
                sheduler.FilePath = fBrowserDialog.SelectedPath;
                this.bindingShedule.DataSource = sheduler;
                TxtPath.Text = fBrowserDialog.SelectedPath;
            }

        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(Directory.GetCurrentDirectory() + "\\config"))
            {
                System.IO.Directory.Delete(Directory.GetCurrentDirectory() + "\\config", true);

                this.bindingSource.RemoveCurrent();
                this.bindingDestination.RemoveCurrent();
                this.bindingSsh.RemoveCurrent();
                this.bindingHost.RemoveCurrent();
                this.bindingBounding.RemoveCurrent();

                this.ClearField(this.groupBox1);
                this.ClearField(this.groupBox2);
                this.ClearField(this.groupBox3);
                this.ClearField(this.groupBox4);
                this.ClearField(this.groupBox5);

                BtnGetPath.Enabled = true;

                BtnValid.Enabled = true;

            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            _currentTableName = "";
            _totalRowsInCurrentTable = 0;
            _totalRowsInAllTables = 0;
            _currentRowIndexInCurrentTable = 0;
            _currentRowIndexInAllTable = 0;
            _totalTables = 0;
            _currentTableIndex = 0;

            this.timer1.Start();

            this.TxtStatus.BackColor = Color.Chartreuse;
            this.TxtStatus.ForeColor = Color.Black;

            this.TxtStatus.Text = "En cours de traitement".ToUpper();

            this.dbManager.DbBackup.ExportInfo.IntervalForProgressReport = 50;

            if (backgroundWorker1.IsBusy != true)
            {
                backgroundWorker1.RunWorkerAsync();
            }

            BtnStop.Enabled = true;
            BtnStart.Enabled = false;
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {

            this.TxtStatus.BackColor = Color.Red;
            this.TxtStatus.ForeColor = Color.White;
            this.TxtStatus.Text = "Stop ...".ToUpper();

            BtnStop.Enabled = false;
            BtnStart.Enabled = true;

            this.timer1.Stop();

            this.dbManager.CloseConnection();

            backgroundWorker1.CancelAsync();

            backgroundWorker1.Dispose();
        }

        private double Speed(string url)
        {
            WebClient wc = new WebClient();

            //DateTime Variable To Store Download Start Time.
            DateTime dt1 = DateTime.Now;

            //Number Of Bytes Downloaded Are Stored In ‘data’
            byte[] data = wc.DownloadData(url);

            //DateTime Variable To Store Download End Time.
            DateTime dt2 = DateTime.Now;

            //To Calculate Speed in Kb Divide Value Of data by 1024 And Then by End Time Subtract Start Time To Know Download Per Second.
            return Math.Round((data.Length / 1024) * (dt2 - dt1).TotalSeconds, 2);
        }

        private void UpdateSpeed(string msg)
        {
            Action action = () => lbSpeedNetwork.Text = msg;
            this.Invoke(action);
        }

        void DoWork(Object obj)
        {
            UpdateSpeed($"{this.Speed("https://www.google.com/")} Kb/s");
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(DoWork));

            pbTable.Maximum = _totalTables;
            if (_currentTableIndex <= pbTable.Maximum)
                pbTable.Value = _currentTableIndex;

            pbRowInCurTable.Maximum = _totalRowsInCurrentTable;
            if (_currentRowIndexInCurrentTable <= pbRowInCurTable.Maximum)
                pbRowInCurTable.Value = _currentRowIndexInCurrentTable;

            pbRowInAllTable.Maximum = _totalRowsInAllTables;
            if (_currentRowIndexInAllTable <= pbRowInAllTable.Maximum)
                pbRowInAllTable.Value = _currentRowIndexInAllTable;

            lbCurrentTableName.Text = "Table en cours de traitement = [" + _currentTableName.ToUpper()+"]";
            lbRowInCurTable.Text = pbRowInCurTable.Value + " sur " + pbRowInCurTable.Maximum;

            lbRowInAllTable.Text = pbRowInAllTable.Value + " sur " + pbRowInAllTable.Maximum;
            lbTableCount.Text = _currentTableIndex + " sur " + _totalTables;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                BtnStart.Enabled = true;
                BtnStop.Enabled = false;
                TxtStatus.Text = "Opération Terminer ...".ToUpper();
                MessageBox.Show("Opération términer avec succès !", "Information", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                this.dbManager.CloseConnection();
            }
            else
            {
                BtnStart.Enabled = true;
                BtnStop.Enabled = false;
                TxtStatus.Text = "Erreur ...".ToUpper();
                TxtStatus.ForeColor = Color.White;
                TxtStatus.BackColor = Color.Red;
                MessageBox.Show("Une erreur est survenue au moment du traitement.", "ERREUR", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                this.dbManager.CloseConnection();
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var src = (DbConfig)this.bindingSource.DataSource;
            var dest = (DbConfig)this.bindingDestination.DataSource;
            var sheduler = (SheduleConfig)this.bindingShedule.DataSource;

            this.dbManager.Source = src;
            this.dbManager.Destination = dest;
            this.dbManager.Shedule = sheduler;

            this.dbManager.InitializeConnection();

            if (sheduler.IsEnableSSH == true)
            {
                var sshCfg = (SSHConfig)this.bindingSsh.DataSource;
                var sshLocal = (SSHLocalServer)this.bindingHost.DataSource;
                var sshBouding = (SSHLocalServer)this.bindingBounding.DataSource;

                SSHManagerService sshManager = new SSHManagerService(sshCfg, sshLocal, sshBouding);

                sshManager.Start();

                if (sshManager.IsConnected())
                {
                    if (this.dbManager.Backup() == true)
                    {
                        if (this.dbManager.CheckDatabase(dest.Database) == false)
                        {
                            this.dbManager.CreateDatabase(dest.Database);

                            this.dbManager.Restore();
                        }
                        else
                        {
                            this.dbManager.RemoveDatabase(dest.Database);

                            this.dbManager.CreateDatabase(dest.Database);

                            this.dbManager.Restore();

                        }

                        if (sshManager.IsConnected() == true)
                        {
                            sshManager.Disconnect();
                        }

                        e.Cancel = true;
                        //Read history database
                        this.ReadFileInDirectory();
                        //Load log file
                        this.ReadFile(FILE_LOG);
                    }
                    else
                    {

                        //e.Result = false;
                        e.Cancel = false;
                        //Read history database
                        this.ReadFileInDirectory();
                        //Load log file
                        this.ReadFile(FILE_LOG);
                    }
                }
                else
                {
                    MessageBox.Show(
                        "Une erreur interne est survenue au moment de la tentadive de connexion au serveur SSH, " +
                        "si le problème persiste vérifier votre connexion internet si il est active ou veuiller " +
                        "contacter votre  (FAI)", "ERREUR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
            else
            {

                if (this.dbManager.Backup() == true)
                {
                    if (this.dbManager.CheckDatabase(dest.Database) == false)
                    {
                        this.dbManager.CreateDatabase(dest.Database);

                        this.dbManager.Restore();
                    }
                    else
                    {
                        this.dbManager.RemoveDatabase(dest.Database);

                        this.dbManager.CreateDatabase(dest.Database);

                        this.dbManager.Restore();
                    }

                    e.Cancel = true;
                    //Read history database
                    this.ReadFileInDirectory();
                    //Load log file
                    this.ReadFile(FILE_LOG);
                }
                else
                {
                    //e.Result = false;
                    e.Cancel = false;
                    //Read history database
                    this.ReadFileInDirectory();
                    //Load log file
                    this.ReadFile(FILE_LOG);
                }
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            this.WriteField(this.groupBox1);
            this.WriteField(this.groupBox2);
            this.WriteField(this.groupBox3);
            this.WriteField(this.groupBox4);
            this.WriteField(this.groupBox5);
        }

        private void DGViewDbHistory_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                var obj = DGViewDbHistory.Rows[e.RowIndex].DataBoundItem;

                if (obj != null)
                {
                    Type type = obj.GetType();

                    FileInfo fileInfo = new FileInfo((string)type.GetProperty("Fichier").GetValue(obj, null));

                    TxtBoxName.Text = $"{fileInfo.Name}";
                    toolTip1.ToolTipTitle = fileInfo.Name;
                    toolTip1.IsBalloon = true;
                    toolTip1.AutoPopDelay = 5000;
                    toolTip1.ToolTipIcon = ToolTipIcon.Info;
                    toolTip1.Show(fileInfo.Name, this);
                    //TxtBoxName.Controls.Add(toolTip1.);
                    TxtBoxExt.Text = $"{fileInfo.Extension.ToUpper()}";
                    TxtBoxDateCreate.Text = $"{fileInfo.CreationTime}";
                    TxtBoxDateWrite.Text = $"{fileInfo.LastWriteTime}";
                    TxtBoxLastAccess.Text = $"{fileInfo.LastAccessTime}";
                    TxtBoxSize.Text = string.Format(CultureInfo.GetCultureInfo("cg-FR"), "{0:# ##0.00} Ko", Math.Round((double)fileInfo.Length / 1024, MidpointRounding.AwayFromZero));
                }
            }
            catch (Exception ex)
            {
                
            }
        }
    }
}
