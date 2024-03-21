using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LTTPConfigEditor
{
    public partial class StringInputForm : Form
    {
        private IEnumerable<string> invalidEntries = Enumerable.Empty<string>();

        public string Input
        {
            get { return textBox.Text; }
        }

        public StringInputForm()
        {
            InitializeComponent();
        }

        public StringInputForm(string title): this()
        {
            this.Text = title;
        }

        public StringInputForm(string title, IEnumerable<string> _invalidEntries): this(title)
        {
            invalidEntries = _invalidEntries;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            if (invalidEntries.Contains(Input))
            {
                MessageBox.Show("The string " + Input + " is invalid.", "Invalid Entry", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
