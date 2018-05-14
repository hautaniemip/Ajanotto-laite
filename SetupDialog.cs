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
    public partial class SetupDialog : Form
    {
        protected Ajanottolaite mainForm;

        public SetupDialog(Ajanottolaite form)
        {
            InitializeComponent();
            mainForm = form;

            baudRateDropdown.SelectedIndex = 4;

            // Clear the dropdown list and find all COM-ports and add them to list
            COMDropdown.Items.Clear();
            foreach (string item in System.IO.Ports.SerialPort.GetPortNames())
            {
                if (COMDropdown.Items.Contains(item))
                {
                    return;
                }

                COMDropdown.Items.Add(item);
            }
        }

        // Refresh the dropdown list
        private void RefreshButton_Click(object sender, EventArgs e)
        {
            // Clear the dropdown list and find all COM-ports and add them to list
            COMDropdown.Items.Clear();

            foreach (string item in System.IO.Ports.SerialPort.GetPortNames())
            {
                if (COMDropdown.Items.Contains(item))
                {
                    return;
                }
                COMDropdown.Items.Add(item);
            }
        }

        // Pass data to mainForm
        private void OKButton_Click(object sender, EventArgs e)
        {
            // Read the baudRate
            int baudRate;
            bool parseOK = Int32.TryParse(baudRateDropdown.Text.ToString(), out baudRate);

            if (!parseOK)
            {
                MessageBox.Show("Error something has gone very badly wrong.\n\nThis error should not be ever displayed because there should not be a invalid item in baud rate list", "Very Bad Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            mainForm.setupData = new SetupData(COMDropdown.Text.ToString(), baudRate);
            mainForm.SetupMainUnit();
        }
    }
}
