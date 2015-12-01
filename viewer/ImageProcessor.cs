using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace pixel
{
    public class ImageProcessor
    {
        public delegate void StateColor(Color? val);

        public delegate void ShowState(string val);

        public delegate void Logger(string text);

        private readonly StateColor _setState;
        private readonly ShowState _showState;
        private readonly Logger _logger;

        public ImageProcessor(StateColor setState, ShowState showState, TaskScheduler guiContext, Logger log)
        {
            _setState = setState;
            _showState = showState;
            GuiContext = guiContext;
            _logger = log;
        }

        public TaskScheduler GuiContext { get; set; }

        public void ProcessPath(string path)
        {
            string outf2 = path + @"\pixel.fcid";
            var outputF2 = new StreamWriter(outf2);

            _counter = 0;
            _errors = new List<string>();
            _setState(Color.Red);

            Task.Factory.StartNew(() => ProcessFiles(path, outputF2, ProcessFile))
                .ContinueWith(_ => ThreadsDone(outputF2), GuiContext);
        }

        private void ThreadsDone(StreamWriter outf)
        {
            outf.Flush();
            outf.Close();
            outf.Dispose();
            _setState(null);  
            _showState("Done");

            // TODO: some mechanism to see errors. consider a button on the status bar?
        }

        private int _counter; // file count being processed

        private List<string> _errors;

        private void ProcessFiles(string path, StreamWriter outf, Func<string, StreamWriter, bool> processor)
        {
            var token = Task.Factory.CancellationToken;
            Task.Factory.StartNew(() => _showState("Processing " + _counter), token, TaskCreationOptions.None, GuiContext);

            var alltasks = new List<Task>();

            string[] dirs = Directory.GetDirectories(path);
            string[] files = Directory.GetFiles(path);
            foreach (var aFile in files)
            {
                string file = aFile;
                alltasks.Add(Task.Factory.StartNew(() => processor(file, outf)));
                _counter++;
                if (_counter % 5 == 0)
                    Task.Factory.StartNew(() => _showState("Processing " + _counter), token, TaskCreationOptions.None, GuiContext);
            }

            Task.Factory.StartNew(() => _showState("Processing " + _counter), token, TaskCreationOptions.None, GuiContext);

            alltasks.AddRange(dirs.Select(aDir => Task.Factory.StartNew(() => ProcessFiles(aDir, outf, processor), token)));
            if (alltasks.Count > 0)
            {
                Task.WaitAll(alltasks.ToArray());
            }

            Task.Factory.StartNew(() => _showState("Processed " + _counter), token, TaskCreationOptions.None, GuiContext);
        }

        public bool ProcessFile(string afile, StreamWriter outf)
        {
            using (Bitmap bmp = new Bitmap(afile))
            {
                try
                {
                    string res = Fabien.BlockString(bmp, 10, 8);
                    lock (outf)
                    {
                        outf.WriteLine(afile + "*" + res);
                    }
                }
                catch (Exception ex)
                {
                    string msg = afile + ":" + ex.Message;
                    _logger(msg);
                    _errors.Add(msg);
                }
            }
            _counter--;
            Task.Factory.StartNew(() => _showState("Processed " + _counter), Task.Factory.CancellationToken, TaskCreationOptions.None, GuiContext);
            return true;
        }

    }
}
