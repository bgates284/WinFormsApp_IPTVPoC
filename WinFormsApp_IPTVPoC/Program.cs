using System;
using System.Windows.Forms;

namespace WinFormsApp_IPTVPoC
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Initialize application visual styles and settings
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Start the app with MainForm
            Application.Run(new MainForm());
        }
    }
}