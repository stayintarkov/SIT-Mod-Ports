namespace LTTPConfigEditor
{
    partial class ArrayEditorForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea4 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series4 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.mainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.arrayDataGridView = new System.Windows.Forms.DataGridView();
            this.arrayChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.mainSplitContainer = new System.Windows.Forms.SplitContainer();
            this.dialogButtonsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.mainTableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.arrayDataGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.arrayChart)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.mainSplitContainer)).BeginInit();
            this.mainSplitContainer.Panel1.SuspendLayout();
            this.mainSplitContainer.Panel2.SuspendLayout();
            this.mainSplitContainer.SuspendLayout();
            this.dialogButtonsTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainTableLayoutPanel
            // 
            this.mainTableLayoutPanel.ColumnCount = 1;
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 300F));
            this.mainTableLayoutPanel.Controls.Add(this.dialogButtonsTableLayoutPanel, 0, 1);
            this.mainTableLayoutPanel.Controls.Add(this.mainSplitContainer, 0, 0);
            this.mainTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            this.mainTableLayoutPanel.RowCount = 2;
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.mainTableLayoutPanel.Size = new System.Drawing.Size(800, 450);
            this.mainTableLayoutPanel.TabIndex = 0;
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(716, 3);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 20);
            this.cancelButton.TabIndex = 0;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(3, 3);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 20);
            this.okButton.TabIndex = 1;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // arrayDataGridView
            // 
            this.arrayDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.arrayDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.arrayDataGridView.Location = new System.Drawing.Point(0, 0);
            this.arrayDataGridView.Name = "arrayDataGridView";
            this.arrayDataGridView.Size = new System.Drawing.Size(300, 412);
            this.arrayDataGridView.TabIndex = 2;
            this.arrayDataGridView.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.arrayDataGridViewCellValidating);
            this.arrayDataGridView.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.arrayDataGridViewCellValueChanged);
            // 
            // arrayChart
            // 
            chartArea4.Name = "ChartArea1";
            this.arrayChart.ChartAreas.Add(chartArea4);
            this.arrayChart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.arrayChart.Location = new System.Drawing.Point(0, 0);
            this.arrayChart.Name = "arrayChart";
            series4.ChartArea = "ChartArea1";
            series4.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series4.Name = "Series1";
            this.arrayChart.Series.Add(series4);
            this.arrayChart.Size = new System.Drawing.Size(490, 412);
            this.arrayChart.TabIndex = 3;
            this.arrayChart.Text = "chart1";
            // 
            // mainSplitContainer
            // 
            this.mainSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainSplitContainer.Location = new System.Drawing.Point(3, 3);
            this.mainSplitContainer.Name = "mainSplitContainer";
            // 
            // mainSplitContainer.Panel1
            // 
            this.mainSplitContainer.Panel1.Controls.Add(this.arrayDataGridView);
            // 
            // mainSplitContainer.Panel2
            // 
            this.mainSplitContainer.Panel2.Controls.Add(this.arrayChart);
            this.mainSplitContainer.Size = new System.Drawing.Size(794, 412);
            this.mainSplitContainer.SplitterDistance = 300;
            this.mainSplitContainer.TabIndex = 1;
            // 
            // dialogButtonsTableLayoutPanel
            // 
            this.dialogButtonsTableLayoutPanel.ColumnCount = 2;
            this.dialogButtonsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.dialogButtonsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.dialogButtonsTableLayoutPanel.Controls.Add(this.cancelButton, 1, 0);
            this.dialogButtonsTableLayoutPanel.Controls.Add(this.okButton, 0, 0);
            this.dialogButtonsTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dialogButtonsTableLayoutPanel.Location = new System.Drawing.Point(3, 421);
            this.dialogButtonsTableLayoutPanel.Name = "dialogButtonsTableLayoutPanel";
            this.dialogButtonsTableLayoutPanel.RowCount = 1;
            this.dialogButtonsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.dialogButtonsTableLayoutPanel.Size = new System.Drawing.Size(794, 26);
            this.dialogButtonsTableLayoutPanel.TabIndex = 2;
            // 
            // ArrayEditorForm
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.mainTableLayoutPanel);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ArrayEditorForm";
            this.ShowInTaskbar = false;
            this.Text = "Edit Array";
            this.mainTableLayoutPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.arrayDataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.arrayChart)).EndInit();
            this.mainSplitContainer.Panel1.ResumeLayout(false);
            this.mainSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.mainSplitContainer)).EndInit();
            this.mainSplitContainer.ResumeLayout(false);
            this.dialogButtonsTableLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel mainTableLayoutPanel;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.DataGridView arrayDataGridView;
        private System.Windows.Forms.DataVisualization.Charting.Chart arrayChart;
        private System.Windows.Forms.TableLayoutPanel dialogButtonsTableLayoutPanel;
        private System.Windows.Forms.SplitContainer mainSplitContainer;
    }
}