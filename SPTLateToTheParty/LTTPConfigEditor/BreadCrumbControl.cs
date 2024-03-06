using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LTTPConfigEditor
{
    public class BreadCrumbControl: FlowLayoutPanel
    {
        private static Dictionary<LinkLabel, Action> linkLabelActions = new Dictionary<LinkLabel, Action>();

        public BreadCrumbControl()
        {

        }

        public void RemoveAllBreadCrumbs()
        {
            UnlinkAllBreadCrumbs();
            linkLabelActions.Clear();
            this.Controls.Clear();
        }

        public void AddBreadCrumb(string text, Action action)
        {
            LinkLabel breadCrumb = CreateBreadCrumb(text, action);

            if (linkLabelActions.ContainsKey(breadCrumb))
            {
                throw new InvalidOperationException("Duplicate bread crumb already exists.");
            }

            linkLabelActions.Add(breadCrumb, action);

            if (linkLabelActions.Count > 1)
            {
                this.Controls.Add(GetSeparator());
            }
            this.Controls.Add(breadCrumb);
        }

        public static void UpdateBreadCrumbControlForTreeView(BreadCrumbControl breadCrumbControl, TreeView treeView, TreeNode selectedNode, Action callbackAction)
        {
            breadCrumbControl.RemoveAllBreadCrumbs();

            List<TreeNode> nodes = new List<TreeNode>();

            TreeNode currentNode = selectedNode;
            while (currentNode != null)
            {
                nodes.Add(currentNode);
                currentNode = currentNode.Parent;
            }

            nodes.Reverse();
            foreach (TreeNode node in nodes)
            {
                Action clickAction = new Action(() =>
                {
                    treeView.SelectedNode = node;
                    callbackAction();
                });

                breadCrumbControl.AddBreadCrumb(node.Text, clickAction);
            }
        }

        private static LinkLabel CreateBreadCrumb(string text, Action action)
        {
            LinkLabel label = new LinkLabel();
            label.Name = text;
            label.Text = text;
            Size labelSize = TextRenderer.MeasureText(label.Text, label.Font);
            label.Width = labelSize.Width;

            if (action != null)
            {
                label.Click += PerformAction;
            }

            return label;
        }

        private static Label GetSeparator()
        {
            Label separator = new Label();
            separator.Text = ">";
            Size separatorSize = TextRenderer.MeasureText(separator.Text, separator.Font);
            separator.Width = separatorSize.Width;

            return separator;
        }

        private static void PerformAction(object sender, EventArgs args)
        {
            LinkLabel linkLabel = sender as LinkLabel;
            
            if (!linkLabelActions.ContainsKey(linkLabel))
            {
                return;
            }

            linkLabelActions[linkLabel]();
        }

        private void UnlinkAllBreadCrumbs()
        {
            foreach (Control control in this.Controls)
            {
                UnlinkBreadCrumb(control.Name);
            }
        }

        private void UnlinkBreadCrumb(string text)
        {
            if (text.Length == 0)
            {
                return;
            }

            this.Controls[text].Click -= PerformAction;
        }
    }
}
