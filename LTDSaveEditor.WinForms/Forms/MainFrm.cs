using LTDSaveEditor.Core;
using LTDSaveEditor.WinForms.Forms;
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

        if (!HashManager.IsInitialized)
        {
            var path = Path.Combine("Data", "GameDataListFull.csv");

            if (File.Exists(path))
                HashManager.Initialize(path);
            else
            {
                WinFormsUtility.ErrorMessage("Failed to load hashes. The file 'GameDataListFull.csv' was not found in the 'Data' folder.");
                Close();
            }
        }
    }

    private void MainFrm_DragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            e.Effect = DragDropEffects.Copy;
        else
            e.Effect = DragDropEffects.None;
        
    }

    private void OnFileDropped(object? sender, DragEventArgs e)
    {
        if(e.Data?.GetData(DataFormats.FileDrop) is string[] paths && paths.Length > 0)
        {
            var path = paths[0];
            if (ValidateSaveFolder(path))
            {
                var saveInstance = SaveInstance.FromFolder(path);
                var editorFrm = new EditorFrm(saveInstance);
                editorFrm.Show();
                Program.AppContext.ChangeMainForm(editorFrm);
                Close();
                editorFrm.Activate();
            }
        }
    }

    public string[] RequiredFiles = [
        "Map.sav", "Mii.sav", "Player.sav"
    ];

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
