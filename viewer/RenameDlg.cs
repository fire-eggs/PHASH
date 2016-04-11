using System;
using System.Windows.Forms;

namespace pixel
{
    public partial class RenameDlg : Form
    {
        public string OtherName { get; set; }

        public string OriginalName { get; set; }

        public string Result { get { return textBox2.Text; } }

        public RenameDlg()
        {
            InitializeComponent();
        }

        private void RenameDlg_Load(object sender, EventArgs e)
        {
            textBox1.Text = OtherName;
            textBox2.Text = OriginalName;
        }
    }
}
