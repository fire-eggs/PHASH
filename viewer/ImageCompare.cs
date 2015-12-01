using System;
using System.Windows.Forms;

namespace pixel
{
    public class ImageCompare
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Form1 appf = new Form1();
            try
            {
                Application.Run(appf);
            }
            catch (Exception ex)
            {
                appf.log(ex);
            }
        }
    }
}