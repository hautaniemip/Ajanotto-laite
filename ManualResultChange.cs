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
    public partial class ManualResultChange : Form
    {
        protected Ajanottolaite mainForm;

        public DataTable editableScores = new DataTable();

        public ManualResultChange(Ajanottolaite form)
        {
            InitializeComponent();

            mainForm = form;
        }

        // Prompt user if the changes should be saved
        private void OKButton_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Olet muokkaamassa tuloksia\nHaluatko varmasti tallentaa muutokset tuloksiin?", "Just making sure", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                mainForm.UpdateScoreTable(editableScores);
                this.Close();
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // Setup DataGrid for editing scores
        public void DataGridSetup(DataTable tempTable)
        {
            editableScores = tempTable;
            DataEditGrid.DataSource = editableScores;
            foreach (DataGridViewColumn column in DataEditGrid.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }
    }
}
