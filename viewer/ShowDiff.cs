using System;
using System.Drawing;
using System.Windows.Forms;

// ReSharper disable SuggestUseVarKeywordEvident
// ReSharper disable InconsistentNaming

namespace pixel
{
    public partial class ShowDiff : Form
    {
        private bool _swap;
        private Size _mySize;
        private Point _myLoc;

        public ShowDiff()
        {
            InitializeComponent();
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
        }

        // Indicates if the dialog is to show the difference or not
        public bool Diff { get; set; }

        public bool Stretch { get; set; }

        public bool StartWithLeft { get; set; }

        public Form1.Pair Pair { private get; set; }

        private string InitText
        {
            get { return StartWithLeft ? "Left Image" : "Right Image"; }
        }
        private string SwapText
        {
            get { return StartWithLeft ? "Right Image" : "Left Image"; }
        }

        private string PairPath
        {
            get
            {
                if (StartWithLeft)
                    return _swap ? Pair.FileRight.Name : Pair.FileLeft.Name;
                return _swap ? Pair.FileLeft.Name : Pair.FileRight.Name;
            }
        }

        // cache the calculated bitmaps rather than rebuild each time the user toggles
        private Bitmap bmpLvR;
        private Bitmap bmpRvL;

        private Bitmap LVR
        {
            get { return bmpLvR ?? (bmpLvR = Image.kbrDiff(Pair.FileLeft.Name, Pair.FileRight.Name, Stretch)); }
        }
        private Bitmap RVL
        {
            get { return bmpRvL ?? (bmpRvL = Image.kbrDiff(Pair.FileRight.Name, Pair.FileLeft.Name, Stretch)); }
        }

        private void doImage()
        {

            try
            {
                if (!_swap)
                {
                    Text = Diff ? "Left vs Right" : InitText;
                    pictureBox1.Image = Diff ? LVR : Image.OpenNoLock(PairPath);
                }
                else
                {
                    Text = Diff ? "Right vs Left" : SwapText;
                    pictureBox1.Image = Diff ? RVL : Image.OpenNoLock(PairPath);
                }
            }
            catch (Exception)
            {
            }
        }

        private void btnSwap_Click(object sender, EventArgs e)
        {
            _swap = !_swap;
            doImage();
        }

        private void ShowDiff_FormClosing(object sender, FormClosingEventArgs e)
        {
            pictureBox1.Image = null;
            _mySize = Size;
            _myLoc = Location;
            bmpLvR = null;
            bmpRvL = null;
            Pair = null;
        }

        private void ShowDiff_Load(object sender, EventArgs e)
        {
            doImage();
            if (_mySize.IsEmpty)
                return;
            Size = _mySize;
            Location = _myLoc;
        }

        private void btnActualSize_Click(object sender, EventArgs e)
        {
            // TODO auto-set to center if image smaller than window size
            // TODO auto-set to 'actual size' if image smaller than window size
            if (pictureBox1.SizeMode == PictureBoxSizeMode.Zoom)
            {
                pictureBox1.SizeMode = PictureBoxSizeMode.CenterImage;
                btnActualSize.Text = "Fit to window";
            }
            else
            {
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                btnActualSize.Text = "Actual size";
            }
        }

        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
            Close();
        }
    }
}
