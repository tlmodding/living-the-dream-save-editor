using System.Diagnostics;

namespace LTDSaveEditor.WinForms.Utility;

public class WinFormsUtility
{
    public static void ErrorMessage(string message)
    {
        MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    public static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
                CreateNoWindow = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{ex.Message}", "Failed to open URL", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}