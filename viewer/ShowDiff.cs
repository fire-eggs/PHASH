using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

// ReSharper disable SuggestUseVarKeywordEvident

namespace pixel
{
    public partial class ShowDiff : Form
    {
        private Form1.Pair _pair;
        private bool _swap;
        private Size _mySize;
        private Point _myLoc;
        private string _path;

        public ShowDiff()
        {
            InitializeComponent();
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
        }

        public bool Stretch { get; set; }

        public Form1.Pair Pair
        {
            set
            {
                _path = "";
                _pair = value;
                doImage();
            }
        }

        public string Single
        {
            set { _path = value; doImage(); }
        }

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

        private void doImage()
        {
            try
            {
                button1.Enabled = (_path == "");
                if (_path != "")
                {
                    Text = "";
                    pictureBox1.Image = FromStream(_path);
                    return;
                }

                if (!_swap)
                {
                    Text = "Left vs Right";
                    pictureBox1.Image = Image.kbrDiff(_pair.FileLeft.Name, _pair.FileRight.Name, Stretch);
                }
                else
                {
                    Text = "Right vs Left";
                    pictureBox1.Image = Image.kbrDiff(_pair.FileRight.Name, _pair.FileLeft.Name, Stretch);
                }
            }
            catch (Exception)
            {
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            _swap = !_swap;
            doImage();
        }

        private void ShowDiff_FormClosing(object sender, FormClosingEventArgs e)
        {
            pictureBox1.Image = null;
            _mySize = Size;
            _myLoc = Location;
        }

        private void ShowDiff_Load(object sender, EventArgs e)
        {
            if (_mySize.IsEmpty)
                return;
            Size = _mySize;
            Location = _myLoc;
        }

        private void btnActualSize_Click(object sender, EventArgs e)
        {
            // TODO change button text
            // TODO auto-set to center if image smaller than window size
            if (pictureBox1.SizeMode == PictureBoxSizeMode.Zoom)
                pictureBox1.SizeMode = PictureBoxSizeMode.CenterImage;
            else
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
        }

        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
            Close();
        }
    }
}
