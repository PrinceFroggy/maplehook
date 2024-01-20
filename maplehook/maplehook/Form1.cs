using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;

namespace maplehook
{
    public partial class Form1 : Form
    {
        #region External Imports

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, int bInheritHandle, uint dwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        #endregion

        #region Constants

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        static readonly IntPtr HWND_TOP = new IntPtr(0);
        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        /// <summary>
        /// Window handles (HWND) used for hWndInsertAfter
        /// </summary>
        public static class HWND
        {
            private static IntPtr
            NoTopMost = new IntPtr(-2),
            TopMost = new IntPtr(-1),
            Top = new IntPtr(0),
            Bottom = new IntPtr(1);
        }

        /// <summary>
        /// SetWindowPos Flags
        /// </summary>
        public static class SWP
        {
            public static readonly int
            NOSIZE = 0x0001,
            NOMOVE = 0x0002,
            NOZORDER = 0x0004,
            NOREDRAW = 0x0008,
            NOACTIVATE = 0x0010,
            DRAWFRAME = 0x0020,
            FRAMECHANGED = 0x0020,
            SHOWWINDOW = 0x0040,
            HIDEWINDOW = 0x0080,
            NOCOPYBITS = 0x0100,
            NOOWNERZORDER = 0x0200,
            NOREPOSITION = 0x0200,
            NOSENDCHANGING = 0x0400,
            DEFERERASE = 0x2000,
            ASYNCWINDOWPOS = 0x4000;
        }

        #endregion

        #region Flags

        /// <summary>
        ///     Special window handles
        /// </summary>
        public enum SpecialWindowHandles
        {
            // ReSharper disable InconsistentNaming
            /// <summary>
            ///     Places the window at the bottom of the Z order. If the hWnd parameter identifies a topmost window, the window loses its topmost status and is placed at the bottom of all other windows.
            /// </summary>
            HWND_TOP = 0,
            /// <summary>
            ///     Places the window above all non-topmost windows (that is, behind all topmost windows). This flag has no effect if the window is already a non-topmost window.
            /// </summary>
            HWND_BOTTOM = 1,
            /// <summary>
            ///     Places the window at the top of the Z order.
            /// </summary>
            HWND_TOPMOST = -1,
            /// <summary>
            ///     Places the window above all non-topmost windows. The window maintains its topmost position even when it is deactivated.
            /// </summary>
            HWND_NOTOPMOST = -2
            // ReSharper restore InconsistentNaming
        }

        [Flags]
        public enum SetWindowPosFlags : uint
        {
            // ReSharper disable InconsistentNaming

            /// <summary>
            ///     If the calling thread and the thread that owns the window are attached to different input queues, the system posts the request to the thread that owns the window. This prevents the calling thread from blocking its execution while other threads process the request.
            /// </summary>
            SWP_ASYNCWINDOWPOS = 0x4000,

            /// <summary>
            ///     Prevents generation of the WM_SYNCPAINT message.
            /// </summary>
            SWP_DEFERERASE = 0x2000,

            /// <summary>
            ///     Draws a frame (defined in the window's class description) around the window.
            /// </summary>
            SWP_DRAWFRAME = 0x0020,

            /// <summary>
            ///     Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to the window, even if the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE is sent only when the window's size is being changed.
            /// </summary>
            SWP_FRAMECHANGED = 0x0020,

            /// <summary>
            ///     Hides the window.
            /// </summary>
            SWP_HIDEWINDOW = 0x0080,

            /// <summary>
            ///     Does not activate the window. If this flag is not set, the window is activated and moved to the top of either the topmost or non-topmost group (depending on the setting of the hWndInsertAfter parameter).
            /// </summary>
            SWP_NOACTIVATE = 0x0010,

            /// <summary>
            ///     Discards the entire contents of the client area. If this flag is not specified, the valid contents of the client area are saved and copied back into the client area after the window is sized or repositioned.
            /// </summary>
            SWP_NOCOPYBITS = 0x0100,

            /// <summary>
            ///     Retains the current position (ignores X and Y parameters).
            /// </summary>
            SWP_NOMOVE = 0x0002,

            /// <summary>
            ///     Does not change the owner window's position in the Z order.
            /// </summary>
            SWP_NOOWNERZORDER = 0x0200,

            /// <summary>
            ///     Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies to the client area, the nonclient area (including the title bar and scroll bars), and any part of the parent window uncovered as a result of the window being moved. When this flag is set, the application must explicitly invalidate or redraw any parts of the window and parent window that need redrawing.
            /// </summary>
            SWP_NOREDRAW = 0x0008,

            /// <summary>
            ///     Same as the SWP_NOOWNERZORDER flag.
            /// </summary>
            SWP_NOREPOSITION = 0x0200,

            /// <summary>
            ///     Prevents the window from receiving the WM_WINDOWPOSCHANGING message.
            /// </summary>
            SWP_NOSENDCHANGING = 0x0400,

            /// <summary>
            ///     Retains the current size (ignores the cx and cy parameters).
            /// </summary>
            SWP_NOSIZE = 0x0001,

            /// <summary>
            ///     Retains the current Z order (ignores the hWndInsertAfter parameter).
            /// </summary>
            SWP_NOZORDER = 0x0004,

            /// <summary>
            ///     Displays the window.
            /// </summary>
            SWP_SHOWWINDOW = 0x0040,

            // ReSharper restore InconsistentNaming
        }


        #endregion

        #region Variables

        MapleConfig MapleStory = new MapleConfig();
        Script Script = new Script();

        Form2 Form2 = new Form2();
        Form3 Form3 = new Form3();
        
        delegate void SetFormLocationCallback();
        delegate void SetPanel1HandleCallback();
        delegate void SetPictureBoxEnableCallback();

        private uint ProcessID;
        private Thread formThread;
        private IntPtr PanelHandle;
        private IntPtr ProcessHandle;
        private IntPtr hWnd;
        private int horizontal;
        private int vertical;
        private int count;
        private bool ScriptVisible = false;
        private bool FormVisible = false;

        #endregion

        #region Event handlers

        private void Form_Location()
        {
            if (this.Form2.InvokeRequired || this.Form3.InvokeRequired)
            {
                SetFormLocationCallback SetFormLoc = new SetFormLocationCallback(Form_Location);
                this.Invoke(SetFormLoc, new object[] { });
            }
            else
            {
                this.Form2.Location = new Point(horizontal + -60, vertical + 50);
                this.Form3.Location = new Point(horizontal + -807, vertical + 10);
            }
        }

        private void Panel_Handle()
        {
            if (this.panel1.InvokeRequired)
            {
                SetPanel1HandleCallback Panel1Handle = new SetPanel1HandleCallback(Panel_Handle);
                this.Invoke(Panel1Handle, new object[] { });
            }
            else
            {
                PanelHandle = this.panel1.Handle;
            }
        }

        private void PictureBox_Enable()
        {
            if (this.pictureBox3.InvokeRequired)
            {
                SetPictureBoxEnableCallback PictureBoxEnable = new SetPictureBoxEnableCallback(PictureBox_Enable);
                this.Invoke(PictureBoxEnable, new object[] { });
            }
            else
            {
                pictureBox3.Visible = false;
            }
        }

        #endregion

        public Form1()
        {
            InitializeComponent();
            this.DesktopLocation = new Point(560, 60);
            Form2.Show();
            Form3.Show();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            formThread = new Thread(new ThreadStart(this.Form1_Thread));
            formThread.IsBackground = true;
            formThread.Start();

           // MapleStory.Launch();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                formThread.Abort();
                MapleStory.Close();
                Application.Exit();
            }
            catch (Exception)
            {
                formThread.Abort();
                Application.Exit();
            }
            finally { }
        }

        #region PictureBox handlers

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            formThread.Abort();
            MapleStory.Close();
            Application.Restart();
            Application.Exit();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            formThread.Abort();
            MapleStory.Close();
            Application.Exit();
        }

        #endregion

        #region ToolTip handlers

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ScriptVisible = false;

            if (FormVisible == false)
            {
                Form2.Show();
                Form3.Show();
            }
        }

        private void hideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ScriptVisible = true;
            Form2.Hide();
            Form3.Hide();
        }

        private void shoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormVisible = false;

            this.Show();
            if (ScriptVisible == false)
            {
                Form2.Show();
                Form3.Show();
            }
        }

        private void hideToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.Hide();
            Form2.Hide();
            Form3.Hide();
            FormVisible = true;
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            formThread.Abort();
            MapleStory.Close();
            Application.Exit();
        }

        #endregion

        private void Form1_Thread()
        {
            while (true)
            {
                horizontal = this.Right;
                vertical = this.Bottom;

                Form_Location();

                if (ProcessHandle == (IntPtr)0)
                {
                    GetWindowThreadProcessId(hWnd, out ProcessID);
                    ProcessHandle = OpenProcess(0x38, 1, ProcessID);
                    hWnd = FindWindow("MapleStoryClass", null);
                    Panel_Handle();
                    SetParent(hWnd, PanelHandle);
                    SetWindowPos(hWnd, (IntPtr)SpecialWindowHandles.HWND_TOP, -3, -25, 800, 600, SetWindowPosFlags.SWP_NOSIZE);
                }
              
                if (count == 500)
                {
                    PictureBox_Enable();
                }
                
                count++;
                Thread.Sleep(100);
            }
        }
    }
}
