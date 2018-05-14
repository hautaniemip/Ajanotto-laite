using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Xna.Framework;

namespace Ajanottolaite
{
    public partial class Ajanottolaite : Form
    {
        // Variable for setuping
        public SetupData setupData { get; set; }

        // Variables needed for automatic backup
        public string backupPath { get; set; }
        public string customFileName { get; set; }
        public bool defaultFileName { get; set; }
        public int timeBetweenBackups { get; set; }
        public string className { get; set; }

        // Variables for timing
        private string time;
        private Stopwatch stopwatch = new Stopwatch();
        private string firstTime;
        private string secondTime;

        // Variables for Arduino communication
        private string arduinoString;
        private bool COMPortSelected = false;
        private SerialPort serialPort;

        // Variables for timing thread
        private Thread currentTimingThread;
        private bool getTime;

        // Different dialogs
        private SetupDialog setupDialog;
        private BackupSetupDialog backupDialog;
        private ClassSetup classSetupDialog;
        private ManualResultChange manualResultChange;
        private About aboutDialog;

        // DataTables for storing, displaying and changing results
        public DataTable scoreTable { get; set; }
        public DataTable tempTable = new DataTable();

        // Variables for backup
        public bool startedBackup = false;
        private string fullBackupPath;
        public System.Windows.Forms.Timer backupTimer = new System.Windows.Forms.Timer();

        public Ajanottolaite()
        {
            InitializeComponent();

            // Setup dialogs for later use
            setupDialog = new SetupDialog(this);
            backupDialog = new BackupSetupDialog(this);
            manualResultChange = new ManualResultChange(this);
            classSetupDialog = new ClassSetup(this);
            aboutDialog = new About();

            // Start timer for updating time on screen
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 1;
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();

            // Make sure times are 0
            ResetTimes();

            // Make intialize DataTable and set it's headers
            scoreTable = new DataTable();
            scoreTable.Columns.Add("Lähtö numero");
            scoreTable.Columns.Add("Nimi");
            scoreTable.Columns.Add("Hevonen");
            scoreTable.Columns.Add("Ensimmäisen vaiheen aika");
            scoreTable.Columns.Add("Toisen vaiheen aika");
            scoreTable.Columns.Add("Virhepisteet");
            scoreTable.Columns.Add("   ");

            tempTable = scoreTable.Copy();
            ScoreViewGrid.DataSource = scoreTable;


            // Make all collumns unsortable
            foreach (DataGridViewColumn column in ScoreViewGrid.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }

        private void ReadSerialPort()
        {
            if (COMPortSelected)
            {
                try
                {
                    if (!serialPort.IsOpen)
                    {
                        serialPort.Open();          // Open serial port if not already open
                    }
                }
                catch (ArgumentOutOfRangeException e)
                {
                    MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (IOException)
                {
                    MessageBox.Show("You have selected invalid COM-port", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                while (getTime)
                {
                    string errorTemp = "";

                    if (!getTime)
                    {
                        currentTimingThread.Abort();        // If timing needs to be stopped abort timing thread from inside
                        return;
                    }

                    try
                    {
                        arduinoString = serialPort.ReadLine();      // Read data from Arduino through serial port
                        Debug.Write(arduinoString);
                    }
                    catch (Exception)
                    {
                        if (getTime)
                        {
                            MessageBox.Show("Reselect your COM-port", "Something went wrong", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }

                        if (!setupDialog.Enabled)
                        {
                            setupDialog.ShowDialog();
                        }

                        currentTimingThread.Abort();
                    }

                    if (arduinoString.Contains("Starting"))
                    {
                        StartButton_Click(null, EventArgs.Empty);       // Start timer on screen
                    }

                    try
                    {
                        if (arduinoString.Contains("First time was: "))
                        {
                            // Get first time
                            string timeString = arduinoString.Substring(16, arduinoString.Length - 17);
                            errorTemp = timeString;
                            int millis = Convert.ToInt32(timeString);
                            SetFirstTime(millis);
                            stopwatch.Restart();
                        }
                    }
                    catch (FormatException)
                    {
                        MessageBox.Show("Unexpected characters in serial data string for first time.\nBut this is the time we got in milliseconds " + errorTemp, "Unexpected characters", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    try
                    {
                        if (arduinoString.Contains("Second time was: "))
                        {
                            // Get second time
                            string timeString = arduinoString.Substring(16, arduinoString.Length - 17);
                            errorTemp = timeString;
                            int millis = Convert.ToInt32(timeString);
                            SetSecondTime(millis);
                            stopwatch.Stop();
                        }
                    }
                    catch (FormatException)
                    {
                        MessageBox.Show("Unexpected characters in serial data string for second time.\nBut this is the time we got in milliseconds " + errorTemp, "Unexpected characters", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                }
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            // Timer function for updating time on screen
            TimeSpan ts = stopwatch.Elapsed;
            string elapsedTime = String.Format("{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
            timerText.Text = elapsedTime;
            timerText.Location = new Point(this.Size.Width / 2 - timerText.Size.Width / 2, timerText.Location.Y);
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            // Manual start for stopwatch
            stopwatch.Start();
            Invoke(new Action(() =>
            {
                PauseButton.Enabled = true;
                StartButton.Enabled = false;
                ResetButton.Enabled = false;
            }));

            // Check if backup is running
            if (!startedBackup)
            {
                // Prompt user for setuping backup if it isn't already setup
                DialogResult reuslt = MessageBox.Show("Et ole asettanut varmuuskopiointia\nHaluatko asettaa sen nyt?", "No backup", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (reuslt == DialogResult.Yes)
                {
                    BackupLocationButton.PerformClick();
                }
            }

        }

        private void PauseButton_Click(object sender, EventArgs e)
        {
            // Manual stop for stopwatch
            stopwatch.Stop();
            PauseButton.Enabled = false;
            StartButton.Enabled = true;
            ResetButton.Enabled = true;
            StartButton.Text = "Continue";
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            // Manual reset for stopwatch
            stopwatch.Reset();
            PauseButton.Enabled = false;
            StartButton.Enabled = true;
            ResetButton.Enabled = true;
            StartButton.Text = "Start";
        }

        private void AddScore_Click(object sender, EventArgs e)
        {
            // Add score to DataTable
            string disState = " ";

            // Read data fields
            int number = (int)RiderNumber.Value;

            // Check if DataTable contains same starting number and prompt user what to do with it if there is same number
            bool contains = scoreTable.AsEnumerable().Any(row => number.ToString() == row.Field<String>("Lähtö numero"));

            if (contains)
            {
                DialogResult result = MessageBox.Show("Ratsukko tällä lähtönumerolla on jo merkattu taulukkoon.\nHaluatko silti lisätä uuden tuloksen samalla lähtönumerolla?", "Already exists", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    return;
                }
            }

            if (DisqualifiedCheckBox.Checked)
            {
                disState = "Hyl";
            }

            if (PenaltyPointsField.Text == null || PenaltyPointsField.Text == "" || PenaltyPointsField.Text == " ")
            {
                PenaltyPointsField.Text = "0";
            }

            // Add new row to DataTable and fill the data into correct collumns
            scoreTable.Rows.Add(RiderNumber.Value, RiderName.Text.ToString(), HorseName.Text.ToString(), firstTime, secondTime, PenaltyPointsField.Text, disState);

            // Reset data fields for next input
            RiderNumber.Value++;
            RiderName.Text = null;
            HorseName.Text = null;
            PenaltyPointsField.Text = "0";
            DisqualifiedCheckBox.Checked = false;

            // Check if backup is running
            if (!startedBackup)
            {
                // Prompt user for setuping backup if it isn't already setup
                DialogResult reuslt = MessageBox.Show("Et ole asettanut varmuuskopiointia\nHaluatko asettaa sen nyt?", "No backup", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (reuslt == DialogResult.Yes)
                {
                    BackupLocationButton.PerformClick();
                }
            }
        }

        // Function for reseting times
        private void ResetTimes()
        {
            TimeSpan ts = new TimeSpan();
            string resetedTimes = String.Format("{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
            FirstTimeText.Text = resetedTimes;
            SecondTimeText.Text = resetedTimes;
            firstTime = resetedTimes;
            secondTime = resetedTimes;
        }

        // Function for setting first time
        private void SetFirstTime(int millis)
        {
            Invoke(new Action(() =>
            {
                TimeSpan ts = new TimeSpan(0, 0, 0, 0, millis);
                Debug.WriteLine(ts);
                string newFirstTime = String.Format("{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
                FirstTimeText.Text = newFirstTime;
                firstTime = newFirstTime;
            }));
        }

        // Function for setting second time
        private void SetSecondTime(int millis)
        {
            Invoke(new Action(() =>
            {
                TimeSpan ts = new TimeSpan(0, 0, 0, 0, millis);
                Debug.WriteLine(ts);
                string newSecondTime = String.Format("{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
                SecondTimeText.Text = newSecondTime;
                secondTime = newSecondTime;
            }));
        }

        // 4 next functions are from form objects that still exist but aren't shown to user
        // TODO: Clean these functions and make sure code works after that
        private void SetupButton_Click(object sender, EventArgs e)
        {
            if (currentTimingThread != null)
            {
                getTime = false;
            }

            if (serialPort != null)
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }
            }

            setupDialog.ShowDialog();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (className.Length <= 1)
                {
                    Debug.WriteLine(className.Length);
                    MessageBox.Show("Anna luokalle pidempi nimi", "Name too short", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("Anna luokalle nimi.", "Name not defined", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string fileName = ClassNameField.Text.ToString();
            fileName = fileName.Replace(" ", "_");

            SaveTimeFile.FileName = fileName;
            SaveTimeFile.ShowDialog();
            string path = SaveTimeFile.FileName;

            try
            {
                scoreTable.WriteCsvFile(path);
            }
            catch (ArgumentException)
            {
            }
        }

        private void BackupLocationButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (className.Length <= 1)
                {
                    Debug.WriteLine(className.Length);
                    MessageBox.Show("Anna luokalle pidempi nimi", "Name too short", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                backupDialog.ShowDialog();
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("Anna luokalle nimi.", "Name not defined", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ClassNameField_TextChanged(object sender, EventArgs e)
        {
            if (startedBackup)
            {
                backupTimer.Stop();
                StartBackup();
            }
        }
        // This was the last of 4 functions

        // Function for setuping main Arduino for communication through serial port
        public void SetupMainUnit()
        {
            if (currentTimingThread != null)
            {
                if (currentTimingThread.IsAlive)
                {
                    currentTimingThread.Abort();
                }
            }

            try
            {
                getTime = true;

                serialPort = new SerialPort(setupData.COMPort, setupData.baudRate);
                COMPortSelected = true;
                Thread thread = new Thread(new ThreadStart(ReadSerialPort));
                thread.IsBackground = true;
                thread.Name = "ArduinoSerialCommunication";
                thread.Start();
                currentTimingThread = thread;
                setupDialog.Close();
            }
            catch (ArgumentException)
            {
                MessageBox.Show("Choose COM-port from 'COM-portti päälaitteelle' list", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Function for starting automatic backup
        public void StartBackup()
        {
            startedBackup = true;

            if (!Directory.Exists(backupPath + @"\Backups"))
            {
                
                Directory.CreateDirectory(backupPath + @"\Backups");
                Debug.WriteLine("File created");
            }

            if (defaultFileName)
            {
                string fileName = className;
                fileName = fileName.Replace(" ", "_");
                fullBackupPath = backupPath + @"\Backups\" + fileName + "_backup.csv";
            }

            if (!defaultFileName)
            {
                string fileName = customFileName;
                fileName.Replace(" ", "_");
                fullBackupPath = backupPath + @"\Backups\" + fileName + "_backup.csv";
            }

            backupTimer.Interval = timeBetweenBackups * 60 * 1000;
            backupTimer.Tick += new EventHandler(MakeBackup);
            backupTimer.Start();
        }

        // Function for making 
        private void MakeBackup(object sender, EventArgs e)
        {
            scoreTable.WriteCsvFile(fullBackupPath);        // Write results to csv file to user defined path
        }

        

        private void ScoreViewGrid_SelectionChanged(object sender, EventArgs e)
        {
            ScoreViewGrid.ClearSelection();     // Makes DataGridView cells unselectable
        }

        // TODO: Move these functions somewhere where they make sense
        // Function for adding 1 penalty point to score
        private void AddPenaltyPoins1_Click(object sender, EventArgs e)
        {
            if (PenaltyPointsField.Text == "")
            {
                PenaltyPointsField.Text = "0";
            }

            int currentPenaltypoints = Convert.ToInt32(PenaltyPointsField.Text.ToString());
            currentPenaltypoints++;
            PenaltyPointsField.Text = currentPenaltypoints.ToString();
        }

        // Function for adding 4 penalty points to score
        private void AddPenaltypoints4_Click(object sender, EventArgs e)
        {
            if (PenaltyPointsField.Text == "")
            {
                PenaltyPointsField.Text = "0";
            }

            int currentPenaltypoints = Convert.ToInt32(PenaltyPointsField.Text.ToString());
            currentPenaltypoints += 4;
            PenaltyPointsField.Text = currentPenaltypoints.ToString();
        }

        // Makes sure that penalty point field has only numbers
        private void PenaltyPointsField_TextChanged(object sender, EventArgs e)
        {
            string append = "";

            foreach (char c in PenaltyPointsField.Text)
            {
                if (!Char.IsNumber(c) && c != Convert.ToChar(Keys.Back))
                {

                }

                else
                {
                    append += c;
                }
            }

            PenaltyPointsField.Text = append;
            PenaltyPointsField.SelectionStart = PenaltyPointsField.Text.Length;
            PenaltyPointsField.SelectionLength = 0;
        }

        // Update scoreTable with edited scores
        public void UpdateScoreTable(DataTable editedScores)
        {
            scoreTable = editedScores;
            ScoreViewGrid.DataSource = scoreTable;
        }

        // Open setup form
        private void päälaitteenAsetuksetToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (currentTimingThread != null)
            {
                getTime = false;
            }

            if (serialPort != null)
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }
            }

            setupDialog.ShowDialog();
        }

        // Open setup form
        private void varmuuskopiointiAsetuksetToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {
                if (className.Length <= 1)
                {
                    Debug.WriteLine(className.Length);
                    MessageBox.Show("Anna luokalle pidempi nimi", "Name too short", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                backupDialog.ShowDialog();
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("Anna luokalle nimi.", "Name not defined", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Save results to .csv file
        private void tallennaTiedostoonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ClassNameField.Text.Length <= 1)
            {
                MessageBox.Show("Anna luokalle pidempi nimi", "Name too short", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string fileName = ClassNameField.Text.ToString();
            fileName = fileName.Replace(" ", "_");

            SaveTimeFile.FileName = fileName;
            SaveTimeFile.ShowDialog();
            string path = SaveTimeFile.FileName;

            try
            {
                scoreTable.WriteCsvFile(path);
            }
            catch (ArgumentException)
            {
            }
        }

        // Open window for editing manualy editing scores
        private void muokkaaTuloksiaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Haluatko varmasti muuttaa tuloksia manuaalisesti", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.No)
            {
                return;
            }

            tempTable = scoreTable.Copy();
            manualResultChange.DataGridSetup(tempTable);
            manualResultChange.ShowDialog();
        }

        // Open setup form
        private void luokanAsetuksetToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            classSetupDialog.ShowDialog();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            aboutDialog.ShowDialog();
        }

        private void lataaVarmuuskopiostaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadFromBackup.ShowDialog();

            scoreTable.ConvertCSVtoDataTable(LoadFromBackup.FileName);
        }
    }
}
