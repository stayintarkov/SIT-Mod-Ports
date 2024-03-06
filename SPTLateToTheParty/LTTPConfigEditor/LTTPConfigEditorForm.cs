using LTTPConfigEditor.Configuration;
using Newtonsoft.Json;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace LTTPConfigEditor
{
    public partial class LTTPConfigEditorForm : Form
    {
        private LateToTheParty.Configuration.ModConfig modConfig;
        private Configuration.ModPackageConfig modPackage;
        private Configuration.ConfigEditorConfig configEditorInfo;

        private BreadCrumbControl breadCrumbControl;
        private Dictionary<TreeNode, Type> configTypes = new Dictionary<TreeNode, Type>();
        private Dictionary<string, Action> valueButtonActions = new Dictionary<string, Action>();

        private bool isClosing = false;

        public LTTPConfigEditorForm()
        {
            InitializeComponent();

            breadCrumbControl = new BreadCrumbControl();
            breadCrumbControl.Dock = DockStyle.Fill;
            nodePropsTableLayoutPanel.Controls.Add(breadCrumbControl, 0, 0);
        }

        private void openToolStripButton_Click(object sender, EventArgs e)
        {
            if (openConfigDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            try
            {
                string packagePath = openConfigDialog.FileName.Substring(0, openConfigDialog.FileName.LastIndexOf('\\')) + "\\..\\package.json";
                modPackage = LoadConfig<Configuration.ModPackageConfig>(packagePath);

                string configEditorInfoFilename = openConfigDialog.FileName.Substring(0, openConfigDialog.FileName.LastIndexOf('\\')) + "\\configEditorInfo.json";
                configEditorInfo = LoadConfig<Configuration.ConfigEditorConfig>(configEditorInfoFilename);

                if (!IsModVersionCompatible(new Version(modPackage.Version), configEditorInfo.SupportedVersions))
                {
                    throw new InvalidOperationException("The selected configuration file is for a version of the LTTP mod that is incompatible with this version of the editor.");
                }

                modConfig = LoadConfig<LateToTheParty.Configuration.ModConfig>(openConfigDialog.FileName);
                configTypes.Clear();
                configTreeView.Nodes.AddRange(CreateTreeNodesForType(modConfig.GetType(), modConfig));

                saveToolStripButton.Enabled = true;
                openToolStripButton.Enabled = false;
                loadTemplateButton.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error when Reading Configuration", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void saveToolStripButton_Click(object sender, EventArgs e)
        {
            try
            {
                SaveConfig(openConfigDialog.FileName, modConfig);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error when Saving Configuration", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void loadTemplateButton_Click(object sender, EventArgs e)
        {

        }

        private void LTTPConfigEditorFormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("You have unsaved changes. Are you sure you want to quit?", "Unsaved Changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.No)
            {
                e.Cancel = true;
            }

            isClosing = true;
        }

        private void ConfigNodeSelected(object sender, TreeViewEventArgs e)
        {
            Action callbackAction = new Action(() => {
                ConfigNodeSelected(configTreeView, new TreeViewEventArgs(configTreeView.SelectedNode));
            });

            BreadCrumbControl.UpdateBreadCrumbControlForTreeView(breadCrumbControl, configTreeView, e.Node, callbackAction);

            string configPath = GetConfigPathForTreeNode(e.Node);
            Configuration.ConfigSettingsConfig nodeConfigInfo = GetConfigInfoForPath(configPath);
            descriptionTextBox.Text = nodeConfigInfo.Description;

            object obj = GetObjectForConfigPath(modConfig, configPath);
            CreateValueControls(valueFlowLayoutPanel, obj, configTypes[e.Node], nodeConfigInfo);
        }

        private T LoadConfig<T>(string filename)
        {
            string json = File.ReadAllText(filename);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }

        private void SaveConfig<T>(string filename, T obj)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filename, json);
        }

        private bool IsModVersionCompatible(Version modVersion, Configuration.ConfigVersionConfig configVersionsSupported)
        {
            if (modVersion.CompareTo(new Version(configVersionsSupported.Min)) < 0)
            {
                return false;
            }

            if (modVersion.CompareTo(new Version(configVersionsSupported.Max)) > 0)
            {
                return false;
            }

            return true;
        }

        private TreeNode[] CreateTreeNodesForType(Type type, object obj)
        {
            List<TreeNode> nodes = new List<TreeNode>();

            PropertyInfo[] props = type.GetProperties();
            foreach (PropertyInfo prop in props)
            {
                JsonPropertyAttribute jsonPropertyAttribute = prop.GetCustomAttribute<JsonPropertyAttribute>();
                string nodeName = jsonPropertyAttribute == null ? prop.Name : jsonPropertyAttribute.PropertyName;
                TreeNode node = new TreeNode(nodeName);
                Type propType = prop.PropertyType;

                if
                (
                    !propType.IsArray
                    && (propType != typeof(string))
                    && !(propType.IsGenericType && (propType.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                )
                {
                    node.Nodes.AddRange(CreateTreeNodesForType(propType, prop.GetValue(obj, null)));
                }

                if (propType.IsGenericType && (propType.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                {
                    IDictionary dict = prop.GetValue(obj, null) as IDictionary;
                    Type valueType = propType.GetGenericArguments()[1];
                    foreach(DictionaryEntry entry in dict)
                    {
                        TreeNode entryNode = new TreeNode(entry.Key.ToString());
                        node.Nodes.Add(entryNode);

                        Type keyType = propType.GetGenericArguments()[0];
                        var configEntryType = typeof(Configuration.ConfigDictionaryEntry<,>).MakeGenericType(keyType, valueType);
                        configTypes.Add(entryNode, configEntryType);

                        entryNode.Nodes.AddRange(CreateTreeNodesForType(entry.Value.GetType(), entry.Value));
                    }
                }

                nodes.Add(node);
                configTypes.Add(node, propType);
            }

            return nodes.ToArray();
        }

        private Configuration.ConfigSettingsConfig GetConfigInfoForPath(string configPath)
        {
            if (configEditorInfo.Settings.ContainsKey(configPath))
            {
                return configEditorInfo.Settings[configPath];
            }

            return new Configuration.ConfigSettingsConfig();
        }

        private Configuration.ConfigSettingsConfig GetConfigInfoForTreeNode(TreeNode node)
        {
            return GetConfigInfoForPath(GetConfigPathForTreeNode(node));
        }

        private static string GetConfigPathForTreeNode(TreeNode node)
        {
            List<string> nodeNames = new List<string>();
            TreeNode currentNode = node;
            while (currentNode != null)
            {
                nodeNames.Add(currentNode.Text);
                currentNode = currentNode.Parent;
            }
            nodeNames.Reverse();

            return string.Join(".", nodeNames);
        }

        private object GetObjectForConfigPath(object obj, string configPath)
        {
            ConfigSearchResult data = GetConfigPathData(obj, configPath);

            if (data.Object.GetType().Name.Contains(typeof(DictionaryEntry).Name))
            {
                DictionaryEntry de = (DictionaryEntry)data.Object;
                return de.Value;
            }

            return data.PropertyInfo.GetValue(data.Object, null);
        }

        private void SetObjectForConfigPath(object obj, string configPath, object newValue)
        {
            ConfigSearchResult data = GetConfigPathData(obj, configPath);

            if (data.Object.GetType().Name.Contains(typeof(DictionaryEntry).Name))
            {
                DictionaryEntry de = (DictionaryEntry)data.Object;
                IDictionary dict = data.PropertyInfo.GetValue(data.DictionaryObject, null) as IDictionary;

                if (dict.Contains(de.Key))
                {
                    dict[de.Key] = newValue;
                }
                else
                {
                    dict.Add(de.Key, newValue);
                }

                data.PropertyInfo.SetValue(data.DictionaryObject, dict, null);

                return;
            }

            data.PropertyInfo.SetValue(data.Object, newValue, null);
        }

        private void RemoveDictionaryEntryFromConfigPath(object obj, string configPath)
        {
            ConfigSearchResult data = GetConfigPathData(obj, configPath);

            if (data.Object.GetType().Name.Contains(typeof(DictionaryEntry).Name))
            {
                DictionaryEntry de = (DictionaryEntry)data.Object;
                IDictionary dict = data.PropertyInfo.GetValue(data.DictionaryObject, null) as IDictionary;

                if (dict.Contains(de.Key))
                {
                    dict.Remove(de.Key);
                }

                data.PropertyInfo.SetValue(data.DictionaryObject, dict, null);

                return;
            }

            throw new ArgumentException("The path \"" + configPath + "\" does not correspond with a dictionary entry.", "configPath");
        }

        private ConfigSearchResult GetConfigPathData(object obj, string configPath)
        {
            string[] pathElements = configPath.Split('.');

            PropertyInfo[] props = obj.GetType().GetProperties();
            foreach (PropertyInfo prop in props)
            {
                Type propType = prop.PropertyType;
                JsonPropertyAttribute jsonPropertyAttribute = prop.GetCustomAttribute<JsonPropertyAttribute>();
                string nodeName = jsonPropertyAttribute == null ? prop.Name : jsonPropertyAttribute.PropertyName;
                object propObj = prop.GetValue(obj, null);

                if
                (
                    (pathElements.Length > 1)
                    && (nodeName == pathElements[0])
                    && propType.IsGenericType
                    && (propType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                )
                {
                    string newConfigPath = string.Join(".", pathElements, 2, pathElements.Length - 2);

                    IDictionary dict = prop.GetValue(obj, null) as IDictionary;
                    Type valueType = propType.GetGenericArguments()[1];
                    foreach (DictionaryEntry entry in dict)
                    {
                        if (entry.Key.ToString() == pathElements[1])
                        {
                            if (newConfigPath.Length == 0)
                            {
                                return new ConfigSearchResult(prop, entry, obj);
                            }

                            return GetConfigPathData(entry.Value, newConfigPath);
                        }
                    }

                    DictionaryEntry newEntry = new DictionaryEntry(pathElements[1], Activator.CreateInstance(valueType));
                    return new ConfigSearchResult(prop, newEntry, obj);
                }

                if (nodeName != pathElements[0])
                {
                    continue;
                }

                if (pathElements.Length > 1)
                {
                    return GetConfigPathData(propObj, string.Join(".", pathElements, 1, pathElements.Length - 1));
                }

                return new ConfigSearchResult(prop, obj);
            }

            throw new InvalidOperationException("Could not extract property for config path: " + configPath);
        }

        private void RemoveValueControls(Panel panel)
        {
            foreach(Control control in panel.Controls)
            {
                TextBox textBox = control as TextBox;
                if (textBox == null)
                {
                    continue;
                }
                else
                {
                    textBox.Validating -= ValueTextBoxValidating;
                }

                Button button = control as Button;
                if (button == null)
                {
                    continue;
                }
                else
                {
                    button.Click -= ValueButtonClickAction;
                }

                CheckBox checkBox = control as CheckBox;
                if (checkBox == null)
                {
                    continue;
                }
                else
                {
                    checkBox.CheckedChanged -= ValueCheckboxCheckedChanged;
                }
            }

            panel.Controls.Clear();
            valueButtonActions.Clear();
        }

        private Action addEntryAction(TreeNode treeNode)
        {
            return new Action(() =>
            {
                string configPath = GetConfigPathForTreeNode(treeNode);
                
                Type configType = configTypes[treeNode];

                Type valueType = configType;
                if (configType.Name.Contains(typeof(Dictionary<,>).Name))
                {
                    valueType = configType.GenericTypeArguments[1];
                    configType = typeof(Configuration.ConfigDictionaryEntry<,>).MakeGenericType(configType.GenericTypeArguments[0], configType.GenericTypeArguments[1]);
                }

                IDictionary dict = GetObjectForConfigPath(modConfig, configPath) as IDictionary;

                StringInputForm stringInputForm = new StringInputForm("New Key", dict.Keys.Cast<string>());
                if (stringInputForm.ShowDialog() == DialogResult.Cancel)
                {
                    return;
                }

                object newValue = Activator.CreateInstance(valueType);
                SetObjectForConfigPath(modConfig, configPath + "." + stringInputForm.Input, newValue);

                TreeNode newNode = new TreeNode(stringInputForm.Input);

                configTypes.Add(newNode, configType);

                newNode.Nodes.AddRange(CreateTreeNodesForType(valueType, newValue));
                treeNode.Nodes.Add(newNode);
            });
        }

        private Action removeEntryAction(TreeNode treeNode)
        {
            return new Action(() =>
            {
                if (MessageBox.Show("Are you sure you want to remove this entry?", "Remove Entry", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.No)
                {
                    return;
                }

                string configPath = GetConfigPathForTreeNode(treeNode);

                Type configType = configTypes[treeNode];
                if (configType.Name.Contains(typeof(Dictionary<,>).Name))
                {
                    configType = configType.GenericTypeArguments[1];
                }

                IDictionary dict = GetObjectForConfigPath(modConfig, configPath) as IDictionary;

                RemoveDictionaryEntryFromConfigPath(modConfig, configPath);

                TreeNode parentNode = treeNode.Parent;
                parentNode.Nodes.Remove(treeNode);
            });
        }

        private Action editArrayAction(TreeNode treeNode)
        {
            return new Action(() =>
            {
                string configPath = GetConfigPathForTreeNode(treeNode);
                Type configType = configTypes[treeNode];
                object arrayObj = GetObjectForConfigPath(modConfig, configPath);

                ArrayEditorForm arrayEditorForm = new ArrayEditorForm(configType, arrayObj);
                if (arrayEditorForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                SetObjectForConfigPath(modConfig, configPath, arrayEditorForm.ArrayObject);
            });
        }

        private void CreateValueControls(Panel panel, object value, Type valueType, Configuration.ConfigSettingsConfig valueProperties)
        {
            RemoveValueControls(panel);

            if (valueType.IsArray)
            {
                Button editArrayButton = CreateValueButton("Edit Array...", editArrayAction(configTreeView.SelectedNode));
                panel.Controls.Add(editArrayButton);
                return;
            }

            if (valueType.Namespace.StartsWith("System") && valueType.Name.Contains(typeof(Dictionary<,>).Name))
            {
                Button editArrayButton = CreateValueButton("Add Entry...", addEntryAction(configTreeView.SelectedNode));
                panel.Controls.Add(editArrayButton);
                return;
            }

            if (!valueType.Namespace.StartsWith("System") && !valueType.Name.Contains(typeof(Configuration.ConfigDictionaryEntry<,>).Name))
            {
                return;
            }

            if (valueType.Name.Contains(typeof(Configuration.ConfigDictionaryEntry<,>).Name))
            {
                Button editArrayButton = CreateValueButton("Remove Entry", removeEntryAction(configTreeView.SelectedNode));
                panel.Controls.Add(editArrayButton);

                if (!valueType.GetGenericArguments()[1].Namespace.StartsWith("System"))
                {
                    return;
                }
            }

            Control valueDisplayControl = CreateValueDisplayControl(value, valueType);
            if (valueDisplayControl == null)
            {
                return;
            }

            System.Windows.Forms.Label valueLabel = new System.Windows.Forms.Label();
            valueLabel.Text = "Value:";
            Size valueLabelSize = TextRenderer.MeasureText(valueLabel.Text, valueLabel.Font);
            valueLabel.Width = valueLabelSize.Width;
            valueLabel.Padding = new Padding(0, 6, 0, 0);
            panel.Controls.Add(valueLabel);

            panel.Controls.Add(valueDisplayControl);

            if (valueProperties.Unit == "")
            {
                return;
            }

            System.Windows.Forms.Label unitLabel = new System.Windows.Forms.Label();
            unitLabel.Text = valueProperties.Unit;

            if ((valueProperties.Max < double.MaxValue) || (valueProperties.Min > double.MinValue))
            {
                unitLabel.Text += " (" + valueProperties.Min + "..." + valueProperties.Max + ")";
            }

            Size unitLabelSize = TextRenderer.MeasureText(unitLabel.Text, unitLabel.Font);
            unitLabel.Width = unitLabelSize.Width;
            unitLabel.Padding = new Padding(0, 6, 0, 0);
            panel.Controls.Add(unitLabel);
        }

        private Control CreateValueDisplayControl(object value, Type valueType)
        {
            if (valueType == typeof(bool))
            {
                CheckBox valueCheckBox = new CheckBox();
                valueCheckBox.Text = "";
                valueCheckBox.Width = 32;
                valueCheckBox.CheckedChanged += ValueCheckboxCheckedChanged;

                valueCheckBox.Checked = (bool)value;

                return valueCheckBox;
            }

            TextBox valueTextBox = new TextBox();
            valueTextBox.Width = 150;
            valueTextBox.Validating += ValueTextBoxValidating;

            valueTextBox.Text = value.ToString();

            return valueTextBox;
        }

        private Button CreateValueButton(string text, Action onClickAction)
        {
            if (valueButtonActions.ContainsKey(text))
            {
                throw new InvalidOperationException("A button with that text was already added.");
            }

            Button button = new Button();
            button.Text = text;
            Size buttonSize = TextRenderer.MeasureText(text, button.Font);
            button.Width = buttonSize.Width + 32;

            button.Click += ValueButtonClickAction;
            valueButtonActions.Add(text, onClickAction);

            return button;
        }

        private void ValueButtonClickAction(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (!valueButtonActions.ContainsKey(button.Text))
            {
                throw new InvalidOperationException("A button with that text does not have an action assigned to it.");
            }

            valueButtonActions[button.Text]();
        }

        private void ValueCheckboxCheckedChanged(object sender, EventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;

            string configPath = GetConfigPathForTreeNode(configTreeView.SelectedNode);
            SetObjectForConfigPath(modConfig, configPath, checkBox.Checked);
        }

        private void ValueTextBoxValidating(object sender, CancelEventArgs e)
        {
            if (isClosing)
            {
                return;
            }

            TextBox textBox = sender as TextBox;

            string configPath = GetConfigPathForTreeNode(configTreeView.SelectedNode);
            Configuration.ConfigSettingsConfig nodeConfigInfo = GetConfigInfoForPath(configPath);
            Type configType = configTypes[configTreeView.SelectedNode];

            if (configType.Name.Contains(typeof(ConfigDictionaryEntry<,>).Name))
            {
                configType = configType.GenericTypeArguments[1];
            }

            try
            {
                object newValueObj = Convert.ChangeType(textBox.Text, configType);

                if (double.TryParse(newValueObj.ToString(), out double newValue))
                {
                    string newValueStr = newValue.ToString();
                    if (newValueStr != textBox.Text)
                    {
                        if (MessageBox.Show("The new value " + textBox.Text + " will be saved as " + newValueStr + ". OK to proceed?", "New Value Accuracy Loss", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1) == DialogResult.No)
                        {
                            e.Cancel = true;
                            return;
                        }

                        newValueObj = Convert.ChangeType(newValueStr, configType);
                        textBox.Text = newValueStr;
                    }

                    int decimalIndex = newValueStr.IndexOf('.');
                    if ((decimalIndex > -1) && (newValueStr.Length - decimalIndex - 1 > nodeConfigInfo.DecimalPlaces))
                    {
                        throw new InvalidOperationException("The new value must have less than or equal to " + nodeConfigInfo.DecimalPlaces + " decimal places.");
                    }

                    if (newValue > nodeConfigInfo.Max)
                    {
                        throw new InvalidOperationException("New value must be less than or equal to " + nodeConfigInfo.Max);
                    }
                    if (newValue < nodeConfigInfo.Min)
                    {
                        throw new InvalidOperationException("New value must be greather than or equal to " + nodeConfigInfo.Min);
                    }
                }

                SetObjectForConfigPath(modConfig, configPath, newValueObj);
            }
            catch (FormatException)
            {
                e.Cancel = true;
                MessageBox.Show("Invalid entry. The value must be a " + configType.Name + ".", "Invalid Config Change", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch(Exception ex)
            {
                e.Cancel = true;
                MessageBox.Show(ex.Message, "Invalid Config Change", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
