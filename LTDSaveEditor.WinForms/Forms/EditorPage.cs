using LTDSaveEditor.Core;
using LTDSaveEditor.Core.SAV;
using LTDSaveEditor.WinForms.Utility;

namespace LTDSaveEditor.WinForms.Forms;

public partial class EditorPage : UserControl
{
    public SavFile SaveFile { get; private set; }
    public EditorPage(SavFile savFile)
    {
        InitializeComponent();

        SaveFile = savFile;
        gamedataTree.AfterSelect += GamdataTree_AfterSelect;
        Load += async (_, _) => LoadGameData();
    }


    private static readonly Brush RowHeaderBrush = new SolidBrush(Color.FromArgb(255, 255, 255));

    private static readonly StringFormat RowHeaderStringFormat = new()
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center
    };

    private void GamdataTree_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (e.Node?.Tag is not uint hash) return;

        foreach(Control control in splitContainer.Panel2.Controls)
        {
            splitContainer.Panel2.Controls.Remove(control);

            if (control is IDisposable disposable)
                disposable.Dispose();
        }



        if (SaveFile.TryGetValue(hash, out var entry))
        {
            var name = HashManager.GetName(hash);

            var dgv = new DataGridView
            {
                VirtualMode = true,
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                BorderStyle = BorderStyle.None,
                EditMode = DataGridViewEditMode.EditOnF2,
                BackgroundColor = splitContainer.Panel2.BackColor,
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(30, 30, 30),
                    ForeColor = Color.White,
                    SelectionBackColor = SystemColors.Highlight,
                    SelectionForeColor = SystemColors.HighlightText
                },
                RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                AllowUserToResizeRows = false,
                AllowUserToDeleteRows = false,
                EnableHeadersVisualStyles = false,
                AllowUserToOrderColumns = false,
                AllowUserToResizeColumns = false,
                AutoGenerateColumns = false, 
                Columns = {}
            };

            dgv.RowPostPaint += (s, e) =>
            {
                var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, dgv.RowHeadersWidth, e.RowBounds.Height);
                var roxIndexName = (e.RowIndex + 1).ToString();
                e.Graphics.DrawString(roxIndexName, Font, Brushes.White, headerBounds, RowHeaderStringFormat);
            };

            splitContainer.Panel2.Controls.Add(dgv);
            dgv.Update();

            if (entry.Value is Array array)
            {
                var targetType = array.GetType().GetElementType();
                if (targetType == null) return;

                var column = CreateColumn(targetType);
                column.ValueType = targetType;
                column.HeaderText = name;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgv.Columns.Add(column);


                dgv.CellValueNeeded += (s, e) => e.Value = array.GetValue(e.RowIndex);
                dgv.CellValuePushed += (s, e) =>
                {
                    if (e.RowIndex < 0 || e.RowIndex >= array.Length)
                        return;

                    if (e.Value?.GetType() == targetType)
                        array.SetValue(e.Value, e.RowIndex);
                    else WinFormsUtility.ErrorMessage("The value you are trying to set doesn't match the array type.");
                };

                dgv.RowCount = array.Length;
            }
            else
            {
                var targetType = entry.Value?.GetType();
                if (targetType == null) return;


                var column = CreateColumn(targetType);
                column.ValueType = targetType;
                column.HeaderText = name;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgv.Columns.Add(column);

                dgv.CellValueNeeded += (s, e) => e.Value = entry.Value;
                dgv.CellValuePushed += (s, e) =>
                {
                    if (e.RowIndex < 0 || e.RowIndex > 1)
                        return;

                    if (e.Value?.GetType() == targetType)
                        entry.Value = e.Value;
                    else WinFormsUtility.ErrorMessage("The value you are trying to set doesn't match the field type.");
                };

                dgv.RowCount = 1;

            }
            
            dgv.Update();
        }
    }

    public static DataGridViewColumn CreateColumn(Type targetType)
    {
        DataGridViewColumn column;
        if (targetType == typeof(bool))
            column = new DataGridViewCheckBoxColumn();
        else if (targetType == typeof(float) || targetType == typeof(double))
            column = new DataGridViewTextBoxColumn { DefaultCellStyle = new DataGridViewCellStyle { Format = "G" } };
        else
            column = new DataGridViewTextBoxColumn();

        return column;
    }


    private async void LoadGameData()
    {
        gamedataTree.BeginUpdate();
        foreach (var (hash, entry) in SaveFile.Entries)
        {
            var name = HashManager.GetName(hash);
            var parts = name.Split('.');

            TreeNodeCollection currentLevel = gamedataTree.Nodes;
            TreeNode? currentNode = null;

            foreach (var part in parts)
            {
                // Try to find existing node at this level
                currentNode = null;

                foreach (TreeNode node in currentLevel)
                {
                    if (node.Text == part)
                    {
                        currentNode = node;
                        break;
                    }
                }

                // If not found, create it
                if (currentNode == null)
                {
                    currentNode = new TreeNode(part);
                    currentLevel.Add(currentNode);
                }

                // Move down one level
                currentLevel = currentNode.Nodes;
            }

            if (currentNode == null)
                continue;

            if (entry.DataType == DataType.Bool64bitKey && entry.Value == null)
                continue;

            if (entry.Value is Array array)
                currentNode.Text += $" ({entry.DataType}[{array.Length}])";
            else
                currentNode.Text += $" ({entry.Value})";
            currentNode.Tag = hash;
        }
        gamedataTree.EndUpdate();
    }
}
