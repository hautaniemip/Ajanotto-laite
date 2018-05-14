using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ajanottolaite
{
    public partial class BackupSetupDialog : Form
    {
        protected Ajanottolaite mainForm;

        private string path;
        private bool defaultFileName = true;

        public BackupSetupDialog(Ajanottolaite form)
        {
            InitializeComponent();

            mainForm = form;

            FileNameDropdown.SelectedIndex = 0;

            FileLocationTooltip.SetToolTip(FileLocationSelect, path);
        }

        // Check if user wants custom file name
        private void FileNameDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (FileNameDropdown.SelectedItem.ToString() == "Custom")
            {
                CustomFileName.Enabled = true;
                defaultFileName = false;
            }

            else
            {
                CustomFileName.Enabled = false;
                defaultFileName = true;
            }
        }

        // Dialog for backup location
        private void FileLocationSelect_Click(object sender, EventArgs e)
        {
            BackupFileBrowse.ShowDialog();
            path = BackupFileBrowse.SelectedPath;
            FileLocationTooltip.SetToolTip(FileLocationSelect, path);
            Debug.WriteLine(path);
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            if (path == null)
            {
                MessageBox.Show("You have to select path", "Empty path", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            // Pass values to mainForm
            mainForm.backupPath = path;
            mainForm.defaultFileName = defaultFileName;
            mainForm.timeBetweenBackups = (int)TimeBetweenField.Value;

            if (!defaultFileName)
            {
                string customFileName = CustomFileName.Text.ToString();

                if (customFileName.Length <= 1)
                {
                    MessageBox.Show("Give longer custom file name", "Too short file name", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                mainForm.customFileName = customFileName;
            }

            mainForm.StartBackup();

            this.Close();
        }
    }
}
