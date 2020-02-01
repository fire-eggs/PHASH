using System;
using System.IO;
using System.Threading;
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
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => HandleException("AppDomain.UnhandledException", (Exception)args.ExceptionObject);
            Application.ThreadException += (sender, args) => HandleException("Application.ThreadException", args.Exception);

            try
            {
                Application.EnableVisualStyles();
                //Application.SetCompatibleTextRenderingDefault(false);
                appf = new Form1();
                Application.Run(appf);
            }
            catch (Exception ex)
            {
                appf.log("app.run", ex);
            }
        }

        private static Form1 appf;

        private static void HandleException(string from, Exception ex)
        {
            var msg = string.Format("Exception on thread ID {0}, handled via {1}", Thread.CurrentThread.ManagedThreadId, from);
            string msg2 = ex.Message + Environment.NewLine + ex.StackTrace;
            var folder = Path.GetDirectoryName(Application.ExecutablePath) ?? @"C:\";
            var _logPath = Path.Combine(folder, "imgComp.log");

            using (StreamWriter file = new StreamWriter(_logPath, true))
            {
                file.WriteLine(DateTime.Now.ToString("yyyy-M-d HH:mm:ss") + ":" + msg);
            }
        }
    }
}