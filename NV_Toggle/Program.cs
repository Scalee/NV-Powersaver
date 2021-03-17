using System;
using System.Windows.Forms;


namespace NV_Toggle
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (Main m = new Main())
            {
                m.Display();
                Application.Run();
            }
        }
    }
}
