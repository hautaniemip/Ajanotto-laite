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
    public partial class ClassSetup : Form
    {
        Ajanottolaite mainForm;
        public ClassSetup(Ajanottolaite form)
        {
            InitializeComponent();
            mainForm = form;
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            if (NameBox.Text == "" || ClassHeightBox.Text == "" || ReviewBox.Text == "")
            {
                // If there is any empty fields warn user about them
                MessageBox.Show("Täytä kaikki tekstikentät.", "Fill in all text fields", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            else
            {
                // Make file name for saving
                mainForm.className = NameBox.Text + " " + ClassHeightBox.Text + " " + ReviewBox.Text;
                if (mainForm.startedBackup)
                {
                    mainForm.backupTimer.Stop();
                    mainForm.StartBackup();
                }
                Debug.WriteLine(mainForm.className);
                this.Close();
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
