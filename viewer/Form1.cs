using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

// BUG: flipping "filter same CID" doesn't clear listbox when only one phash loaded

// ReSharper disable SuggestUseVarKeywordEvident

namespace pixel
{
    /// <summary>
    /// Summary description for Form1.
    /// </summary>
    public partial class Form1 : Form
    {
        private ShowDiff _diffDlg; // to retain context data

        private int _counter;

        private const int PIX_COUNT = 10; // number of "pixels" across/down 
        private const int THRESHOLD = 2000;
        private const double FTHRESHOLD = 0.7;

        private string _logPath;

        // TODO means to view the log

        public void log(string msg)
        {
            try
            {
                lock (_logPath)
                {
                    using (StreamWriter file = new StreamWriter(_logPath, true))
                    {
                        file.WriteLine(DateTime.Now.ToString("yyyy-M-d HH:mm:ss") + ":" + msg);
                    }
                }
            }
// ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }
        }

        public void log(Exception ex)
        {
            string msg = ex.Message + Environment.NewLine + ex.StackTrace;
            log(msg);
        }

        public Form1()
        {
            InitializeComponent();
            _data = new FileSet(); //List<FileData>();

            AllowDrop = true;
            DragDrop += Form1_DragDrop;
            DragEnter += Form1_DragEnter;

            var folder = Path.GetDirectoryName(Application.ExecutablePath) ?? @"C:\";
            _logPath = Path.Combine(folder, "imgComp.log");
        }

        void Form1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = IsValidDrop(e) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void ProcessPath(string path)
        {
            ImageProcessor ip = new ImageProcessor(StatusColor, ShowStatus, TaskScheduler.FromCurrentSynchronizationContext(), log);
            ip.ProcessPath(path);
        }

        private void StatusColor(Color? val)
        {
            if (val != null)
            {
                _oldColor = statusStrip1.BackColor;
                statusStrip1.BackColor = (Color)val;
            }
            else
            {
                statusStrip1.BackColor = _oldColor;
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            MessageBox.Show("drag-and-drop currently unsupported");
            //string path = DropPath(e);
            //if (path != null)
            //{
            //    ProcessPath(path);//threadProcessPath(path);
            //}
            //else
            //{
            //    path = DropCID(e);
            //    if (path == null)
            //    {
            //        path = DropFCID(e);
            //        if (path == null)
            //            return;
            //        ProcessFCID(path);
            //    }
            //    else
            //        ProcessCID(path);                // load CID
            //}
        }

        private bool IsValidDrop(DragEventArgs e)
        {
            return false; // TODO used to be able to process a path, CID or FCID
            //return (DropPath(e) != null || DropCID(e) != null || DropFCID(e) != null);
        }

        private string DropPath(DragEventArgs e)
        {
            Array data = e.Data.GetData("FileName") as Array;
            // TODO allow dropping multiple paths??
            if (data != null && data.Length == 1 && data.GetValue(0) is String)
            {
                var fn = ((string[]) data)[0];
                if (Directory.Exists(fn))
                    return Path.GetFullPath(fn);
            }
            return null;
        }

        private string DropCID(DragEventArgs e)
        {
            Array data = e.Data.GetData("FileName") as Array;

            // TODO allow dropping multiple CID files
            if (data != null && data.Length == 1 && data.GetValue(0) is String)
            {
                var fn = ((string[])data)[0];
                if (Path.GetExtension(fn).ToLower() == ".cid")
                    return Path.GetFullPath(fn);
            }
            return null;
        }

        private string DropFCID(DragEventArgs e)
        {
            Array data = e.Data.GetData("FileName") as Array;

            // TODO allow dropping multiple CID files
            if (data != null && data.Length == 1 && data.GetValue(0) is String)
            {
                var fn = ((string[])data)[0];
                fn = Path.GetFullPath(fn);
                if (Path.GetExtension(fn).ToLower() == ".fcid")
                    return fn;
            }
            return null;
        }

        //public bool processFile(string afile, StreamWriter outf)
        //{
        //    Bitmap bmp = null;
        //    try
        //    {
        //        bmp = new Bitmap(afile);
        //        var pixY = bmp.Height / PIX_COUNT;
        //        var pixX = bmp.Width / PIX_COUNT;

        //        //var res = Pixelate.BlockAvgString(bmp, pixX, pixY, PIX_COUNT*PIX_COUNT);
        //        //                var res = Pixelate.BlockWeightString(bmp, pixX, pixY, PIX_COUNT * PIX_COUNT);
        //        var res = Pixelate.BlockAvgWghtString(bmp, pixX, pixY, PIX_COUNT * PIX_COUNT);

        //        //                var res = Pixelate(bmp, pixX, pixY);
        //        //                Console.WriteLine(afile + "*");
        //        lock (outf)
        //        {
        //            outf.WriteLine(afile + "*" + res);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        string msg = afile + ":" + ex.Message;
        //        log(msg); // TODO does the log file need to be locked?
        //        _errors.Add(msg);
        //    }
        //    finally
        //    {
        //        if (bmp != null)
        //            bmp.Dispose();
        //    }
        //    return true;
        //}

        //public bool processFile2(string afile, StreamWriter outf, StreamWriter outf2)
        //{
        //    Bitmap bmp = null;
        //    string res;
        //    try
        //    {
        //        bmp = new Bitmap(afile);

        //        if (false) // pixelate variants don't work as well as Fabien's original
        //        {
        //            var pixY = bmp.Height/PIX_COUNT;
        //            var pixX = bmp.Width/PIX_COUNT;

        //            //var res = Pixelate.BlockAvgString(bmp, pixX, pixY, PIX_COUNT*PIX_COUNT);
        //            //                var res = Pixelate.BlockWeightString(bmp, pixX, pixY, PIX_COUNT * PIX_COUNT);
        //            res = Pixelate.BlockAvgWghtString(bmp, pixX, pixY, PIX_COUNT*PIX_COUNT);

        //            //                var res = Pixelate(bmp, pixX, pixY);
        //            //                Console.WriteLine(afile + "*");
        //            lock (outf)
        //            {
        //                outf.WriteLine(afile + "*" + res);
        //            }
        //        }

        //        res = Fabien.BlockString(bmp, 10, 8);
        //        lock (outf2)
        //        {
        //            outf2.WriteLine(afile + "*" + res);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        string msg = afile + ":" + ex.Message;
        //        log(msg);
        //        _errors.Add(msg);
        //    }
        //    finally
        //    {
        //        if (bmp != null)
        //            bmp.Dispose();
        //    }
        //    return true;
        //}

        //private void threadProcessPath(string path)
        //{
        //    //string outf1 = path + @"\pixel.cid";
        //    string outf2 = path + @"\pixel.fcid";
        //    //var outputF1 = new StreamWriter(outf1);
        //    var outputF2 = new StreamWriter(outf2);

        //    _guiContext = TaskScheduler.FromCurrentSynchronizationContext();
        //    _counter = 0;
        //    _errors = new List<string>();
        //    _oldColor = statusStrip1.BackColor;
        //    statusStrip1.BackColor = Color.Red;

        //    Task.Factory.StartNew(() => processFiles2(path, null, outputF2, processFile2))
        //        .ContinueWith(_ => threadsDone(null, outputF2), _guiContext);
        //    //Task.Factory.StartNew(() => processFiles2(path, outputF1, outputF2, processFile2))
        //    //    .ContinueWith(_ => threadsDone(outputF1, outputF2), _guiContext);
        //}

        //private void processFiles2(string path, StreamWriter outf, StreamWriter outf2, Func<string, StreamWriter, StreamWriter, bool> processor)
        //{
        //    var token = Task.Factory.CancellationToken;
        //    Task.Factory.StartNew(() => { ShowStatus("Processing " + _counter); }, token, TaskCreationOptions.None, _guiContext);

        //    var alltasks = new List<Task>();

        //    string[] dirs = Directory.GetDirectories(path);
        //    string[] files = Directory.GetFiles(path);
        //    foreach (var aFile in files)
        //    {
        //        string file = aFile;
        //        alltasks.Add(Task.Factory.StartNew(() => processor(file, outf, outf2)));
        //        _counter++;
        //        if (_counter % 5 == 0)
        //            Task.Factory.StartNew(() => { ShowStatus("Processing " + _counter); }, token, TaskCreationOptions.None, _guiContext);
        //    }

        //    Task.Factory.StartNew(() => { ShowStatus("Processing " + _counter); }, token, TaskCreationOptions.None, _guiContext);

        //    alltasks.AddRange(dirs.Select(aDir => Task.Factory.StartNew(() => processFiles2(aDir, outf, outf2, processor))));
        //    if (alltasks.Count > 0)
        //    {
        //        Task.WaitAll(alltasks.ToArray());
        //    }

        //    Task.Factory.StartNew(() => { ShowStatus("Processed " + _counter); }, token, TaskCreationOptions.None, _guiContext);
        //}

        //private void threadProcessFiles(string path, StreamWriter outf, Func<string,StreamWriter,bool> processor)
        //{
        //    _guiContext = TaskScheduler.FromCurrentSynchronizationContext();
        //    _counter = 0;
        //    _errors = new List<string>();
        //    _oldColor = statusStrip1.BackColor;
        //    statusStrip1.BackColor = Color.Red;
        //    Task.Factory.StartNew(() => processFiles(path, outf, processor))
        //        .ContinueWith(_ => threadsDone(outf), _guiContext);
        //}

        //private void threadsDone(StreamWriter outf)
        //{
        //    outf.Flush();
        //    outf.Close();
        //    outf.Dispose();
        //    statusStrip1.BackColor = _oldColor;
        //    ShowStatus("Done");

        //    // TODO: some mechanism to see errors. consider a button on the status bar?
        //}

        //private void threadsDone(StreamWriter outf1, StreamWriter outf2)
        //{
        //    //outf1.Flush();
        //    outf2.Flush();
        //    //outf1.Close();
        //    outf2.Close();
        //    //outf1.Dispose();
        //    outf2.Dispose();
        //    statusStrip1.BackColor = _oldColor;
        //    ShowStatus("Done");
        //}

        // TODO can this disappear and use ImageProcessor variant instead?
        // process all files/directories in a path
        private void processFiles(string path, StreamWriter outf, Func<string,StreamWriter,bool> processor )
        {
            var token = Task.Factory.CancellationToken;
            Task.Factory.StartNew(() => { ShowStatus("Processed " + _counter); }, token, TaskCreationOptions.None, _guiContext);

            var alltasks = new List<Task>();

            string[] dirs = Directory.GetDirectories(path);
            string[] files = Directory.GetFiles(path);
            foreach (var aFile in files)
            {
                string file = aFile;
                alltasks.Add( Task.Factory.StartNew(() => processor(file, outf)));
                _counter++;
                if (_counter % 5 == 0)
                    Task.Factory.StartNew(() => { ShowStatus("Processed " + _counter); }, token, TaskCreationOptions.None, _guiContext);
            }

            Task.Factory.StartNew(() => { ShowStatus("Processed " + _counter); }, token, TaskCreationOptions.None, _guiContext);

            alltasks.AddRange(dirs.Select(aDir => Task.Factory.StartNew(() => processFiles(aDir, outf, processor))));
            if (alltasks.Count > 0)
            {
                Task.WaitAll(alltasks.ToArray());
            }

            Task.Factory.StartNew(() => { ShowStatus("Processed " + _counter); }, token, TaskCreationOptions.None, _guiContext);
        }

        // TODO can this move to ImageProcessor?
        // Pixelate tester - create a PNG file showing the pixelated result
        private bool pixelateTest(string afile, StreamWriter outf)
        {
            try
            {
                var fn0 = Path.GetDirectoryName(afile) + "\\";
                var fn1 = Path.GetFileNameWithoutExtension(afile);
                var bmp = new Bitmap(afile);
                var pixY = bmp.Height / PIX_COUNT;
                var pixX = bmp.Width / PIX_COUNT;

                var pix = Pixelate.PixelateImg(bmp, pixX, pixY);
                var fn = fn0 + fn1 + "_pix.png"; // +Path.GetExtension(afile);
                pix.Save(fn, ImageFormat.Png); //bmp.RawFormat);
            }
            catch (Exception)
            {
                MessageBox.Show(afile);
            }
            return true;
        }

        public class FileData
        {
            public string Name { get; set; }
            public int[] Vals { get; set; }
            public double[] FVals { get; set; }
            public int Source { get; set; } // CID source for filtering
            public ulong PHash { get; set; } // PHash value
            public uint CRC { get; set; } // CRC value
        };

        public class Pair
        {
            public FileData FileLeft { get { return _data.Get(FileLeftDex); }
                set { } 
            }
            public FileData FileRight { get{ return _data.Get(FileRightDex); }
                set { }
            }

            public int FileLeftDex { get; set; }

            public int FileRightDex { get; set; }

            public string op { get; set; }
            public int Val { get; set; }
            public double FVal { get; set; }

            public bool CRCMatch { get; set; }

            public override string ToString()
            {
                if (Val==0 && CRCMatch)
                    return ("DUP") + " : " + FileLeft.Name + " | " + FileRight.Name;
                //int i = (int)Math.Round(FVal * 100);
                int i = Val;
                //                if (Equals(op, "F"))
                return i.ToString("000") + " : " + FileLeft.Name + " | " + FileRight.Name;
                //                return Val.ToString("D3") + op + FileLeft.Name + "|" + FileRight.Name;
            }

            // Sorting comparison.
            public static int Comparer(Pair x, Pair y)
            {
                if (x == null || y == null) // I'm not putting any null entries in the list but they're arriving here???
                    return 0;

                int val = x.Val - y.Val;
                if (val == 0) // same value, sort by name
                {
                    if (x.CRCMatch)
                        return 1;
                    if (y.CRCMatch)
                        return -1;
                    val = String.Compare(x.FileLeft.Name, y.FileLeft.Name, StringComparison.Ordinal);
                }
                return val;
            }
            public static int FComparer(Pair x, Pair y)
            {
                if (x == null || y == null) // I'm not putting any null entries in the list but they're arriving here???
                    return 0;

                if (x.FVal < y.FVal)
                    return -1;
                return String.Compare(x.FileLeft.Name, y.FileLeft.Name, StringComparison.Ordinal);
//                double val = x.FVal - y.FVal;
//                if (val < 0.000001) // same value, sort by name
//                    return String.Compare(x.FileLeft.Name, y.FileLeft.Name, StringComparison.Ordinal);
//                return val < 0 ? -1 : 1;
            }
        };
#if false
        int compareFH(int[] vals1, int[] vals2)
        {
            // compare assuming that i2 is a flipped-horizontal version of i1
            int val = 0;
            for (int y = 0; y < 10; y++ )
            {
                for (int x= 0; x < 10; x++)
                {
                    int dex1 = y*10 + x;
                    int dex2 = y*10 + (9 - x);
//                    val += Math.Abs(vals1[dex1] - vals2[dex2]);
                    int val0 = vals1[dex1] - vals2[dex2];
                    val += val0 > 0 ? val0 : -val0;
                    if (val > THRESHOLD)
                        return int.MaxValue;
                }
            }
            return val;
        }

        int compareFV(int[] vals1, int[] vals2)
        {
            // compare assuming that i2 is a flipped-vertical version of i1
            int val = 0;
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    int dex1 = y * 10 + x;
                    int dex2 = (9-y) * 10 + x;
//                    val += Math.Abs(vals1[dex1] - vals2[dex2]);
                    int val0 = vals1[dex1] - vals2[dex2];
                    val += val0 > 0 ? val0 : -val0;
                    if (val > THRESHOLD)
                        return int.MaxValue;
                }
            }
            return val;
        }

        int compareR90(int[] vals1, int[] vals2)
        {
            // compare assuming that i2 is a rotated 90-degrees version of i1
            int val = 0;
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    int dex1 = y * 10 + x;
                    int dex2 = (9 - x) * 10 + y;
//                    val += Math.Abs(vals1[dex1] - vals2[dex2]);
                    int val0 = vals1[dex1] - vals2[dex2];
                    val += val0 > 0 ? val0 : -val0;
                    if (val > THRESHOLD)
                        return int.MaxValue;
                }
            }
            return val;
        }

        int compareR180(int[] vals1, int[] vals2)
        {
            int val = 0;
            int dex1 = 0;
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    int dex2 = (9 - x) * 10 + (9-y);
//                    val += Math.Abs(vals1[dex1] - vals2[dex2]);
                    int val0 = vals1[dex1] - vals2[dex2];
                    val += val0 > 0 ? val0 : -val0;
                    if (val > THRESHOLD)
                        return int.MaxValue;
                    dex1++;
                }
            }
            return val;
        }

        int compareR270(int[] vals1, int[] vals2)
        {
            int val = 0;
            int dex1 = 0;
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    int dex2 = x * 10 + (9 - y);
//                    val += Math.Abs(vals1[dex1] - vals2[dex2]);
                    int val0 = vals1[dex1] - vals2[dex2];
                    val += val0 > 0 ? val0 : -val0;

                    if (val > THRESHOLD)
                        return int.MaxValue;
                    dex1++;
                }
            }
            return val;
        }
#endif

        private const int BLKCNTM1 = PIX_COUNT - 1;

        double compareFAll(double[] vals1, double[] vals2, out string comp)
        {
            double total = 0.0;
            int count = vals1.GetLength(0);
            for (int i = 0; i < count; i++)
            {
                total += Math.Abs(vals1[i] - vals2[i]);
            }
            comp = "F";
            return total/count;
        }

        // Make a *single* pass over the values, finding the best match
        int compareAll(int[] vals1, int[] vals2, out string comp)
        {
            int valNorm = 0;
            int valFH = 0;
            int valFV = 0;
            int valR9 = 0;
            int valR1 = 0;
            int valR2 = 0;

            int dex1 = 0;
            int diff_count = 0;
            int zero_count = 0;
            for (int y = 0; y < PIX_COUNT; y++)
            {
                for (int x = 0; x < PIX_COUNT; x++)
                {
                    // "Normal": block vs block
                    int val0 = Math.Abs(vals1[dex1] - vals2[dex1]);
                    if (val0 > 25)
                        diff_count ++;
                    if (val0 == 0)
                        zero_count++;
                    valNorm += val0;

                    // "FH": flipped horizontal
                    int dexFH = y * PIX_COUNT + (BLKCNTM1 - x);
                    val0 = vals1[dex1] - vals2[dexFH];
                    valFH += val0 > 0 ? val0 : -val0;

                    // "FV": flipped vertically
                    int dexFV = (BLKCNTM1 - y) * PIX_COUNT + x;
                    val0 = vals1[dex1] - vals2[dexFV];
                    valFV += val0 > 0 ? val0 : -val0;

                    // "R9": rotated 90 degrees
                    int dexR9 = (BLKCNTM1 - x) * PIX_COUNT + y;
                    val0 = vals1[dex1] - vals2[dexR9];
                    valR9 += val0 > 0 ? val0 : -val0;

                    // "R1": rotated 180 degrees
                    int dexR1 = (BLKCNTM1 - x) * PIX_COUNT + (BLKCNTM1 - y);
                    val0 = vals1[dex1] - vals2[dexR1];
                    valR1 += val0 > 0 ? val0 : -val0;

                    // "R2": rotated 270 degrees
                    int dexR2 = x * PIX_COUNT + (BLKCNTM1 - y);
                    val0 = vals1[dex1] - vals2[dexR2];
                    valR2 += val0 > 0 ? val0 : -val0;

                    dex1++;
                }
            }

            // find the best value
            int res = int.MaxValue;
            comp = "::";
            if (valNorm <= THRESHOLD && valNorm < res)
            {
                res = valNorm;
                comp = ":NO:";

                // NOTE test: if "same" except for a small # of major diffs, WITHOUT rotation/flipping
                if (zero_count > 50 && diff_count < 10)
                    res = res / 10;
            }

            if (true)
                return res; // NOTE temp hack

            if (valFH <= THRESHOLD && valFH < res)
            {
                res = valFH;
                comp = ":FH:";
            }
            if (valFV <= THRESHOLD && valFV < res)
            {
                res = valFV;
                comp = ":FV:";
            }
            if (valR9 <= THRESHOLD && valR9 < res)
            {
                res = valR9;
                comp = ":R9:";
            }
            if (valR1 <= THRESHOLD && valR1 < res)
            {
                res = valR1;
                comp = ":R1:";
            }
            if (valR2 <= THRESHOLD && valR2 < res)
            {
                res = valR2;
                comp = ":R2:";
            }
            return res;
        }
#if false
        int compare(int[] vals1, int[] vals2)
        {
            //int valSum1 = 0;
            //int valSum2 = 0;
            //for (int i=0; i < 100;i++)
            //{
            //    valSum1 += i1.Vals[i];
            //    valSum2 += i2.Vals[i];
            //}
            //int delta = Math.Abs(valSum1 - valSum2);

            // TODO one or more arrays have too many entries???
            // TODO use a constant
            int val = 0;
            for (int i = 0; i < 100; i++)
            {
                int val0 = vals1[i] - vals2[i];
                val += val0 > 0 ? val0 : -val0;
//                int val2 = val0 > 0 ? val0 : -val0;
//                int val2 = Math.Abs();
//                if (val2 > 1)
//                    val += val2;
                if (val > THRESHOLD)
                    return int.MaxValue;
            }

//            if (delta > THRESHOLD && val < THRESHOLD)
//            {
//                Console.WriteLine("{2},{3}:{0}|{1}", i1.Name, i2.Name, val, delta);

//                //            Console.WriteLine("{2},{3}:{0}|{1}",i1.Name, i2.Name, val,delta);
////                Console.WriteLine("{2},{3}", i1.Name, i2.Name, val, delta);                
//            }
            return val;
        }
#endif

        class FileSet
        {
            public FileSet()
            {
                Infos = new List<FileData>();
            }

            public string BasePath { get; set; }

            public List<FileData> Infos { get; set; }

            public void Add(FileData entry)
            {
                Infos.Add(entry);
            }

            public int Count { get { return Infos.Count; } }

            public FileData Get(int index)
            {
                return Infos[index];
            }
        }

        // global file context - allows loading multiple CID files
        private static FileSet _data;
        private TaskScheduler _guiContext;

        private List<Pair> _pairList;
        private List<Pair> _viewList; // possibly filtered version of the list
        private Color _oldColor;
        private bool _filterSameCid; // are file pairings from the same CID to be filtered out?
        private int _cidCount; // the CID source id

        private void ThreadCompareFiles()
        {
            // Go single-threaded

            var token = Task.Factory.CancellationToken;
//            Task.Factory.StartNew(() => { ShowStatus("Compared " + counter); }, token, TaskCreationOptions.None, guiContext);

//            var alltasks = new List<Task>();
            for (int i = 0; i < _data.Count; i++)
            {
//                int i1 = i;
//                alltasks.Add(Task.Factory.StartNew(() => CompareOneFile(i1), token));
                CompareOneFile(i);
                if (i % 5 == 0)
                {
                    int i1 = i;
                    Task.Factory.StartNew(() => ShowStatus("Comparing " + i1), token, TaskCreationOptions.None, _guiContext);
                }
            }

            //if (alltasks.Count > 0)
            //{
            //    Task.WaitAll(alltasks.ToArray());
            //}
        }

        private void ThreadCompareFFiles()
        {
            // Go single-threaded

            for (int i = 0; i < _data.Count; i++)
            {
                CompareOneFFile(i);
                if (i % 10 == 0)
                {
                    int i1 = i;
                    Task.Factory.StartNew(() => ShowStatus("Comparing " + i1), Task.Factory.CancellationToken, TaskCreationOptions.None, _guiContext);
                }
            }
        }

        private void CompareOneFFile(int me)
        {
            var myDat = _data.Get(me); // [me];
            var vals1 = myDat.FVals;
            int maxval = _data.Count;

            for (int j = me + 1; j < maxval; j++)
            {
                var aDat = _data.Get(j); //[j];
                var vals2 = aDat.FVals;
                string comp;
                double val = compareFAll(vals1, vals2, out comp);
                if (val < FTHRESHOLD)
                {
                    Pair p = new Pair { FVal = val, op = comp, FileLeft = myDat, FileRight = aDat, };
                    _pairList.Add(p);
                }
            }
        }

        private void CompareOneFile(int me)
        {
            var myDat = _data.Get(me); //[me];
            var vals1 = myDat.Vals;
            int maxval = _data.Count;

            for (int j = me + 1; j < maxval; j++)
            {
                var aDat = _data.Get(j); //[j];
                var vals2 = aDat.Vals;

                string comp;
                int val = compareAll(vals1, vals2, out comp);
#if false
#if DEBUG
                // skip comparing file against itself
                Debug.Assert(myDat.Name != aDat.Name);
#endif
                string comp = "::";
                int val = int.MaxValue;
                int val1 = compare(vals1, vals2);
                if (val1 < val)
                {
                    val = val1;
                    comp = ":NO:";
                }
                int val2 = compareFH(vals1, vals2);
                if (val2 < val)
                {
                    val = val2;
                    comp = ":FH:";
                }
                int val3 = compareFV(vals1, vals2);
                if (val3 < val)
                {
                    val = val3;
                    comp = ":FV:";
                }
                int val4 = compareR90(vals1, vals2);
                if (val4 < val)
                {
                    val = val4;
                    comp = ":R9:";
                }
                int val5 = compareR180(vals1, vals2);
                if (val5 < val)
                {
                    val = val5;
                    comp = ":R1:";
                }
                int val6 = compareR270(vals1, vals2);
                if (val6 < val)
                {
                    val = val6;
                    comp = ":R2:";
                }

                int valAll = compareAll(vals1, vals2);
                Debug.Assert(val == valAll);

//                Console.WriteLine("Comp:{0} vs {1}:{2}", me, j,val);
#endif
                if (val < THRESHOLD)
                {
                    Pair p = new Pair { Val = val, op = comp, FileLeft = myDat, FileRight = aDat, };
                    _pairList.Add(p);
                }
            }
            
        }

        private void FilterOutMatchingCID()
        {
            if (!_filterSameCid)
                _viewList = _pairList;
            else
            {
                // TODO need to do this on the GUI thread otherwise doesn't show
                //oldColor = statusStrip1.BackColor;
                //statusStrip1.BackColor = Color.Red;

                _viewList = new List<Pair>();
                foreach (var pair in _pairList)
                {
                    if (pair.FileLeft.Source != pair.FileRight.Source)
                        _viewList.Add(pair);
                }

//                statusStrip1.BackColor = oldColor;
            }
        }

        private void setListbox()
        {
            listBox1.SelectedIndex = -1;
            if (_viewList.Count > 0)
            {
                listBox1.DataSource = _viewList.GetRange(0, Math.Min(1000, _viewList.Count));
                listBox1.SelectedIndex = 0;
            }
        }

        private void ThreadCompareDone()
        {
            log(string.Format("compare done: {0}", _pairList.Count));

            ShowStatus("Compare done - sort"); // TODO need to allow the GUI thread to run to see this
            Thread.Sleep(250);
            try
            {
//                if (_FCID)
                    _pairList.Sort(Pair.FComparer);
//                else
//                    _pairList.Sort(Pair.Comparer);

                FilterOutMatchingCID();

                setListbox();

//                var token = Task.Factory.CancellationToken;
//                Task.Factory.StartNew(setListbox, token, TaskCreationOptions.None, _guiContext);

//                listBox1.DataSource = _viewList;
//                listBox1.SelectedIndex = -1;
//                if (_viewList.Count > 0)
//                    listBox1.SelectedIndex = 0;
//                Thread.Sleep(250);
                ShowStatus("Compare done");
            }
            catch (Exception ex)
            {
                log(ex);
            }
            finally
            {
                statusStrip1.BackColor = _oldColor;
            }
        }

        // TODO read entire file in, then fire off to tasks
        private void ParseCID(string filename)
        {
            char[] splitChars = {'*'};
            char[] splitChars2 = {'&'};

            var token = Task.Factory.CancellationToken;
            Task.Factory.StartNew(() => { ShowStatus("Loading CID"); }, token, TaskCreationOptions.None, _guiContext);

            using (FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (BufferedStream bs = new BufferedStream(fs))
                {
                    using (StreamReader sr = new StreamReader(bs))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            var parts1 = line.Split(splitChars);

                            // Postpone to later. kinda ugly right now (8/26/13 - 14:07)
                            //if (!File.Exists(parts1[0]))
                            //    continue; // file no longer exists, nothing to do

                            var parts2 = parts1[1].Substring(0, parts1[1].Length - 1).Split(splitChars2);

                            string name = parts1[0];
                            //name = name.Replace("G:", @"\\vmware-host\Shared Folders\HG");
                            FileData fd = new FileData
                            {
                                Name = name,
                                Vals = parts2.Select(s => int.Parse(s)).ToArray(),
                                Source = _cidCount
                            };
                            if (fd.Name == null || fd.Vals == null)
                                continue;

                            _data.Add(fd);
                        }
                    }
                }
            }

            _pairList = new List<Pair>();
            Task.Factory.StartNew(ThreadCompareFiles).ContinueWith(_ => ThreadCompareDone(), _guiContext);
        }

        public void ProcessCID(string path)
        {
            listBox1.DataSource = null; // prevent preliminary updates
            pictureBox1.ImageLocation = "";
            pictureBox2.ImageLocation = "";

            _cidCount++; // loading (another) CID

            _guiContext = TaskScheduler.FromCurrentSynchronizationContext();
            _oldColor = statusStrip1.BackColor;
            statusStrip1.BackColor = Color.Red;
            Task.Factory.StartNew(() => ParseCID(path));
        }

        private void ParseFCID(string filename)
        {
            char[] splitChars = { '*' };
            char[] splitChars2 = { '&' };

            var token = Task.Factory.CancellationToken;
            Task.Factory.StartNew(() => { ShowStatus("Loading FCID"); }, token, TaskCreationOptions.None, _guiContext);

//            bool firstLine = true;
            using (FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (BufferedStream bs = new BufferedStream(fs))
                {
                    using (StreamReader sr = new StreamReader(bs))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            var parts1 = line.Split(splitChars);
                            string name = parts1[0];
                            if (name == null)
                                continue;

                            // TODO what *is* the base when drag+drop, multi-folder, etc???
                            //if (firstLine) // TODO split out of loop?
                            //{
                            //    // First line contains base path
                            //    _data.BasePath = name;
                            //    firstLine = false;
                            //    continue;
                            //}

                            // Postpone to later. kinda ugly right now (8/26/13 - 14:07)
                            //if (!File.Exists(parts1[0]))
                            //    continue; // file no longer exists, nothing to do

                            var parts2 = parts1[1].Substring(0, parts1[1].Length - 1).Split(splitChars2);

                            //name = name.Replace("G:", @"\\vmware-host\Shared Folders\HG");
                            FileData fd = new FileData
                            {
                                Name = name,
                                FVals = parts2.Select(s => double.Parse(s)).ToArray(),
                                Source = _cidCount
                            };
                            if (fd.Name == null || fd.FVals == null)
                                continue;

                            _data.Add(fd);
                        }
                    }
                }
            }

            _pairList = new List<Pair>();
            Task.Factory.StartNew(ThreadCompareFFiles).ContinueWith(_ => ThreadCompareDone(), _guiContext);
        }

        public void ProcessFCID(string path)
        {
            listBox1.DataSource = null; // prevent preliminary updates
            pictureBox1.ImageLocation = "";
            pictureBox2.ImageLocation = "";

            _cidCount++; // loading (another) CID

            _guiContext = TaskScheduler.FromCurrentSynchronizationContext();
            _oldColor = statusStrip1.BackColor;
            statusStrip1.BackColor = Color.Red;
            Task.Factory.StartNew(() => ParseFCID(path));
        }

        // PictureBox locks the image, preventing rename
        // http://stackoverflow.com/questions/5961652/how-can-i-load-an-image-from-a-file-without-keeping-the-file-locked
        private System.Drawing.Image FromStream(string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                MemoryStream ms = new MemoryStream();
                fs.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
                return System.Drawing.Image.FromStream(ms);
            }             
        }

        private void LoadImage(PictureBox control, string filename)
        {
            control.SizeMode = PictureBoxSizeMode.Zoom;
            control.ImageLocation = filename;
            try
            {
                control.Image = FromStream(filename);
            }
            catch (Exception)
            {
                control.SizeMode = PictureBoxSizeMode.CenterImage;
                control.Image = control.ErrorImage;
                control.ImageLocation = "";
            }
        }

        private void ListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var res = listBox1.SelectedItem as Pair;
            if (res == null)
            {
                ShowStatus("");
                return;
            }

            LoadImage(pictureBox1, res.FileLeft.Name);
            LoadImage(pictureBox2, res.FileRight.Name);

            if (pictureBox1.Image != null && pictureBox2.Image != null)
            {
                var size1 = pictureBox1.Image.Size;
                var size2 = pictureBox2.Image.Size;

                try
                {
                    var info1 = new FileInfo(res.FileLeft.Name);
                    var info2 = new FileInfo(res.FileRight.Name); // 20151028 path written to phash is invalid/incorrect (japanese characters in file name)

                    double sz1 = info1.Length / 1024.0;
                    double sz2 = info2.Length / 1024.0;

                    ShowStatus(string.Format("({0},{1})[{4,-12:F}K] vs ({2},{3})[{5,-12:F}K]", size1.Height, size1.Width, size2.Height, size2.Width, sz1, sz2));

                    diffBtn.Enabled = (size1 == size2);
                }
                catch (Exception)
                {
                    ShowStatus("");
                }
            }
            else
            {
                ShowStatus("");
            }
        }

        private void BtnLeftAsDup_Click(object sender, EventArgs e)
        {
            // rename the 'left' image as a dup of the right

            var res = listBox1.SelectedItem as Pair;
            if (res == null)
                return;

            string p1 = Path.GetDirectoryName(res.FileLeft.Name);
            string p2 = Path.GetFileName(res.FileRight.Name);
            // name becomes path\dup#_file.ext
            moveFile(@"{0}\dup{1}_{2}", p1, p2, res.FileLeft.Name, true);
        }

        private void BtnRightAsDup_Click(object sender, EventArgs e)
        {
            // rename the 'right' image as a dup of the left

            var res = listBox1.SelectedItem as Pair;
            if (res == null)
                return;

            string p1 = Path.GetDirectoryName(res.FileRight.Name);
            string p2 = Path.GetFileName(res.FileLeft.Name);
            // name becomes path\dup#_file.ext
            moveFile(@"{0}\dup{1}_{2}", p1, p2, res.FileRight.Name, true);
        }

        private void ClearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // clear everything
            listBox1.DataSource = null;
            listBox1.SelectedItem = null;

            _data = new FileSet(); // List<FileData>();
            pictureBox1.ImageLocation = "";
            pictureBox2.ImageLocation = "";
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DoExit();
            Close();
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO Show about dialog
        }

        private void PixTestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // for testing: pixelate all images in a directory

            var fbd = new FolderBrowserDialog();
            if (DialogResult.OK == fbd.ShowDialog())
            {
                string path = fbd.SelectedPath;
                _guiContext = TaskScheduler.FromCurrentSynchronizationContext();
                processFiles(path, null, pixelateTest);
            }
        }

        public void ShowStatus(string text)
        {
            label1.Text = text;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DoExit();
        }

        private void DoExit()
        {
            // Save current sizes/position
            Settings1.Default.MainSize = Size;
            Settings1.Default.MainLoc = Location;
            Settings1.Default.SplitDist = splitContainer1.SplitterDistance;
            Settings1.Default.Save();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // restore size/position
            Size = Settings1.Default.MainSize;
            Location = Settings1.Default.MainLoc;
            splitContainer1.SplitterDistance = Settings1.Default.SplitDist;
        }

        private void FilterSameCIDToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            _filterSameCid = filterSameCIDToolStripMenuItem.Checked;
            FilterOutMatchingCID();
            setListbox();
        }

        private void DiffBtn_Click(object sender, EventArgs e)
        {
            DoShowDiff(false);
        }

        // Move the image from the left folder to the right folder
        private void BtnMoveRight_Click(object sender, EventArgs e)
        {
            var res = listBox1.SelectedItem as Pair;
            if (res == null)
                return;
            string destPath = Path.GetDirectoryName(res.FileRight.Name); // right folder
            string destName = Path.GetFileName(res.FileLeft.Name); // left filename

            // name becomes path\#_file.ext
            moveFile(@"{0}\{1}_{2}", destPath, destName, res.FileLeft.Name, false);
        }

        // Move the image from the right folder to the left folder
        private void BtnMoveLeft_Click(object sender, EventArgs e)
        {
            var res = listBox1.SelectedItem as Pair;
            if (res == null)
                return;
            string destPath = Path.GetDirectoryName(res.FileLeft.Name); // left folder
            string destName = Path.GetFileName(res.FileRight.Name); // right filename

            // name becomes path\#_file.ext
            moveFile(@"{0}\{1}_{2}", destPath, destName, res.FileRight.Name, false);
        }

        // ReSharper disable EmptyGeneralCatchClause
        // nameform:
        // 0: base path
        // 1: base file
        // 2: suffix number
        private void moveFile(string nameForm, string destPath, string destName, string origPath, bool mustRename)
        {
            if (!mustRename)
            {
                string destpath = Path.Combine(destPath, destName);
                if (!File.Exists(destpath))
                {
                    // First see if plain move works (no name conflict)
                    try
                    {
                        log(string.Format("Attempt to move {0} to {1}", origPath, destpath));
                        File.Move(origPath, destpath);
                        return;
                    }
                    catch (Exception ex)
                    {
                        log(ex.Message);
                    }
                }
            }

            // Then test up to 10 numbered names, using provided format
            int i = 0;
            bool ok = false;
            while (!ok && i < 10)
            {
                string newname = string.Format(nameForm, destPath, i, destName);

                try
                {
                    log(string.Format("Attempt to move {0} to {1}", origPath, newname));
                    File.Move(origPath, newname);
                    ok = true;
                }
                catch (Exception ex)
                {
                    log(ex.Message);
                    i++;
                }
            }
        }

        private void doShowFile(bool left)
        {
            var res = listBox1.SelectedItem as Pair;
            if (res == null)
                return;
            if (_diffDlg == null)
                _diffDlg = new ShowDiff { Owner = this };
            _diffDlg.Stretch = false;
            _diffDlg.Single = left ? res.FileLeft.Name : res.FileRight.Name; // TODO allow double/swap but not diff
            _diffDlg.ShowDialog();
        }

        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
            doShowFile(left:true);
        }

        private void pictureBox2_DoubleClick(object sender, EventArgs e)
        {
            doShowFile(left:false);
        }

        private void listBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int itemIndex = listBox1.IndexFromPoint(e.Location);
                if (itemIndex < 0)
                    return;
                listBox1.SelectedIndex = itemIndex;
                contextMenuStrip1.Show(listBox1, e.Location);
            }
        }

        private void filenamesToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Pair res = listBox1.SelectedItem as Pair;
            if (res == null)
                return;
            string text = String.Format("{0}{1}{2}{1}", res.FileLeft.Name, Environment.NewLine, res.FileRight.Name);
            Clipboard.SetText(text);
        }

        private void btnStretchDiff_Click(object sender, EventArgs e)
        {
            DoShowDiff(true);
        }

        private void DoShowDiff(bool stretch)
        {
            // Show the difference between the two images
            var res = listBox1.SelectedItem as Pair;
            if (res == null)
                return;
            if (_diffDlg == null)
                _diffDlg = new ShowDiff() { Owner = this };
            _diffDlg.Stretch = stretch; // TODO order dependency issue!
            _diffDlg.Pair = res;
            _diffDlg.ShowDialog();
        }

        private void loadPHashToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // a phash file consists of:
            // 1. the base path (e.g. "e:\test")
            // 2. a series of lines of the form:
            //    "n : file : file"
            //    "<hash>|<filepath>

            var ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            ofd.Filter = "PHASH files (*.phash)|*.phash|PHASHC files (*.phashc)|*.phashc";
            ofd.FilterIndex = 1;
            ofd.DefaultExt = "PHASH";
            ofd.CheckFileExists = true;
            if (DialogResult.OK != ofd.ShowDialog(this))
            {
                return;
            }
            ProcessPHash(ofd.FileName);
        }

        private void ProcessPHash(string path)
        {
            listBox1.DataSource = null; // prevent preliminary updates
            pictureBox1.ImageLocation = "";
            pictureBox2.ImageLocation = "";

            _cidCount++; // loading (another) PHASH

            _guiContext = TaskScheduler.FromCurrentSynchronizationContext();
            _oldColor = statusStrip1.BackColor;
            statusStrip1.BackColor = Color.Red;
            ParsePHash(path);
        }

        private void ParsePHash(string filename)
        {
            // new variant. lines are of the form <hash>|<filepath>
            // PHASHC variant: lines of the form <hash>|<crc>|<filepath>

            char[] splitChars = { '|' };
            _viewList = new List<Pair>();

//            var token = Task.Factory.CancellationToken;
//            Task.Factory.StartNew(() => { ShowStatus("Loading PHASH"); }, token, TaskCreationOptions.None, _guiContext);

            bool firstLine = true;
            using (FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (BufferedStream bs = new BufferedStream(fs))
                {
                    using (StreamReader sr = new StreamReader(bs))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (firstLine)
                            {
                                // First line contains base path
                                _data.BasePath = line.Trim();
                                firstLine = false;
                                continue;
                            }

                            var parts2 = line.Split(splitChars);
                            int namedex;
                            int crcdex;
                            if (parts2.Length == 2)
                            {
                                // .phash
                                namedex = 1;
                                crcdex = -1;
                            }
                            else
                            {
                                // .phashc
                                namedex = 2;
                                crcdex = 1;
                            }
                            FileData fd = new FileData
                            {
                                Name = parts2[namedex],
                                PHash = ulong.Parse(parts2[0]),
                                Source = _cidCount,
                                CRC = 0
                            };
                            if (crcdex != -1)
                                fd.CRC = uint.Parse(parts2[crcdex]);

                            if (fd.Name == null)
                                continue;

                            _data.Add(fd);
                        }
                    }
                }
            }

            _pairList = new List<Pair>();
            Task.Factory.StartNew(ThreadComparePFiles).ContinueWith(_ => ThreadCompareDone(), _guiContext);
        }

        private void ThreadComparePFiles()
        {
            // Go single-threaded

            for (int i = 0; i < _data.Count; i++)
            {
                CompareOnePFile(i);
                if (i % 10 == 0)
                {
                    int i1 = i;
                    Task.Factory.StartNew(() => ShowStatus("Comparing " + i1), Task.Factory.CancellationToken, TaskCreationOptions.None, _guiContext);
                }
            }
        }

        private const int PHASH_THRESHOLD = 25;

        private void CompareOnePFile(int me)
        {
            FileData myDat = _data.Get(me); // [me];
            int maxval = _data.Count;

            for (int j = me + 1; j < maxval; j++)
            {
                FileData aDat = _data.Get(j); //[j];
                int val = phash_ham_dist(myDat.PHash, aDat.PHash);
                if (val < PHASH_THRESHOLD)
                {
                    // was FVal = val / 200.0
                    // NOTE: ham dist is always multiple of two ...
                    Pair p = new Pair {Val = val / 2, op = "?", FileLeftDex = me, FileRightDex = j}; //FileLeft = myDat, FileRight = aDat, };
                    p.CRCMatch = myDat.CRC == aDat.CRC && myDat.CRC != 0 && aDat.CRC != 0; // hoping a legit file CRC isn't zero...
                    _pairList.Add(p);
                }
            }
        }

        // copied and adapted from the phash codebase [TODO consider p/invoking the C dll?]
        int phash_ham_dist(ulong hash1, ulong hash2)
        {
            ulong x = hash1 ^ hash2;

            const ulong m1 = 0x5555555555555555UL;
            const ulong m2 = 0x3333333333333333UL;
            const ulong h01 = 0x0101010101010101UL;
            const ulong m4 = 0x0f0f0f0f0f0f0f0fUL;

            x -= (x >> 1) & m1;
            x = (x & m2) + ((x >> 2) & m2);
            x = (x + (x >> 4)) & m4;
            return (int)((x * h01) >> 56);
        }
    }
}
