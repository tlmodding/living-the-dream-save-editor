using LTDSaveEditor.Core;
using LTDSaveEditor.WinForms;
using LTDSaveEditor.WinForms.Forms;
using LTDSaveEditor.WinForms.Settings;
using LTDSaveEditor.WinForms.Utility;

class AppContext : ApplicationContext
{
    public AppContext()
    {
        if (!HashManager.IsInitialized)
        {
            var path = Path.Combine("Data", "GameDataListFull.csv");

            if (File.Exists(path))
                HashManager.Initialize(path);
            else
            {
                WinFormsUtility.ErrorMessage("Failed to load hashes. The file 'GameDataListFull.csv' was not found in the 'Data' folder.");
                return;
            }
        }

        if (UserOptions.Instance.OpenLastSaveOnStartup && !string.IsNullOrEmpty(UserOptions.Instance.LastSaveFolder))
        {
            var lastFolder = UserOptions.Instance.LastSaveFolder;
            if (Directory.Exists(lastFolder))
            {
                string[] requiredFiles = ["Map.sav", "Mii.sav", "Player.sav"];
                bool hasAllFiles = requiredFiles.All(file => File.Exists(Path.Combine(lastFolder, file)));
                if (hasAllFiles)
                {
                    var saveInstance = SaveInstance.FromFolder(lastFolder);
                    MainForm = new EditorFrm(saveInstance);
                    MainForm.Show();
                    return;
                }
            }
        }

        MainForm = new MainFrm();
        MainForm.Show();
    }

    public void ChangeMainForm(Form newForm)
    {
        MainForm = newForm;
    }

    protected override void OnMainFormClosed(object? sender, EventArgs e)
    {
        base.OnMainFormClosed(sender, e);
        UserOptions.Instance.Save();
    }
}