namespace LTTPConfigEditor
{
    partial class LTTPConfigEditorForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LTTPConfigEditorForm));
            this.mainToolStrip = new System.Windows.Forms.ToolStrip();
            this.openToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.saveToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.mainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.configTreeView = new System.Windows.Forms.TreeView();
            this.nodePropsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.descriptionTextBox = new System.Windows.Forms.TextBox();
            this.valueFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.topBannerFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.tempalatesLabel = new System.Windows.Forms.Label();
            this.templatesComboBox = new System.Windows.Forms.ComboBox();
            this.loadTemplateButton = new System.Windows.Forms.Button();
            this.openConfigDialog = new System.Windows.Forms.OpenFileDialog();
            this.relatedFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.mainToolStrip.SuspendLayout();
            this.mainTableLayoutPanel.SuspendLayout();
            this.nodePropsTableLayoutPanel.SuspendLayout();
            this.topBannerFlowLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainToolStrip
            // 
            this.mainToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripButton,
            this.saveToolStripButton});
            this.mainToolStrip.Location = new System.Drawing.Point(0, 0);
            this.mainToolStrip.Name = "mainToolStrip";
            this.mainToolStrip.Size = new System.Drawing.Size(984, 25);
            this.mainToolStrip.TabIndex = 0;
            this.mainToolStrip.Text = "toolStrip1";
            // 
            // openToolStripButton
            // 
            this.openToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.openToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("openToolStripButton.Image")));
            this.openToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.openToolStripButton.Name = "openToolStripButton";
            this.openToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.openToolStripButton.Text = "&Open";
            this.openToolStripButton.Click += new System.EventHandler(this.openToolStripButton_Click);
            // 
            // saveToolStripButton
            // 
            this.saveToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.saveToolStripButton.Enabled = false;
            this.saveToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("saveToolStripButton.Image")));
            this.saveToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.saveToolStripButton.Name = "saveToolStripButton";
            this.saveToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.saveToolStripButton.Text = "&Save";
            this.saveToolStripButton.Click += new System.EventHandler(this.saveToolStripButton_Click);
            // 
            // mainTableLayoutPanel
            // 
            this.mainTableLayoutPanel.ColumnCount = 2;
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 350F));
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.mainTableLayoutPanel.Controls.Add(this.configTreeView, 0, 1);
            this.mainTableLayoutPanel.Controls.Add(this.nodePropsTableLayoutPanel, 1, 1);
            this.mainTableLayoutPanel.Controls.Add(this.topBannerFlowLayoutPanel, 0, 0);
            this.mainTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTableLayoutPanel.Location = new System.Drawing.Point(0, 25);
            this.mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            this.mainTableLayoutPanel.RowCount = 2;
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainTableLayoutPanel.Size = new System.Drawing.Size(984, 436);
            this.mainTableLayoutPanel.TabIndex = 1;
            // 
            // configTreeView
            // 
            this.configTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.configTreeView.HideSelection = false;
            this.configTreeView.HotTracking = true;
            this.configTreeView.Location = new System.Drawing.Point(3, 39);
            this.configTreeView.Name = "configTreeView";
            this.configTreeView.Size = new System.Drawing.Size(344, 394);
            this.configTreeView.TabIndex = 0;
            this.configTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.ConfigNodeSelected);
            // 
            // nodePropsTableLayoutPanel
            // 
            this.nodePropsTableLayoutPanel.ColumnCount = 1;
            this.nodePropsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.nodePropsTableLayoutPanel.Controls.Add(this.descriptionTextBox, 0, 1);
            this.nodePropsTableLayoutPanel.Controls.Add(this.valueFlowLayoutPanel, 0, 2);
            this.nodePropsTableLayoutPanel.Controls.Add(this.relatedFlowLayoutPanel, 0, 3);
            this.nodePropsTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nodePropsTableLayoutPanel.Location = new System.Drawing.Point(353, 39);
            this.nodePropsTableLayoutPanel.Name = "nodePropsTableLayoutPanel";
            this.nodePropsTableLayoutPanel.RowCount = 5;
            this.nodePropsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.nodePropsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 128F));
            this.nodePropsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this.nodePropsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this.nodePropsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.nodePropsTableLayoutPanel.Size = new System.Drawing.Size(628, 394);
            this.nodePropsTableLayoutPanel.TabIndex = 2;
            // 
            // descriptionTextBox
            // 
            this.descriptionTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.descriptionTextBox.Location = new System.Drawing.Point(3, 31);
            this.descriptionTextBox.Multiline = true;
            this.descriptionTextBox.Name = "descriptionTextBox";
            this.descriptionTextBox.ReadOnly = true;
            this.descriptionTextBox.Size = new System.Drawing.Size(622, 122);
            this.descriptionTextBox.TabIndex = 0;
            // 
            // valueFlowLayoutPanel
            // 
            this.valueFlowLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.valueFlowLayoutPanel.Location = new System.Drawing.Point(3, 159);
            this.valueFlowLayoutPanel.Name = "valueFlowLayoutPanel";
            this.valueFlowLayoutPanel.Size = new System.Drawing.Size(622, 90);
            this.valueFlowLayoutPanel.TabIndex = 1;
            // 
            // topBannerFlowLayoutPanel
            // 
            this.mainTableLayoutPanel.SetColumnSpan(this.topBannerFlowLayoutPanel, 2);
            this.topBannerFlowLayoutPanel.Controls.Add(this.tempalatesLabel);
            this.topBannerFlowLayoutPanel.Controls.Add(this.templatesComboBox);
            this.topBannerFlowLayoutPanel.Controls.Add(this.loadTemplateButton);
            this.topBannerFlowLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topBannerFlowLayoutPanel.Location = new System.Drawing.Point(3, 3);
            this.topBannerFlowLayoutPanel.Name = "topBannerFlowLayoutPanel";
            this.topBannerFlowLayoutPanel.Size = new System.Drawing.Size(978, 30);
            this.topBannerFlowLayoutPanel.TabIndex = 3;
            // 
            // tempalatesLabel
            // 
            this.tempalatesLabel.AutoSize = true;
            this.tempalatesLabel.Location = new System.Drawing.Point(3, 0);
            this.tempalatesLabel.Name = "tempalatesLabel";
            this.tempalatesLabel.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.tempalatesLabel.Size = new System.Drawing.Size(219, 19);
            this.tempalatesLabel.TabIndex = 0;
            this.tempalatesLabel.Text = "Apply configuration changes from a template:";
            // 
            // templatesComboBox
            // 
            this.templatesComboBox.FormattingEnabled = true;
            this.templatesComboBox.Location = new System.Drawing.Point(228, 3);
            this.templatesComboBox.Name = "templatesComboBox";
            this.templatesComboBox.Size = new System.Drawing.Size(250, 21);
            this.templatesComboBox.TabIndex = 1;
            // 
            // loadTemplateButton
            // 
            this.loadTemplateButton.Enabled = false;
            this.loadTemplateButton.Location = new System.Drawing.Point(484, 3);
            this.loadTemplateButton.Name = "loadTemplateButton";
            this.loadTemplateButton.Size = new System.Drawing.Size(75, 23);
            this.loadTemplateButton.TabIndex = 2;
            this.loadTemplateButton.Text = "Load";
            this.loadTemplateButton.UseVisualStyleBackColor = true;
            this.loadTemplateButton.Click += new System.EventHandler(this.loadTemplateButton_Click);
            // 
            // openConfigDialog
            // 
            this.openConfigDialog.DefaultExt = "json";
            this.openConfigDialog.FileName = "config.json";
            this.openConfigDialog.Filter = "Late to the Party Configuration|config.json|All Files|*.*";
            this.openConfigDialog.Title = "Open Late to the Party Configuration";
            // 
            // relatedFlowLayoutPanel
            // 
            this.relatedFlowLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.relatedFlowLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.relatedFlowLayoutPanel.Location = new System.Drawing.Point(3, 255);
            this.relatedFlowLayoutPanel.Name = "relatedFlowLayoutPanel";
            this.relatedFlowLayoutPanel.Size = new System.Drawing.Size(622, 90);
            this.relatedFlowLayoutPanel.TabIndex = 2;
            // 
            // LTTPConfigEditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 461);
            this.Controls.Add(this.mainTableLayoutPanel);
            this.Controls.Add(this.mainToolStrip);
            this.Name = "LTTPConfigEditorForm";
            this.Text = "Late to the Party Config Editor";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LTTPConfigEditorFormClosing);
            this.mainToolStrip.ResumeLayout(false);
            this.mainToolStrip.PerformLayout();
            this.mainTableLayoutPanel.ResumeLayout(false);
            this.nodePropsTableLayoutPanel.ResumeLayout(false);
            this.nodePropsTableLayoutPanel.PerformLayout();
            this.topBannerFlowLayoutPanel.ResumeLayout(false);
            this.topBannerFlowLayoutPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip mainToolStrip;
        private System.Windows.Forms.ToolStripButton openToolStripButton;
        private System.Windows.Forms.ToolStripButton saveToolStripButton;
        private System.Windows.Forms.TableLayoutPanel mainTableLayoutPanel;
        private System.Windows.Forms.OpenFileDialog openConfigDialog;
        private System.Windows.Forms.TreeView configTreeView;
        private System.Windows.Forms.TableLayoutPanel nodePropsTableLayoutPanel;
        private System.Windows.Forms.FlowLayoutPanel topBannerFlowLayoutPanel;
        private System.Windows.Forms.Label tempalatesLabel;
        private System.Windows.Forms.ComboBox templatesComboBox;
        private System.Windows.Forms.Button loadTemplateButton;
        private System.Windows.Forms.TextBox descriptionTextBox;
        private System.Windows.Forms.FlowLayoutPanel valueFlowLayoutPanel;
        private System.Windows.Forms.FlowLayoutPanel relatedFlowLayoutPanel;
    }
}

