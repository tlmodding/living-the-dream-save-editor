using LTDSaveEditor.WinForms.Settings;

namespace LTDSaveEditor.WinForms.Forms;

public partial class OptionsFrm : Form
{
    public OptionsFrm()
    {
        InitializeComponent();

        propertyGrid.SelectedObject = UserOptions.Instance;
    }
}
