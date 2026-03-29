using LTDSaveEditor.Core;
using LTDSaveEditor.Core.SAV;

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

    private void GamdataTree_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (e.Node?.Tag is not uint hash) return;


        if (SaveFile.TryGetValue(hash, out var entry))
        {
            if (entry.Value is Array array)
            {
                var arrayValues = new List<string>();
                System.Collections.IList list = array;
                for (int i = 0; i < list.Count; i++)
                {
                    object? item = list[i];
                    arrayValues.Add($"{i}: {item?.ToString() ?? "null"}");
                }

                valueLabel.Text = $"Hash: {hash:X} | Type: {entry.DataType}[{array.Length}]:\n{string.Join('\n', arrayValues)}\n\n";
            }
            else valueLabel.Text = $"Hash: {hash:X} | Value: {entry.Value} | Type: {entry.DataType}";
        }
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
