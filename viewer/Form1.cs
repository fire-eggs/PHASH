using JWC;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

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

        //private int _counter;

        //private const int PIX_COUNT = 10; // number of "pixels" across/down 
        //private const int THRESHOLD = 2000;
        //private const double FTHRESHOLD = 0.7;

        private static string _logPath;

        protected MruStripMenu mnuMRU;

        public static void log(string msg)
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

        public void log(string msg2, Exception ex)
        {
            string msg = msg2 + ":" + ex.Message + Environment.NewLine + ex.StackTrace;
            log(msg);
        }

        public Form1()
        {
            InitializeComponent();
//            _data = new FileSet(); //List<FileData>();

            AllowDrop = false;  // TODO restore drag-and-drop support?
            //DragDrop += Form1_DragDrop;
            //DragEnter += Form1_DragEnter;

            var folder = Path.GetDirectoryName(Application.ExecutablePath) ?? @"C:\";
            _logPath = Path.Combine(folder, "imgComp.log");

            mnuMRU = new MruStripMenuInline(fileToolStripMenuItem, recentFilesToolStripMenuItem, OnMRU );
            mnuMRU.MaxEntries = 6;
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

        //void Form1_DragEnter(object sender, DragEventArgs e)
        //{
        //    e.Effect = IsValidDrop(e) ? DragDropEffects.Copy : DragDropEffects.None;
        //}

        //private void ProcessPath(string path)
        //{
        //    ImageProcessor ip = new ImageProcessor(StatusColor, ShowStatus, TaskScheduler.FromCurrentSynchronizationContext(), log);
        //    ip.ProcessPath(path);
        //}

        //private void StatusColor(Color? val)
        //{
        //    if (val != null)
        //    {
        //        _oldColor = statusStrip1.BackColor;
        //        statusStrip1.BackColor = (Color)val;
        //    }
        //    else
        //    {
        //        statusStrip1.BackColor = _oldColor;
        //    }
        //}

        //private void Form1_DragDrop(object sender, DragEventArgs e)
        //{
        //    string path = DropPath(e);
        //    if (path != null)
        //    {
        //        ProcessPath(path);//threadProcessPath(path);
        //    }
        //    else
        //    {
        //        path = DropCID(e);
        //        if (path == null)
        //        {
        //            path = DropFCID(e);
        //            if (path == null)
        //                return;
        //            ProcessFCID(path);
        //        }
        //        else
        //            ProcessCID(path);                // load CID
        //    }
        //}

        //private bool IsValidDrop(DragEventArgs e)
        //{
        //    return false; // TODO used to be able to process a path, CID or FCID
        //    //return (DropPath(e) != null || DropCID(e) != null || DropFCID(e) != null);
        //}

        //private string DropPath(DragEventArgs e)
        //{
        //    Array data = e.Data.GetData("FileName") as Array;
        //    // TODO allow dropping multiple paths??
        //    if (data != null && data.Length == 1 && data.GetValue(0) is String)
        //    {
        //        var fn = ((string[]) data)[0];
        //        if (Directory.Exists(fn))
        //            return Path.GetFullPath(fn);
        //    }
        //    return null;
        //}

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
// Perf. hit                    return String.CompareOrdinal(FileLeft.Name, other.FileLeft.Name);
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

            public string TTip()
            {
                return string.Format("{0}{1}{2}", FileLeft.Name, Environment.NewLine, FileRight.Name);
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
            if (_pairList == null)
                return;
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
            if (_viewList != null && _viewList.Count > 0)
            {
                listBox1.DataSource = _viewList; //.GetRange(0, Math.Min(1000, _viewList.Count));
                listBox1.SelectedIndex = 0;
            }
            else
            {
                listBox1.DataSource = null;
                ClearPictureBoxes();
            }
        }

        private void ThreadCompareDone()
        {
            log($"compare done: {_pairList.Count}");

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

        private void LoadImage(PictureBox control, string filename, System.Drawing.Image img)
        {
            try
            {
                control.SizeMode = PictureBoxSizeMode.Zoom;
                control.ImageLocation = filename;
                control.Image = img;
            }
            catch (Exception)
            {
                control.SizeMode = PictureBoxSizeMode.CenterImage;
                control.Image = control.ErrorImage;
                control.ImageLocation = "";
            }
        }

        private System.Drawing.Image LoadImage(string filename)
        {
            try
            {
                return Image.OpenNoLock(filename);
            }
            catch (Exception e)
            {
                return null;
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

            bool existL = File.Exists(res.FileLeft.Name);
            bool existR = File.Exists(res.FileRight.Name);
            if (!existL || !existR)
            {
                clearOnFail();
                return;
            }

            var imgL = LoadImage(res.FileLeft.Name);
            var imgR = LoadImage(res.FileRight.Name);

            existL = imgL == null;
            existR = imgR == null;
            var animL = !existL && ImageAnimator.CanAnimate(imgL);
            var animR = !existR && ImageAnimator.CanAnimate(imgR);

            // a) file may exist but fail to load
            // b) if one image is animated and the other is not, don't show
            if (animR != animL || existL || existR)
            {
                clearOnFail();
                return;
            }

            LoadImage(pictureBox1, res.FileLeft.Name, imgL);
            LoadImage(pictureBox2, res.FileRight.Name, imgR);

            var size1 = pictureBox1.Image.Size;
            var size2 = pictureBox2.Image.Size;

            try
            {
                var info1 = new FileInfo(res.FileLeft.Name);
                var info2 = new FileInfo(res.FileRight.Name); 

                double sz1 = info1.Length / 1024.0;
                double sz2 = info2.Length / 1024.0;

                ShowStatus(string.Format("({0},{1})[{4,-12:F}K] vs ({2},{3})[{5,-12:F}K]", 
                           size1.Height, size1.Width, 
                           size2.Height, size2.Width, sz1, sz2));

                diffBtn.Enabled = (size1 == size2);
                btnStretchDiff.Enabled = true;
            }
            catch (Exception)
            {
                ShowStatus("");
                diffBtn.Enabled = false;
                btnStretchDiff.Enabled = false;
            }

            void clearOnFail()
            {
                ShowStatus("");
                diffBtn.Enabled = false;
                btnStretchDiff.Enabled = false;
                if (!existL)
                    RemoveMissingFile(res.FileLeft.Name);
                if (!existR)
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

            _data = null; //new FileSet(); // List<FileData>();
            _pairList = null; // failure to release memory
            _viewList = null;
            ClearPictureBoxes();

            GC.Collect();

//            Console.WriteLine("Total Memory: {0}", GC.GetTotalMemory(false));
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO Show about dialog
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
            // KBR 20200130 Need to restore if minimized
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;

            // Save current sizes/position
            Settings1.Default.MainSize = Size;
            Settings1.Default.MainLoc = Location;
            Settings1.Default.SplitDist = splitContainer1.SplitterDistance;

            Settings1.Default.MRUFiles = new StringCollection();
            Settings1.Default.MRUFiles.AddRange(mnuMRU.GetFiles());

            if (!DiffDialog.LastSize.IsEmpty)
            {
                Settings1.Default.DlgLoc = DiffDialog.LastLoc;
                Settings1.Default.DlgSize = DiffDialog.LastSize;
            }

            Settings1.Default.Save();

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

            DiffDialog.LastLoc = Settings1.Default.DlgLoc;
            DiffDialog.LastSize = Settings1.Default.DlgSize;
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
            if (!mustRename)
            {
                string destpath = Path.Combine(destPath, destName);
                if (!File.Exists(destpath))
                {
                    // First see if plain move works (no name conflict)
                    if (SimpleMoveFile(origPath, destpath) == MOVE_OK)
                        return true;
                }
            }

            // Then test up to 10 numbered names, using provided format
            for (int i = 0; i < 10; i++)
            {
                string newname = string.Format(nameForm, destPath, i, destName);
                int res = SimpleMoveFile(origPath, newname);
                if (res == MOVE_OK)
                    return true;
                if (res == MOVE_FILE_TOO_LONG)
                {
                    // TODO provide a rename assistant? How many characters need to be removed?
                    MessageBox.Show("The resulting file path is too long. Rename the original or pick a shorter destination. See the log for details.");
                    return false;
                }
            }

            MessageBox.Show("All attempts to move the file failed. See the log for details.");
            return false;
        }

        private int MOVE_OK = 1;
        private int MOVE_FILE_TOO_LONG = -1;
        private int MOVE_FAIL = 0;

        private int SimpleMoveFile(string origPath, string destpath)
        {
            try
            {
                log(string.Format("Attempt to move {0} to {1}", origPath, destpath));
                File.Move(origPath, destpath);
                return MOVE_OK;
            }
            catch (PathTooLongException)
            {
                log("Destination path too long.");
                return MOVE_FILE_TOO_LONG;
            }
            catch (Exception ex)
            {
                log(ex.Message);
                return MOVE_FAIL;
            }
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

            // KBR 20170110 for some reason the clipboard operation failed
            // TODO still need a try/catch anyway?
            Clipboard.SetDataObject(text, true, 5, 100);
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
            ofd.Filter = "PHASHC files (*.phashc)|*.phashc|PHASHD files (*.phashd)|*.phashd";
            ofd.FilterIndex = 1;
            ofd.DefaultExt = "PHASHC";
            ofd.CheckFileExists = true;
            if (DialogResult.OK != ofd.ShowDialog(this))
            {
                return;
            }
            mnuMRU.AddFile(ofd.FileName);
            ProcessPHash(ofd.FileName);
        }

        private void ClearPictureBoxes()
        {
            if (pictureBox1.Image != null) 
                pictureBox1.Image.Dispose();
            pictureBox1.Image = null;
            pictureBox1.ImageLocation = "";
            if (pictureBox2.Image != null) 
                pictureBox2.Image.Dispose();
            pictureBox2.Image = null;
            pictureBox2.ImageLocation = "";
        }

        private void ProcessPHash(string path)
        {
            if (_data == null)
                _data = new FileSet();

            listBox1.DataSource = null; // prevent preliminary updates
            _viewList = null;
            ClearPictureBoxes();

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
                if (i % 100 == 0) // 20170110 reduce updates from 10 to 100: slight speedup & less memory usage
                {
                    int i1 = i;
                    Task.Factory.StartNew(() => ShowStatus("Comparing " + i1), Task.Factory.CancellationToken, TaskCreationOptions.None, _guiContext);
                }
            }
        }

        private const int PHASH_THRESHOLD = 18; // 20170110 reduce number of useless entries being added 25;

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
            if (MoveFile(@"{0}\{1}_{2}", folder, Path.GetFileName(pb.ImageLocation), pb.ImageLocation, false))
                RemoveMissingFile(pb.ImageLocation);
          
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
            {
                _renameDlg = new RenameDlg {Owner = this};
                _renameDlg.StartPosition = FormStartPosition.CenterParent;
            }

            _renameDlg.OriginalName = Path.GetFileName(pb.ImageLocation);
            _renameDlg.OtherName = Path.GetFileName(pb == pictureBox1 ? pictureBox2.ImageLocation : pictureBox1.ImageLocation);
            if (_renameDlg.ShowDialog() == DialogResult.OK)
            {
                // pattern only necessary if rename required
                if (MoveFile(@"{0}\{1}_{2}", Path.GetDirectoryName(pb.ImageLocation), _renameDlg.Result, pb.ImageLocation, false))
                    RemoveMissingFile(pb.ImageLocation);
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
            // TODO still having problems around the end of the list trying to set SelectedIndex past the end

            try
            {
                int oldSel = listBox1.SelectedIndex;
                int len = _viewList.Count - 1;
                for (int i = len; i >= 0; i--)
                    if (_viewList[i].FileLeft.Name == path || _viewList[i].FileRight.Name == path)
                        _viewList.RemoveAt(i);

                ListBox1_SelectedIndexChanged(null,null);

                //int newSel = Math.Min(len-1, oldSel);
                //listBox1.SelectedIndex = newSel;
                //if (oldSel == 0)
                //    ListBox1_SelectedIndexChanged(null, null);
            }
            catch (Exception ex)
            {
                log(ex); // TODO stop crashing when search runs past end/beginning
            }
        }

        private void listBox1_MouseMove(object sender, MouseEventArgs e)
        {
            // Tooltip handling for the listbox: allows seeing full paths without adding horizontal scroll
            ListBox lb = (ListBox) sender;
            int index = lb.IndexFromPoint(e.Location);
            if (index < 0)
            {
                toolTip1.Hide(lb);
                return;
            }

            Pair pair = _viewList[index];
            string ttString = pair.TTip();
            if (toolTip1.GetToolTip(lb) != ttString)
                toolTip1.SetToolTip(lb, ttString);
        }

        private void rightDupsToFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (StreamWriter file = new StreamWriter(@"E:\pixel_dups.txt", false))
            {
                foreach (Pair pair in _viewList)
                {
                    if (pair.Val == 0 && pair.CRCMatch)
                    {
                        file.WriteLine(pair.FileRight.Name);
                    }
                }
            }
        }

        private void lockLeftDupButtonToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            btnLeftAsDup.Enabled = !lockLeftDupButtonToolStripMenuItem.Checked;
        }

        private void lockRightDupButtonToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            btnRightAsDup.Enabled = !lockRightDupButtonToolStripMenuItem.Checked;
        }
    }
}
