using LTDSaveEditor.Core;
using LTDSaveEditor.Core.SAV;
using LTDSaveEditor.WinForms.Forms.TreeViewCells;
using LTDSaveEditor.WinForms.Settings;
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

        if (SaveFile.TryGetEntry(hash, out var entry))
        {
            var data = HashManager.TryGetData(hash, out var d) ? d : null;
            var name = data?.Name ?? "< Unknown >";

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
                var roxIndexName = e.RowIndex.ToString();
                e.Graphics.DrawString(roxIndexName, Font, Brushes.White, headerBounds, RowHeaderStringFormat);
            };

            splitContainer.Panel2.Controls.Add(dgv);
            dgv.Update();

            if (entry.Value is Array array)
            {
                var targetType = array.GetType().GetElementType();
                if (targetType == null) return;

                var column = CreateColumn(entry, data, targetType);
                column.ValueType = targetType;
                column.HeaderText = name;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgv.Columns.Add(column);

                dgv.EditingControlShowing += (s, e) =>
                {
                    if (e.Control is TextBox textBox && entry.DataType is DataType.EnumArray)
                    {
                        if (dgv.CurrentCell == null) return;

                        var value = array.GetValue(dgv.CurrentCell.RowIndex);
                        if (value != null) textBox.Text = value.ToString();
                    }
                };

                dgv.CellFormatting += (s, e) =>
                {
                    if (entry.DataType is DataType.EnumArray && array.GetValue(e.RowIndex) is uint enumHash)
                    {
                        e.Value = GetFormattedHash(data, enumHash);
                    }
                };

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


                var column = CreateColumn(entry, data, targetType);
                column.ValueType = targetType;
                column.HeaderText = name;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgv.Columns.Add(column);

                dgv.EditingControlShowing += (s, e) =>
                {
                    if (e.Control is TextBox textBox && entry.DataType is DataType.Enum)
                        textBox.Text = entry.Value?.ToString();
                };

                dgv.CellFormatting += (s, e) =>
                {
                    if (entry.DataType is DataType.Enum && entry.Value is uint enumHash)
                    {
                        e.Value = GetFormattedHash(data, enumHash);
                    }
                };

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

    private string GetFormattedHash(GameData? data, uint enumHash)
    {
        return UserOptions.Instance.EnumDisplayMode switch
        {
            EnumDisplayMode.Name => data != null && data.Options.TryGetValue(enumHash, out var t) ? t : GetHashfallback(enumHash),
            EnumDisplayMode.Hash => enumHash.ToString("X"),
            EnumDisplayMode.Number => enumHash.ToString(),
            _ => throw new NotImplementedException(),
        };
    }

    private string GetHashfallback(uint enumHash) => enumHash.ToString("X");

    public static DataGridViewColumn CreateColumn(SavFileEntry entry, GameData? data, Type targetType)
    {
        DataGridViewColumn column;
        if (targetType == typeof(bool))
            column = new DataGridViewCheckBoxColumn();
        else if (targetType == typeof(float) || targetType == typeof(double))
            column = new DataGridViewTextBoxColumn { DefaultCellStyle = new DataGridViewCellStyle { Format = "G" } };
        else
            column = new DataGridViewTextBoxColumn();

        if (entry.DataType == DataType.Enum || entry.DataType == DataType.EnumArray)
            column.CellTemplate = new MMH3DataGridCell(data?.Options ?? []);

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

            if (UserOptions.Instance.ShowFlagType)
            {
                if (entry.Value is Array array)
                    currentNode.Text += $" ({entry.DataType}[{array.Length}])";
                else
                    currentNode.Text += $" ({entry.DataType})";
            }
            currentNode.Tag = hash;
        }

        gamedataTree.Sort();
        gamedataTree.EndUpdate();
    }
}
