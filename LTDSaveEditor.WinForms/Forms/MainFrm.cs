using LTDSaveEditor.Core;
using LTDSaveEditor.WinForms.Forms;
using LTDSaveEditor.WinForms.Settings;
using LTDSaveEditor.WinForms.Utility;

namespace LTDSaveEditor.WinForms;

public partial class MainFrm : Form
{
    public MainFrm()
    {
        InitializeComponent();

        MinimumSize = Size;
        MaximumSize = Size;

        instructionLabel.Location = new Point((ClientSize.Width / 2) - (instructionLabel.Width / 2), ClientSize.Height / 2 + 55);

        TopMost = true;

        // Add drag-n-drop folder functionality
        AllowDrop = true;
        DragEnter += MainFrm_DragEnter;
        DragDrop += OnFileDropped;
        Click += MainFrm_Click;
    }

    public string[] RequiredFiles = [
        "Map.sav", "Mii.sav", "Player.sav"
    ];

    private async void MainFrm_Click(object? sender, EventArgs e)
    {
        var openFolderDialog = new FolderBrowserDialog();

        if (openFolderDialog.ShowDialog() == DialogResult.OK)
            ValidateAndOpenSave(openFolderDialog.SelectedPath);
    }

    private void MainFrm_DragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            e.Effect = DragDropEffects.Copy;
        else
            e.Effect = DragDropEffects.None;
    }

    private async void OnFileDropped(object? sender, DragEventArgs e)
    {
        if(e.Data?.GetData(DataFormats.FileDrop) is string[] paths && paths.Length > 0)
            ValidateAndOpenSave(paths[0]);
    }

    private async void ValidateAndOpenSave(string saveFolder)
    {
        if (!ValidateSaveFolder(saveFolder))
            return;

        UserOptions.Instance.LastSaveFolder = saveFolder;

        var saveInstance = SaveInstance.FromFolder(saveFolder);
        var editorFrm = new EditorFrm(saveInstance);
        editorFrm.Show();
        Program.AppContext.ChangeMainForm(editorFrm);
        Close();
        editorFrm.Activate();
    }

    public bool ValidateSaveFolder(string saveFolder)
    {
        if (File.Exists(saveFolder)) { 
            WinFormsUtility.ErrorMessage("Please drop a folder, not a file.");
            return false;
        }

        if (!Directory.Exists(saveFolder))
        {
            WinFormsUtility.ErrorMessage("The specified folder does not exist.");
            return false;
        }

        var files = Directory.GetFiles(saveFolder).Select(x => Path.GetFileName(x));

        if (!RequiredFiles.All(y => files.Contains(y, StringComparer.OrdinalIgnoreCase)))
        {
            WinFormsUtility.ErrorMessage("The specified folder does not contain the required save files.");
            return false;
        }

        return true;
    }
}
