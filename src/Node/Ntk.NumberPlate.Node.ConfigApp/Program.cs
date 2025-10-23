using Ntk.NumberPlate.Node.ConfigApp.Forms;
namespace Ntk.NumberPlate.Node.ConfigApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}


