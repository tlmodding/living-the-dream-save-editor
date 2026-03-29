using LTDSaveEditor.Core;
using LTDSaveEditor.Core.SAV;
using LTDSaveEditor.WinForms.Utility;
using WeifenLuo.WinFormsUI.Docking;

namespace LTDSaveEditor.WinForms.Forms;

public partial class EditorFrm : Form
{
    public SaveInstance SaveInstance { get; }
    public EditorFrm(SaveInstance instance)
    {
        InitializeComponent();

        SaveInstance = instance;

        saveToolStripMenuItem.Click += (_, _) =>
        {
            try
            {
                var dir = Path.Combine(SaveInstance.Folder, "Temp");
                Directory.CreateDirectory(dir);

                var playerPath = Path.Combine(dir, "Player.sav");
                using var playerStream = File.Create(playerPath);
                    SaveInstance.Player.Save(playerStream);
            }
            catch (Exception ex)
            {
                WinFormsUtility.ErrorMessage($"Failed to save: {ex.Message}");
            }
        };

        closeToolStripMenuItem.Click += (_, _) => Close();

        dockPanel.Theme = new VS2015DarkTheme();
        CreateTab("Player", SaveInstance.Player);
        CreateTab("Mii", SaveInstance.Mii);
        CreateTab("Map", SaveInstance.Map);
    }

    private async void CreateTab(string tabName, SavFile savFile)
    {
        var page = new EditorPage(savFile);
        var dock = DockableControl.Create(page, tabName);

        dock.Show(dockPanel, DockState.Document);
    }

    private void EditorFrm_FormClosing(object sender, FormClosingEventArgs e)
    {
        var result = MessageBox.Show("Are you sure you want to exit? Any unsaved changes will be lost.", "Confirm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        e.Cancel = result == DialogResult.No;
    }
}