using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using JWC;

// BUG: flipping "filter same CID" doesn't clear listbox when only one phash loaded
// TODO: on move or rename, update pic box ASAP ?

// ReSharper disable SuggestUseVarKeywordEvident
// ReSharper disable InconsistentNaming

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

        protected MruStripMenu mnuMRU;

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

            mnuMRU = new MruStripMenuInline(fileToolStripMenuItem, recentFilesToolStripMenuItem, OnMRU );
        }

        private void OnMRU(int number, string filename)
        {
            if (!File.Exists(filename))
            {
                mnuMRU.RemoveFile(number);
                MessageBox.Show("The file no longer exists:" + filename);
                return;
            }

            // TODO process could fail for some reason, in which case remove the file from the MRU list
            mnuMRU.SetFirstFile(number);
            ProcessPHash(filename);
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

        public class Pair : IComparable<Pair>
        {
            public FileData FileLeft { get { return _data.Get(FileLeftDex); } }
            public FileData FileRight { get{ return _data.Get(FileRightDex); } }

            public int FileLeftDex { get; set; }

            public int FileRightDex { get; set; }

            public string op { get; set; }
            public int Val { get; set; }
            public double FVal { get; set; }

            public bool CRCMatch { get; set; }

            /// Sort Pairs so that:
            /// 1. 'DUP' entries are first
            /// 2. Lower 'Val' entries are earlier (zero diff should be earliest)
            /// 3. Same diff level, sort by left filename
            public int CompareTo(Pair other)
            {
                int delta = Val - other.Val;
                if (delta == 0) 
                {
                    if (CRCMatch != other.CRCMatch)
                    {
                        if (CRCMatch)
                            return -1;
                        if (other.CRCMatch)
                            return +1;
                    }
                    return String.CompareOrdinal(FileLeft.Name, other.FileLeft.Name);
                }

                return delta;
            }

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
        };

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
        private BindingList<Pair> _viewList; // possibly filtered version of the list
        private Color _oldColor;
        private bool _filterSameCid; // are file pairings from the same CID to be filtered out?
        private int _cidCount; // the CID source id

        private void FilterOutMatchingCID()
        {
            _viewList = new BindingList<Pair>();
            int i = 0;
            foreach (var pair in _pairList)
            {
                if (!_filterSameCid || pair.FileLeft.Source != pair.FileRight.Source)
                {
                    _viewList.Add(pair);
                    i++;
                    if (i > 1000) // NOTE: 1000 pair limit. Consider 'on-demand' loading instead?
                        break; // exitloop
                }
            }
        }

        private void setListbox()
        {
            listBox1.SelectedIndex = -1;
            if (_viewList.Count > 0)
            {
                listBox1.DataSource = _viewList; //.GetRange(0, Math.Min(1000, _viewList.Count));
                listBox1.SelectedIndex = 0;
            }
        }

        private void ThreadCompareDone()
        {
            log(string.Format("compare done: {0}", _pairList.Count));

            try
            {
                _pairList.Sort();
                FilterOutMatchingCID();
                setListbox();
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

        private void LoadImage(PictureBox control, string filename)
        {
            control.SizeMode = PictureBoxSizeMode.Zoom;
            control.ImageLocation = filename;
            try
            {
                control.Image = Image.OpenNoLock(filename);
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

            bool fail1 = string.IsNullOrEmpty(pictureBox1.ImageLocation);
            bool fail2 = string.IsNullOrEmpty(pictureBox2.ImageLocation);

            if (!fail1 && !fail2)
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
                    btnStretchDiff.Enabled = true;
                }
                catch (Exception)
                {
                    ShowStatus("");
                    diffBtn.Enabled = false;
                    btnStretchDiff.Enabled = false;
                }
            }
            else
            {
                ShowStatus("");
                diffBtn.Enabled = false;
                btnStretchDiff.Enabled = false;
                if (fail1)
                    RemoveMissingFile(res.FileLeft.Name);
                if (fail2)
                    RemoveMissingFile(res.FileRight.Name);
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
            if (MoveFile(@"{0}\dup{1}_{2}", p1, p2, res.FileLeft.Name, true))
                RemoveMissingFile(res.FileLeft.Name);
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
            if (MoveFile(@"{0}\dup{1}_{2}", p1, p2, res.FileRight.Name, true))
                RemoveMissingFile(res.FileLeft.Name);
        }

        private void ClearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // clear everything
            listBox1.DataSource = null;
            listBox1.SelectedItem = null;

            _data = new FileSet(); // List<FileData>();
            _pairList = null; // failure to release memory
            _viewList = null;

            pictureBox1.ImageLocation = "";
            pictureBox2.ImageLocation = "";
            GC.Collect();
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //DoExit();
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

            Settings1.Default.MRUFiles = new StringCollection();
            Settings1.Default.MRUFiles.AddRange(mnuMRU.GetFiles());

            Settings1.Default.Save();

            // TODO save/load showdiff size/location
            // TODO handle multi monitor
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // restore size/position
            Size = Settings1.Default.MainSize;
            Location = Settings1.Default.MainLoc;
            splitContainer1.SplitterDistance = Settings1.Default.SplitDist;

            if (Settings1.Default.MRUFiles != null && Settings1.Default.MRUFiles.Count > 0)
            {
                string[] tmp = new string[Settings1.Default.MRUFiles.Count];
                Settings1.Default.MRUFiles.CopyTo(tmp, 0);
                mnuMRU.SetFiles(tmp);
            }
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
            if (MoveFile(@"{0}\{1}_{2}", destPath, destName, res.FileLeft.Name, false))
                RemoveMissingFile(res.FileLeft.Name);
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
            if (MoveFile(@"{0}\{1}_{2}", destPath, destName, res.FileRight.Name, false))
                RemoveMissingFile(res.FileRight.Name);
        }

        // ReSharper disable EmptyGeneralCatchClause
        // nameform:
        // 0: base path
        // 1: base file
        // 2: suffix number
        private bool MoveFile(string nameForm, string destPath, string destName, string origPath, bool mustRename)
        {
            // TODO dest path could be > 256 char limit. The trick is to drop characters from the end of the filename, not the path or extension.

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
                        return true;
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

            return ok;
        }

        // Helper to allocate the ShowDiff dialog once
        public ShowDiff DiffDialog
        {
            get { return _diffDlg ?? (_diffDlg = new ShowDiff {Owner = this}); }
        }

        private void doShowFile(bool left)
        {
            var res = listBox1.SelectedItem as Pair;
            if (res == null)
                return;
            var dlg = DiffDialog;
            dlg.Diff = false;
            dlg.Stretch = false;
            dlg.Pair = res;
            dlg.StartWithLeft = left;
            dlg.ShowDialog();
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
            var dlg = DiffDialog;
            dlg.Diff = true;
            dlg.Stretch = stretch;
            dlg.Pair = res;
            dlg.ShowDialog();
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
            mnuMRU.AddFile(ofd.FileName);
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

        #region PictureBox context menu

        private FolderBrowserDialog _moveDlg;
        private RenameDlg _renameDlg;

        private void moveToToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var pb = GetPictureBox(sender);

            if (_moveDlg == null)
                _moveDlg = new FolderBrowserDialog();
            _moveDlg.Description = pb.ImageLocation;
            _moveDlg.SelectedPath = Path.GetDirectoryName(pb.ImageLocation);
            DialogResult dr = _moveDlg.ShowDialog(this);
            if (dr != DialogResult.OK)
                return;
            string folder = _moveDlg.SelectedPath;

            // pattern only necessary if rename required
            MoveFile(@"{0}\{1}_{2}", folder, Path.GetFileName(pb.ImageLocation), pb.ImageLocation, false);          
        //private void moveFile(string nameForm, string destPath, string destName, string origPath, bool mustRename)
        }

        private static PictureBox GetPictureBox(object sender)
        {
            ToolStripMenuItem mi = sender as ToolStripMenuItem;
            if (mi == null)
                return null;
            ContextMenuStrip cms = mi.Owner as ContextMenuStrip;
            if (cms == null)
                return null;
            return cms.SourceControl as PictureBox;
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var pb = GetPictureBox(sender);
            if (_renameDlg == null)
                _renameDlg = new RenameDlg() { Owner = this };
            _renameDlg.OriginalName = Path.GetFileName(pb.ImageLocation);
            _renameDlg.OtherName = Path.GetFileName(pb == pictureBox1 ? pictureBox2.ImageLocation : pictureBox1.ImageLocation);
            if (_renameDlg.ShowDialog() == DialogResult.OK)
            {
                // pattern only necessary if rename required
                if (MoveFile(@"{0}\{1}_{2}", Path.GetDirectoryName(pb.ImageLocation), _renameDlg.Result, pb.ImageLocation, false))
                    RemoveMissingFile(pb.ImageLocation);
                //private void moveFile(string nameForm, string destPath, string destName, string origPath, bool mustRename)
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || pictureBox1.Image == null)
                return;
            pixContextMenuStrip.Show(pictureBox1, e.Location);
        }

        private void pictureBox2_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || pictureBox2.Image == null)
                return;
            pixContextMenuStrip.Show(pictureBox2, e.Location);
        }
        #endregion

        private void logFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("notepad.exe", _logPath);
        }

        // A file found to be missing, or marked as dup. Remove all entries which
        // reference that file from the view list.
        private void RemoveMissingFile(string path)
        {
            int oldSel = listBox1.SelectedIndex;
            int len = _viewList.Count - 1;
            for (int i = len; i >= 0; i--)
                if (_viewList[i].FileLeft.Name == path || _viewList[i].FileRight.Name == path)
                    _viewList.RemoveAt(i);
            listBox1.SelectedIndex = Math.Max(0,oldSel - 1);
        }
    }
}
