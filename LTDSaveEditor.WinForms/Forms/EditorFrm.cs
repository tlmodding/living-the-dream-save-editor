using LTDSaveEditor.Core;
using LTDSaveEditor.Core.SAV;
using LTDSaveEditor.WinForms.Utility;
using WeifenLuo.WinFormsUI.Docking;

namespace LTDSaveEditor.WinForms.Forms;

public partial class EditorFrm : Form
{
    public SaveInstance SaveInstance { get; }
    public BackupManager BackupManager { get; }
    public bool sessionBackupCreated = false;

    public EditorFrm(SaveInstance instance)
    {
        InitializeComponent();

        SaveInstance = instance;
        BackupManager = new BackupManager(SaveInstance.Folder);

        saveToolStripMenuItem.Click += (_, _) =>
        {
            if (!sessionBackupCreated)
            {
                BackupManager.CreateBackup(SaveInstance.Player.Path, SaveInstance.Mii.Path, SaveInstance.Map.Path);
                sessionBackupCreated = true;
            }

            try
            {
                var dir = Path.Combine(SaveInstance.Folder, "Temp");
                Directory.CreateDirectory(dir);

                var playerPath = Path.Combine(dir, "Player.sav");
                SaveInstance.Player.SaveTo(playerPath);

                var miiPath = Path.Combine(dir, "Mii.sav");
                SaveInstance.Mii.SaveTo(miiPath);

                var mapPath = Path.Combine(dir, "Map.sav");
                SaveInstance.Map.SaveTo(mapPath);
            }
            catch (Exception ex)
            {
                WinFormsUtility.ErrorMessage($"Failed to save: {ex.Message}");
            }
        };

        closeToolStripMenuItem.Click += (_, _) => Close();
        optionsToolStripMenuItem.Click += (_, _) => new OptionsFrm().ShowDialog();

        dockPanel.Theme = new VS2015DarkTheme();
        var playerTab = CreateTab("Player", SaveInstance.Player);
        CreateTab("Mii", SaveInstance.Mii);
        CreateTab("Map", SaveInstance.Map);

        playerTab.Activate();
    }

    private DockableControl<EditorPage> CreateTab(string tabName, SavFile savFile)
    {
        var page = new EditorPage(savFile);
        var dock = DockableControl.Create(page, tabName);

        dock.Show(dockPanel, DockState.Document);

        return dock;
    }

    private void EditorFrm_FormClosing(object sender, FormClosingEventArgs e)
    {
        var result = MessageBox.Show("Are you sure you want to exit? Any unsaved changes will be lost.", "Confirm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        e.Cancel = result == DialogResult.No;
    }
}