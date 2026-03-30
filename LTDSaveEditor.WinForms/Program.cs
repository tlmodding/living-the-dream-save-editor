using LTDSaveEditor.Core;
using LTDSaveEditor.WinForms.Utility;

namespace LTDSaveEditor.WinForms;

internal static class Program
{
    internal static AppContext AppContext { get; private set; } = new AppContext();

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        if (!HashManager.IsInitialized)
        {
            var path = Path.Combine("Data", "GameDataListFull.csv");

            if (File.Exists(path))
                HashManager.Initialize(path);
            else
            {
                WinFormsUtility.ErrorMessage("Failed to load hashes. The file 'GameDataListFull.csv' was not found in the 'Data' folder.");
                Application.ExitThread();
                return;
            }
        }

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.SetColorMode(SystemColorMode.Dark);
        Application.Run(AppContext);
    }
}