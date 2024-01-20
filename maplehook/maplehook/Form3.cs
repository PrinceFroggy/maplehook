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
using maplehook;

namespace maplehook
{
    public partial class Form3 : Form
    {
        #region External Imports

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);

        #endregion

        #region Variables

        Script Script = new Script();
        
        delegate void SetLineCallback();

        private Thread LineThread;
        private int lastLine = 0;
        private bool highLightCheck = false;
        private bool scriptStart = false;

        public static string[] textOut;

        #endregion

        #region Event handler
       
        private void Line_Enable()
        {
            if (this.richTextBox1.InvokeRequired)
            {
                SetLineCallback LineEnable = new SetLineCallback(Line_Enable);
                this.Invoke(LineEnable, new object[] { });
            }
            else
            {
                if (Script.ScriptStoped == false)
                {
                    HighlightCurrentLine();

                    if (highLightCheck == true)
                    {
                        highLightCheck = false;
                    }
                }
                else
                {
                    richTextBox1.ReadOnly = false;

                    if (highLightCheck == false)
                    {
                        ClearHighlight();
                        highLightCheck = true;
                    }

                    if (Form2.textIn == true)
                    {
                        richTextBox1.Text = Form2.textOO;
                        Form2.textIn = false;
                    }
                }
            }
        }

        private void HighlightCurrentLine()
        {
            int selectionStart = richTextBox1.SelectionStart;
            int selectionLength = richTextBox1.SelectionLength;

            int ScriptCL = Script.CurrentLine;
            int firstCharPosition = richTextBox1.GetFirstCharIndexFromLine(ScriptCL);
            int lineNumber = richTextBox1.GetLineFromCharIndex(firstCharPosition);
            int lastCharPosition = richTextBox1.GetFirstCharIndexFromLine(lineNumber + 1);
            if (lastCharPosition == -1)
                lastCharPosition = richTextBox1.TextLength;

            if (lineNumber != lastLine)
            {
                if (richTextBox1.Text != String.Empty && richTextBox1.GetLineFromCharIndex(richTextBox1.TextLength) + 1 != 1)
                {
                    int previousFirstCharPosition = richTextBox1.GetFirstCharIndexFromLine(lastLine);
                    int previousLastCharPosition = richTextBox1.GetFirstCharIndexFromLine(lastLine + 1);
                    if (previousLastCharPosition == -1)
                        previousLastCharPosition = richTextBox1.TextLength;

                    richTextBox1.SelectionStart = previousFirstCharPosition;
                    richTextBox1.SelectionLength = previousLastCharPosition - previousFirstCharPosition;
                    richTextBox1.SelectionBackColor = SystemColors.Window;
                    lastLine = lineNumber;
                }
                lastLine = lineNumber;
            }

            if (richTextBox1.Text != String.Empty && richTextBox1.GetLineFromCharIndex(richTextBox1.TextLength) + 1 != 1)
            {
                richTextBox1.SelectionStart = firstCharPosition;
                richTextBox1.SelectionLength = lastCharPosition - firstCharPosition;
                if (richTextBox1.SelectionLength > 0)
                    richTextBox1.SelectionBackColor = Color.LightBlue;
            }
            else
            {
                richTextBox1.SelectionStart = 0;
                richTextBox1.SelectionLength = lastCharPosition;
                if (richTextBox1.SelectionLength > 0)
                    richTextBox1.SelectionBackColor = Color.LightBlue;
            }

            richTextBox1.SelectionStart = selectionStart;
            richTextBox1.SelectionLength = selectionLength;
        }

        private void ClearHighlight()
        {
            richTextBox1.SelectionStart = 0;
            richTextBox1.SelectionLength = richTextBox1.Text.Length;
            richTextBox1.SelectionBackColor = Color.White;
        }

        #endregion

        #region ContextMenu

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Text = Clipboard.GetText();
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }

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
                        case 9000:
                            if (scriptStart == false)
                            {
                                Script.StartScript(richTextBox1.Lines);
                                richTextBox1.ReadOnly = true;
                                scriptStart = true;
                            }
                            else
                            {
                                ClearHighlight();
                                Script.StopScript();
                                richTextBox1.ReadOnly = false;
                                scriptStart = false;
                            }
                            break;
                    }
                    break;
            }
        }

        public Form3()
        {
            InitializeComponent();

            RegisterHotKey(this.Handle, 9000, 0, (int)Keys.F9);
            treeView1.Nodes.Add("Key");
            treeView1.Nodes.Add("Mouse");
            treeView1.Nodes.Add("Event");

            treeView1.Nodes[0].Nodes.Add("SendKeys(\"string\");");
            treeView1.Nodes[0].Nodes[0].ToolTipText = "Type the string.";
            treeView1.Nodes[0].Nodes.Add("SendKey(control);");
            treeView1.Nodes[0].Nodes[1].ToolTipText = "Press a key.";
            treeView1.Nodes[0].Nodes.Add("HoldKey(control);");
            treeView1.Nodes[0].Nodes[2].ToolTipText = "Hold down a key.";
            treeView1.Nodes[0].Nodes.Add("ReleaseKey(control);");
            treeView1.Nodes[0].Nodes[3].ToolTipText = "Release the key.";

            treeView1.Nodes[1].Nodes.Add("Mouse = Point(x, y);");
            treeView1.Nodes[1].Nodes[0].ToolTipText = "Change the mouse posistion to the a point.";
            treeView1.Nodes[1].Nodes.Add("Mouse.X = 0;");
            treeView1.Nodes[1].Nodes[1].ToolTipText = "Change the mouse X posistion.";
            treeView1.Nodes[1].Nodes.Add("Mouse.Y = 0;");
            treeView1.Nodes[1].Nodes[2].ToolTipText = "Change the mouse Y posistion.";
            treeView1.Nodes[1].Nodes.Add("LeftClick;");
            treeView1.Nodes[1].Nodes[3].ToolTipText = "Perform a left click.";
            treeView1.Nodes[1].Nodes.Add("RightClick;");
            treeView1.Nodes[1].Nodes[4].ToolTipText = "Perform a right click.";

            treeView1.Nodes[2].Nodes.Add("Sleep(Mili secs);");
            treeView1.Nodes[2].Nodes[0].ToolTipText = "Sleep for a desired amount of mili seconds.";
            treeView1.Nodes[2].Nodes.Add("Sleep(Random(Min, Max));");
            treeView1.Nodes[2].Nodes[1].ToolTipText = "Sleeps for a random amount of time between min and max";
            treeView1.Nodes[2].Nodes.Add("goto(line or label);");
            treeView1.Nodes[2].Nodes[2].ToolTipText = "Go to the desired line or label.";
            treeView1.Nodes[2].Nodes.Add("Loop (X times)");
            treeView1.Nodes[2].Nodes[3].ToolTipText = "Loop the desired amount of times.";
            treeView1.Nodes[2].Nodes.Add("LeaveLoop;");
            treeView1.Nodes[2].Nodes[4].ToolTipText = "Leave the current loop.";
            treeView1.Nodes[2].Nodes.Add("LeaveWhileLoop;");
            treeView1.Nodes[2].Nodes[5].ToolTipText = "Leave the current while loop no matter if the values are equal or not.";
            treeView1.Nodes[2].Nodes.Add("Random(min, max)");
            treeView1.Nodes[2].Nodes[6].ToolTipText = "Random number between min and max";
            treeView1.Nodes[2].Nodes.Add("PixelRead(x, y)");
            treeView1.Nodes[2].Nodes[7].ToolTipText = "Reads the pixel color at the selected posistion";
            treeView1.Nodes[2].Nodes.Add("End;");
            treeView1.Nodes[2].Nodes[8].ToolTipText = "Stops the script.";
            treeView1.Nodes[2].Nodes.Add("Terminate;");
            treeView1.Nodes[2].Nodes[9].ToolTipText = "Terminates this process.\nUse \"Terminate(\"process name\");\" to terminate all process with that name.";
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            LineThread = new Thread(new ThreadStart(this.Line_Thread));
            LineThread.IsBackground = true;
            LineThread.Start();
        }

        private void Form3_MouseHover(object sender, EventArgs e)
        {
            this.Focus();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            int i = richTextBox1.SelectionStart;
            if (richTextBox1.Text.Contains("	"))
            {
                richTextBox1.Text = richTextBox1.Text.Replace("	", "       ");
                richTextBox1.SelectionStart = i + 6;
            }
            textOut = richTextBox1.Lines;
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            string str = treeView1.SelectedNode.Text;
            if (str != "Key" && str != "Mouse" && str != "Event")
            {
                Clipboard.SetText(str.Replace("Mili secs", "1000").Replace("x, y", "0, 0").Replace("line or label", "1").Replace("Loop (X times)", "Loop (10)\r\n{\r\n\r\n}"));
            }
        }

        private void Line_Thread()
        {
            while (true)
            {
                Line_Enable();
                Thread.Sleep(100);
            }
        }
    }
}
