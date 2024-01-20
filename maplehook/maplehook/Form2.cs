using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;

namespace maplehook
{
    public partial class Form2 : Form
    {
        #region External Imports

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);

        #endregion

        #region Global Variables

        Script Script = new Script();

        #endregion

        #region Event Handlers

        public static bool textIn = false;
        public static string textOO;

        #endregion

        protected override void WndProc(ref Message message)
        {
            const int WM_NCHITTEST = 0x0084;

            if (message.Msg == WM_NCHITTEST)
                return;

            base.WndProc(ref message);
            switch (message.Msg)
            {
                case 0x312:
                    switch (message.WParam.ToInt32())
                    {
                        case 1000:
                            int X = Cursor.Position.X - 3;
                            int Y = Cursor.Position.Y - 1;
                            pictureBox1.Focus();
                            toolTip1.Show("Mouse coordinates retrieved and sent.", pictureBox1);
                            Clipboard.SetText("Mouse = Point(" + X + "," + Y + ");");
                            toolTip1.Hide(pictureBox1);
                            break;

                        case 1100:
                            X = Cursor.Position.X - 3;
                            Y = Cursor.Position.Y - 1;
                            pictureBox2.Focus();
                            toolTip1.Show("Pixel color retrieved and sent.", pictureBox2);
                            Clipboard.SetText("PixelRead(" + X + "," + Y + "); " + "// The color of the pixel retrieved was: 0x" + Script.GetPixel(Cursor.Position.X - 3, Cursor.Position.Y - 1).ToString("X"));
                            toolTip1.Hide(pictureBox2);
                            break;
                    }
                    break;
            }
        }

        public Form2()
        {
            InitializeComponent();
            RegisterHotKey(this.Handle, 1000, 0, (int)Keys.F10);
            RegisterHotKey(this.Handle, 1100, 0, (int)Keys.F11);
        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }

        private void Form2_MouseHover(object sender, EventArgs e)
        {
            this.Focus();
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            OpenFileDialog OFD = new OpenFileDialog();
            OFD.Filter = "maplehook script (*.mhs)|*.mhs|All files(*.*)|*.*";
            OFD.CheckPathExists = true;

            if (DialogResult.OK == OFD.ShowDialog())
            {
                if (OFD.FileName != "")
                {
                    StreamReader SR = new StreamReader(OFD.FileName);
                    textOO = SR.ReadToEnd();
                    textIn = true;
                    SR.Close();
                }
            }
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            Stream Stream;
            SaveFileDialog SFD = new SaveFileDialog();
            SFD.Filter = "maplehook script (*.mhs)|*.mhs";
            SFD.Title = "Save your script";
            SFD.AddExtension = true;

            if (DialogResult.OK == SFD.ShowDialog())
            {
                if (SFD.FileName != "")
                {
                    if ((Stream = SFD.OpenFile()) != null)
                    {
                        StreamWriter SW = new StreamWriter(Stream);
                        SW.AutoFlush = true;

                        foreach (string line in Form3.textOut)
                        {
                            SW.WriteLine(line);
                        }
                        SW.Close();
                        Stream.Close();
                    }
                }
            }
        }
    }
}
