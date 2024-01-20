using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Threading;

namespace maplehook
{
    class MapleConfig
    {
        #region External Imports

        [DllImport("psapi.dll")]
        static extern int EmptyWorkingSet(IntPtr hwProc);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern int SetWindowText(IntPtr hWnd, string text);

        #endregion

        #region Variables

        Process MapleStory;

        private Thread ReduceMemory;

        #endregion

        private string GetMaplePath()
        {
            RegistryKey MSSubKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wizet\\");
            RegistryKey MSLocation = MSSubKey.OpenSubKey("MapleStory");
            return MSLocation.GetValue("ExecPath").ToString();
        }

        public void Launch()
        {
            ProcessStartInfo maplestory = new ProcessStartInfo();
            maplestory.FileName = GetMaplePath() + @"\MapleStory.exe";
            maplestory.Arguments = "WebStart";
            MapleStory = Process.Start(maplestory);

            ReduceMemory = new Thread(new ThreadStart(this.ReduceMapleStoryMemory));
            ReduceMemory.IsBackground = true;
            ReduceMemory.Start();
        }

        public void Close()
        {
            ReduceMemory.Abort();
            try
            {
                MapleStory.Kill();
            }
            catch { }
        }

        private void ReduceMapleStoryMemory()
        {
            while (true)
            {
                try
                {
                    EmptyWorkingSet(Process.GetProcessById(MapleStory.Id).Handle);
                }
                catch (Exception)
                {
                    ReduceMemory.Abort();
                }
                finally { }
                Thread.Sleep(100);
            }
        }
    }
}
