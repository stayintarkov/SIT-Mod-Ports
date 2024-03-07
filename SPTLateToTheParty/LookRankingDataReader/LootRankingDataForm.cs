using LookRankingDataReader.Models;
using Newtonsoft.Json;
using System.Data;

namespace LookRankingDataReader
{
    public partial class LootRankingDataForm : Form
    {
        private static LootRankingContainerConfig? lootRankingContainer;

        public LootRankingDataForm()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openLootRankingDataDialog.ShowDialog() == DialogResult.OK)
            {
                string json = File.ReadAllText(openLootRankingDataDialog.FileName);
                lootRankingContainer = JsonConvert.DeserializeObject<LootRankingContainerConfig>(json);
                updateLootRankingData();
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void updateLootRankingData()
        {
            lootRankingDataGridView.DataSource = null;

            if ((lootRankingContainer == null) || (lootRankingContainer.Items.Count == 0))
            {
                return;
            }

            this.Cursor = Cursors.WaitCursor;
            lootRankingDataGridView.SuspendLayout();

            DataTable dt = new DataTable();
            dt.Columns.Add("ID", typeof(string));
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Value", typeof(double));
            dt.Columns.Add("Cost Per Slot", typeof(double));
            dt.Columns.Add("Weight", typeof(double));
            dt.Columns.Add("Size", typeof(double));
            dt.Columns.Add("Grid Size", typeof(double));
            dt.Columns.Add("Max Dimension", typeof(double));
            dt.Columns.Add("Armor Class", typeof(double));
            dt.Columns.Add("Parent Weighting", typeof(double));

            foreach (LootRankingDataConfig item in lootRankingContainer.Items.Values)
            {
                DataRow row = dt.NewRow();
                row["ID"] = item.ID;
                row["Name"] = item.Name;
                row["Value"] = item.Value;
                row["Cost Per Slot"] = item.CostPerSlot;
                row["Weight"] = item.Weight;
                row["Size"] = item.Size;
                row["Grid Size"] = item.GridSize;
                row["Max Dimension"] = item.MaxDim;
                row["Armor Class"] = item.ArmorClass;
                row["Parent Weighting"] = item.ParentWeighting;
                dt.Rows.Add(row);
            }

            lootRankingDataGridView.DataSource = dt;

            lootRankingDataGridView.ResumeLayout();
            this.Cursor = Cursors.Default;
        }
    }
}