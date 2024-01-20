using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Diagnostics;

namespace maplehook
{
    class Script
    {
        #region External Imports

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        #endregion

        #region Global variables
        public int CurrentLine = 0;
        string[] CurrentScript;
        public bool ScriptStoped = true;

        public string GetCurrentLineText
        {
            get
            {
                try
                {
                    return FilterString(CurrentLine);
                }
                catch (IndexOutOfRangeException) { return "gotostart"; }
                catch (Exception) { return "null"; }
            }

            set
            {
                CurrentScript[CurrentLine] = value;
            }
        }

        Random Random = new Random();
        public Thread Thread;

        KeyClass KeyClass = new KeyClass();
        KeysConverter KeyConverter = new KeysConverter();

        string[] LabelNames = new string[100];
        int[] LabelLine = new int[100];

        bool IsInLoop = false;
        int StartLoop = 0;
        int EndLoop = 0;
        int DoLoop = 0;

        int StartWhile = 0;
        int EndWhile = 0;
        bool IsInWhileLoop = false;

        string[] IntVariableName;
        int[] IntVariable;

        #endregion

        public void StopScript()
        {
            ScriptStoped = true;
            Thread.Abort();
        }

        public void StartScript(string[] Script)
        {
            LabelNames = new string[100];
            LabelLine = new int[100];

            IntVariable = new int[100];
            IntVariableName = new string[100];

            DoLoop = 0;
            EndLoop = 0;
            StartLoop = 0;
            IsInLoop = false;

            EndWhile = 0;
            StartWhile = 0;
            IsInWhileLoop = false;

            ScriptStoped = false;
            CurrentScript = Script;
            CurrentLine = 0;

            for (int i = 0; i < CurrentScript.Length; i++) //Checks every line for labels before it starts the script
            {
                if (GetCurrentLineText == "gotostart") //if it finds the end then it will stop checking
                {
                    i = 10000;
                }
                else if (GetCurrentLineText.EndsWith(":"))
                {
                    for (int i2 = 0; i2 < 100; i2++)
                    {
                        if (LabelNames[i] == null)
                        {
                            LabelNames[i] = GetCurrentLineText;
                            LabelLine[i] = CurrentLine;
                            i2 = 1000;
                        }
                    }
                }
                else if (GetCurrentLineText.StartsWith("int"))
                {
                    for (int i2 = 0; i2 < 100; i2++)
                    {
                        if (IntVariableName[i] == null)
                        {
                            try
                            {
                                IntVariableName[i] = GetCurrentLineText.Substring(3, GetCurrentLineText.IndexOf('=') - 3);
                                IntVariable[i] = Convert.ToInt32(GetFunction(GetCurrentLineText.Substring(GetCurrentLineText.IndexOf('=') + 1, GetCurrentLineText.IndexOf(';') - (GetCurrentLineText.IndexOf('=') + 1))));
                                i2 = 1000;
                            }
                            catch
                            {
                                IntVariableName[i] = GetCurrentLineText.Substring(3, GetCurrentLineText.IndexOf(';') - 3);
                                i2 = 1000;
                            }
                        }
                    }
                }
                CurrentLine++;
            }

            CurrentLine = 0;
            Thread = new Thread(HandleScript);
            Thread.Start();
        }

        void HandleScript()
        {
        Loop:
            try
            {
                string CurrentLineText = GetCurrentLineText;
                if (CurrentLineText != "" && CurrentLineText != "gotostart" && !CurrentLineText.EndsWith(":") && !CurrentLineText.Contains('{') && !CurrentLineText.Contains('}') && !CurrentLineText.StartsWith("int"))
                {
                    if (CurrentLineText.StartsWith("goto(")) //goto line or label
                    {
                        try
                        {
                            CurrentLine = Convert.ToInt32(GetFunction(CurrentLineText.Substring(5, CurrentLineText.LastIndexOf(");") - 5)));
                        }
                        catch (FormatException)
                        {
                            string GoTo = CurrentLineText.Substring(5, CurrentLineText.LastIndexOf(");") - 5) + ":";
                            for (int i = 0; i < 101; i++)
                            {
                                if (GoTo == LabelNames[i])
                                {
                                    CurrentLine = LabelLine[i] - 1;
                                    i = 1000;
                                }
                                if (i >= 100 && i != 1000)
                                {
                                    throw new Exception("I can not find out where \"" + GoTo + "\" is located.\nMake sure u have added \"" + GoTo + "\" somewhere in your script.");
                                }
                            }
                        }
                    }

                    else if (CurrentLineText.StartsWith("sleep(")) //Sleep function 
                    {
                        if (CurrentLineText.StartsWith("sleep(random(") && CurrentLineText.EndsWith("));"))
                        {
                            string Min = GetFunction(CurrentLineText.Substring(13, CurrentLineText.IndexOf(',') - 13));
                            string Max = GetFunction(CurrentLineText.Substring(CurrentLineText.IndexOf(',') + 1, CurrentLineText.LastIndexOf("));") - CurrentLineText.IndexOf(',') - 1));

                            Thread.Sleep(GetRandom(Convert.ToInt32(Min), Convert.ToInt32(Max)));
                        }
                        else if (CurrentLineText.StartsWith("sleep(rand(") && CurrentLineText.EndsWith("));"))
                        {
                            string Min = GetFunction(CurrentLineText.Substring(11, CurrentLineText.IndexOf(',') - 11));
                            string Max = GetFunction(CurrentLineText.Substring(CurrentLineText.IndexOf(',') + 1, CurrentLineText.LastIndexOf("));") - CurrentLineText.IndexOf(',') - 1));

                            Thread.Sleep(GetRandom(Convert.ToInt32(Min), Convert.ToInt32(Max)));
                        }
                        else
                        {
                            Thread.Sleep(Convert.ToInt32(GetFunction(CurrentLineText.Substring(6, CurrentLineText.LastIndexOf(");") - 6))));
                        }
                    }

                    #region Macro stuff
                    else if (CurrentLineText.StartsWith("mouse.x=")) //move mouse to X
                    {
                        Cursor.Position = new Point(Convert.ToInt32(GetFunction(CurrentLineText.Substring(8, CurrentLineText.LastIndexOf(";") - 8))), Cursor.Position.Y);
                    }
                    else if (CurrentLineText.StartsWith("mouse.y=")) //move mouse to Y
                    {
                        Cursor.Position = new Point(Cursor.Position.X, Convert.ToInt32(GetFunction(CurrentLineText.Substring(8, CurrentLineText.LastIndexOf(";") - 8))));
                    }
                    else if (CurrentLineText.StartsWith("mouse=point(")) //move mouse to point
                    {
                        int X = Convert.ToInt32(GetFunction(CurrentLineText.Substring(12, CurrentLineText.IndexOf(",") - 12)));
                        int Y = Convert.ToInt32(GetFunction(CurrentLineText.Substring(CurrentLineText.IndexOf(",") + 1, CurrentLineText.LastIndexOf(");") - (CurrentLineText.IndexOf(",") + 1))));
                        Cursor.Position = new Point(X, Y);
                    }

                    else if (CurrentLineText == "leftclick;") //left click
                    {
                        KeyClass.DoLeftClick();
                    }
                    else if (CurrentLineText == "doubleleftclick;") //double left click
                    {
                        KeyClass.DoLeftClick();
                        KeyClass.DoLeftClick();
                    }

                    else if (CurrentLineText == "rightclick;") //right click
                    {
                        KeyClass.DoRightClick();
                    }
                    else if (CurrentLineText == "doublerightclick;") //double right click
                    {
                        KeyClass.DoRightClick();
                        KeyClass.DoRightClick();
                    }

                    else if (CurrentLineText.StartsWith("sendkeys(\"")) //sends the provided string
                    {
                        KeyClass.SendKey(CurrentLineText.Substring(10, CurrentLineText.IndexOf("\");") - 10));
                    }
                    else if (CurrentLineText.StartsWith("sendkey(")) //sends a special key (ex: control or shift)
                    {
                        KeyClass.SendSpecialKey(CurrentLineText.Substring(8, CurrentLineText.IndexOf(");") - 8));
                    }

                    else if (CurrentLineText.StartsWith("holdkey("))
                    {
                        KeyClass.HoldKey(CurrentLineText.Substring(8, CurrentLineText.IndexOf(");") - 8));
                    }
                    else if (CurrentLineText.StartsWith("releasekey("))
                    {
                        KeyClass.ReleaseKey(CurrentLineText.Substring(11, CurrentLineText.IndexOf(");") - 11));
                    }

                    #endregion

                    #region Loop
                    else if (CurrentLineText.StartsWith("loop(")) //loop (amount of times here)
                    {
                        DoLoop = Convert.ToInt32(GetFunction(CurrentLineText.Substring(5, CurrentLineText.LastIndexOf(")") - 5)));

                        if (CurrentScript[CurrentLine + 1].Contains('{') == false)
                        {
                            throw new Exception("Could not find { for the loop.");
                        }

                        StartLoop = CurrentLine + 1;
                        for (int i = 0; i < CurrentScript.Length - CurrentLine; i++)
                        {
                            if (CurrentScript[CurrentLine + i].Contains('}'))
                            {
                                if (i == 1)
                                {
                                    EndLoop = CurrentLine + i + 1;
                                }
                                else
                                {
                                    EndLoop = CurrentLine + i;
                                }
                                i = 10000;
                            }
                        }

                        if (EndLoop > StartLoop)
                        {
                            IsInLoop = true;
                        }
                    }

                    #endregion

                    #region If...
                    else if (CurrentLineText.StartsWith("if("))
                    {
                        string string1 = "";
                        string string2 = "";
                        int CompareStyle = 0;

                        if (CurrentLineText.Contains("=="))
                        {
                            string1 = CurrentLineText.Substring(3, CurrentLineText.LastIndexOf("==") - 3);
                            string2 = CurrentLineText.Substring(CurrentLineText.LastIndexOf("==") + 2, CurrentLineText.Length - (CurrentLineText.LastIndexOf("==") + 3));
                            CompareStyle = 1;
                        }
                        else if (CurrentLineText.Contains("!="))
                        {
                            string1 = CurrentLineText.Substring(3, CurrentLineText.LastIndexOf("!=") - 3);
                            string2 = CurrentLineText.Substring(CurrentLineText.LastIndexOf("!=") + 2, CurrentLineText.Length - (CurrentLineText.LastIndexOf("!=") + 3));
                            CompareStyle = 2;
                        }
                        else if (CurrentLineText.Contains(">="))
                        {
                            string1 = CurrentLineText.Substring(3, CurrentLineText.LastIndexOf(">=") - 3);
                            string2 = CurrentLineText.Substring(CurrentLineText.LastIndexOf(">=") + 2, CurrentLineText.Length - (CurrentLineText.LastIndexOf(">=") + 3));
                            CompareStyle = 3;
                        }
                        else if (CurrentLineText.Contains("<="))
                        {
                            string1 = CurrentLineText.Substring(3, CurrentLineText.LastIndexOf("<=") - 3);
                            string2 = CurrentLineText.Substring(CurrentLineText.LastIndexOf("<=") + 2, CurrentLineText.Length - (CurrentLineText.LastIndexOf("<=") + 3));
                            CompareStyle = 4;
                        }
                        else if (CurrentLineText.Contains(">"))
                        {
                            string1 = CurrentLineText.Substring(3, CurrentLineText.LastIndexOf(">") - 3);
                            string2 = CurrentLineText.Substring(CurrentLineText.LastIndexOf(">") + 1, CurrentLineText.Length - (CurrentLineText.LastIndexOf(">") + 2));
                            CompareStyle = 5;
                        }
                        else if (CurrentLineText.Contains("<"))
                        {
                            string1 = CurrentLineText.Substring(3, CurrentLineText.LastIndexOf("<") - 3);
                            string2 = CurrentLineText.Substring(CurrentLineText.LastIndexOf("<") + 1, CurrentLineText.Length - (CurrentLineText.LastIndexOf("<") + 2));
                            CompareStyle = 6;
                        }

                        string1 = GetFunction(string1);
                        string2 = GetFunction(string2);

                        try
                        {
                            Convert.ToInt32(string1);
                        }
                        catch { throw new Exception("Error with \"" + string1 + "\""); }

                        try
                        {
                            Convert.ToInt32(string2);
                        }
                        catch { throw new Exception("Error with \"" + string2 + "\""); }

                        if (CurrentScript[CurrentLine + 1].Contains('{') == false)
                        {
                            throw new Exception("Could not find the \"{\"");
                        }

                        if (Compare(Convert.ToInt32(string1), Convert.ToInt32(string2), CompareStyle) == false)
                        {
                            int BodyCount = 0;
                            for (int i = 2; i < (CurrentScript.Length - CurrentLine); i++)
                            {
                                if (CurrentScript[CurrentLine + i].Contains('{'))
                                {
                                    BodyCount++;
                                }
                                if (CurrentScript[CurrentLine + i].Contains('}'))
                                {
                                    if (BodyCount <= 0)
                                    {
                                        CurrentLine = CurrentLine + i;
                                        try
                                        {
                                            if (FilterString(CurrentLine + 1) == "else")
                                            {
                                                CurrentLine += 2;
                                            }
                                        }
                                        catch { }
                                        i = 10000;
                                    }
                                    else
                                    {
                                        BodyCount--;
                                    }
                                }
                            }
                        }
                    }

                    #endregion

                    #region Else
                    else if (CurrentLineText == "else")
                    {
                        if (FilterString(CurrentLine + 1) != "{")
                        {
                            throw new Exception("Could not find the '{'");
                        }

                        int BodyCount = 0;
                        for (int i = 0; i < CurrentScript.Length; i++)
                        {
                            if (FilterString(CurrentLine + i) == "{")
                            {
                                BodyCount++;
                            }
                            else if (FilterString(CurrentLine + i) == "}")
                            {
                                if (BodyCount > 1)
                                {
                                    BodyCount--;
                                }
                                else
                                {
                                    CurrentLine = CurrentLine + i;
                                    i = 1000;
                                }
                            }
                        }
                    }

                    #endregion

                    #region while ...
                    else if (CurrentLineText.StartsWith("while("))
                    {
                        string string1 = "";
                        string string2 = "";
                        int CompareStyle = 0;

                        if (CurrentLineText.Contains("=="))
                        {
                            string1 = CurrentLineText.Substring(6, CurrentLineText.LastIndexOf("==") - 6);
                            string2 = CurrentLineText.Substring(CurrentLineText.LastIndexOf("==") + 2, CurrentLineText.Length - (CurrentLineText.LastIndexOf("==") + 3));
                            CompareStyle = 1;
                        }
                        else if (CurrentLineText.Contains("!="))
                        {
                            string1 = CurrentLineText.Substring(6, CurrentLineText.LastIndexOf("!=") - 6);
                            string2 = CurrentLineText.Substring(CurrentLineText.LastIndexOf("!=") + 2, CurrentLineText.Length - (CurrentLineText.LastIndexOf("!=") + 3));
                            CompareStyle = 2;
                        }
                        else if (CurrentLineText.Contains(">="))
                        {
                            string1 = CurrentLineText.Substring(6, CurrentLineText.LastIndexOf(">=") - 6);
                            string2 = CurrentLineText.Substring(CurrentLineText.LastIndexOf(">=") + 2, CurrentLineText.Length - (CurrentLineText.LastIndexOf(">=") + 3));
                            CompareStyle = 3;
                        }
                        else if (CurrentLineText.Contains("<="))
                        {
                            string1 = CurrentLineText.Substring(6, CurrentLineText.LastIndexOf("<=") - 6);
                            string2 = CurrentLineText.Substring(CurrentLineText.LastIndexOf("<=") + 2, CurrentLineText.Length - (CurrentLineText.LastIndexOf("<=") + 3));
                            CompareStyle = 4;
                        }
                        else if (CurrentLineText.Contains(">"))
                        {
                            string1 = CurrentLineText.Substring(6, CurrentLineText.LastIndexOf(">") - 6);
                            string2 = CurrentLineText.Substring(CurrentLineText.LastIndexOf(">") + 1, CurrentLineText.Length - (CurrentLineText.LastIndexOf(">") + 2));
                            CompareStyle = 5;
                        }
                        else if (CurrentLineText.Contains("<"))
                        {
                            string1 = CurrentLineText.Substring(6, CurrentLineText.LastIndexOf("<") - 6);
                            string2 = CurrentLineText.Substring(CurrentLineText.LastIndexOf("<") + 1, CurrentLineText.Length - (CurrentLineText.LastIndexOf("<") + 2));
                            CompareStyle = 6;
                        }

                        string1 = GetFunction(string1);
                        string2 = GetFunction(string2);

                        try
                        {
                            Convert.ToInt32(string1);
                        }
                        catch { throw new Exception("Error with \"" + string1 + "\""); }

                        try
                        {
                            Convert.ToInt32(string2);
                        }
                        catch { throw new Exception("Error with \"" + string2 + "\""); }

                        if (CurrentScript[CurrentLine + 1].Contains('{') == false)
                        {
                            throw new Exception("Could not find the \"{\"");
                        }

                        StartWhile = CurrentLine;
                        int BodyCount = 0;

                        for (int i = 2; i < (CurrentScript.Length - CurrentLine); i++)
                        {
                            if (CurrentScript[CurrentLine + i].Contains('{'))
                            {
                                BodyCount++;
                            }
                            if (CurrentScript[CurrentLine + i].Contains('}'))
                            {
                                if (BodyCount <= 0)
                                {
                                    EndWhile = CurrentLine + i;
                                    i = 10000;
                                }
                                else
                                {
                                    BodyCount--;
                                }
                            }
                        }

                        if (Compare(Convert.ToInt32(string1), Convert.ToInt32(string2), CompareStyle) == true)
                        {
                            CurrentLine++;
                            IsInWhileLoop = true;
                        }
                        else
                        {
                            CurrentLine = EndWhile;
                            IsInWhileLoop = false;
                        }
                    }

                    #endregion

                    else if (CurrentLineText == "leaveloop;")
                    {
                        IsInLoop = false;
                        StartLoop = 0;
                        EndLoop = 0;
                    }

                    else if (CurrentLineText == "leavewhileloop;")
                    {
                        IsInWhileLoop = false;
                        StartWhile = 0;
                        EndWhile = 0;
                    }

                    else if (CurrentLineText == "end;")
                    {
                        StopScript();
                    }

                    else if (CurrentLineText == "terminate;") //Terminate current process
                    {
                        Process.GetCurrentProcess().Kill();
                    }
                    else if (CurrentLineText.StartsWith("terminate(\"")) //Terminates all process with the provided name
                    {
                        Process[] Processes = Process.GetProcessesByName(CurrentLineText.Substring(11, CurrentLineText.IndexOf("\");") - 11));
                        foreach (Process pro in Processes)
                        {
                            pro.Kill();
                        }
                    }

                    else
                    {
                        #region Handle variables
                        bool IsVariable = false;
                        string VariableName = "";

                        if (CurrentLineText.Contains("+="))
                        {
                            VariableName = CurrentLineText.Substring(0, CurrentLineText.IndexOf("+="));
                        }
                        else if (CurrentLineText.Contains("-="))
                        {
                            VariableName = CurrentLineText.Substring(0, CurrentLineText.IndexOf("-="));
                        }
                        else if (CurrentLineText.Contains('+'))
                        {
                            VariableName = CurrentLineText.Substring(0, CurrentLineText.IndexOf('+'));
                        }
                        else if (CurrentLineText.Contains('-'))
                        {
                            VariableName = CurrentLineText.Substring(0, CurrentLineText.IndexOf('-'));
                        }
                        else if (CurrentLineText.Contains('='))
                        {
                            VariableName = CurrentLineText.Substring(0, CurrentLineText.IndexOf('='));
                        }

                        for (int i = 0; i < 100; i++)
                        {
                            if (IntVariableName[i] == VariableName)
                            {
                                IsVariable = true;
                                if (CurrentLineText.EndsWith("++;"))
                                {
                                    IntVariable[i]++;
                                }
                                else if (CurrentLineText.EndsWith("--;"))
                                {
                                    IntVariable[i]--;
                                }
                                else if (CurrentLineText.StartsWith(VariableName + "+="))
                                {
                                    IntVariable[i] += Convert.ToInt32(GetFunction(CurrentLineText.Substring(CurrentLineText.IndexOf('=') + 1, CurrentLineText.IndexOf(';') - (CurrentLineText.IndexOf('=') + 1))));
                                }
                                else if (CurrentLineText.StartsWith(VariableName + "-="))
                                {
                                    IntVariable[i] -= Convert.ToInt32(GetFunction(CurrentLineText.Substring(CurrentLineText.IndexOf('=') + 1, CurrentLineText.IndexOf(';') - (CurrentLineText.IndexOf('=') + 1))));
                                }
                                else if (CurrentLineText.StartsWith(VariableName + "="))
                                {
                                    IntVariable[i] = Convert.ToInt32(GetFunction(CurrentLineText.Substring(CurrentLineText.IndexOf('=') + 1, CurrentLineText.IndexOf(';') - (CurrentLineText.IndexOf('=') + 1))));
                                }
                            }
                        }

                        #endregion

                        if (IsVariable == false)
                        {
                            MessageBox.Show("I do not know what: " + '"' + CurrentLineText + '"' + " means\nat line: " + (CurrentLine + 1), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            StopScript();
                        }
                    }
                }

                if (IsInLoop == true)
                {
                    if (CurrentLine >= EndLoop - 1)
                    {
                        CurrentLine = StartLoop;
                        DoLoop--;
                        if (DoLoop <= 0)
                        {
                            IsInLoop = false;
                            CurrentLine = EndLoop;
                        }
                    }
                }
                if (IsInWhileLoop == true)
                {
                    if (CurrentLine >= EndWhile - 1)
                    {
                        CurrentLine = StartWhile;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ScriptStoped == false)
                {
                    MessageBox.Show("An exception was thrown on line: " + (CurrentLine + 1) + " " + '"' + GetCurrentLineText + '"' + "\nException message: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    StopScript();
                }
            }

            CurrentLine++;

        QuickLoop:
            if (GetCurrentLineText == "" || GetCurrentLineText.StartsWith("int"))
            {
                CurrentLine++;
                goto QuickLoop;
            }
            else if (GetCurrentLineText == "gotostart")
            {
                CurrentLine = 0;
                goto QuickLoop;
            }

            if (ScriptStoped == false)
            {
                goto Loop;
            }
        }

        int GetRandom(int min, int max)
        {
            return Random.Next(min, max);
        }

        string GetFunction(string text)
        {
            if (text.Contains('+') || text.Contains('-') || text.Contains('*') || text.Contains('/'))
            {
                text = DoCalculation(text);
            }

            if (text.StartsWith("0x")) //Converts hex to int
            {
                text = int.Parse(text.Substring(2, text.Length - 2), System.Globalization.NumberStyles.HexNumber).ToString();
            }

            else if (text.StartsWith("random("))
            {
                text = GetRandom(Convert.ToInt32(text.Substring(7, text.IndexOf(',') - 7)), Convert.ToInt32(text.Substring(text.IndexOf(',') + 1, text.IndexOf(")") - (text.IndexOf(',') + 1)))).ToString();
            }

            else if (text.StartsWith("pixelread(")) //Checks the pixel color at the selected posistion
            {
                text = GetPixel(Convert.ToInt32(text.Substring(10, text.IndexOf(',') - 10)), Convert.ToInt32(text.Substring(text.IndexOf(',') + 1, text.IndexOf(')') - (text.IndexOf(',') + 1)))).ToString();
            }

            else if (text == "null")
            {
                text = "0";
            }

            else
            {
                for (int i = 0; i < 100; i++)
                {
                    if (IntVariableName[i] == text)
                    {
                        text = IntVariable[i].ToString();
                    }
                }
            }
            return text;
        }

        string DoCalculation(string text)
        {
            int[] Numbers = new int[100];
            char[] CalculationStyle = new char[100];
            int i = 0;
            bool Loop = true;

            while (Loop == true)
            {
                int IndexOfAdd = text.IndexOf('+');
                int IndexOfSub = text.IndexOf('-');
                int IndexOfMul = text.IndexOf('*');
                int IndexOfDiv = text.IndexOf('/');

                if (IndexOfAdd == -1)
                    IndexOfAdd = 10000;
                if (IndexOfSub == -1)
                    IndexOfSub = 10000;
                if (IndexOfMul == -1)
                    IndexOfMul = 10000;
                if (IndexOfDiv == -1)
                    IndexOfDiv = 10000;

                if (IndexOfAdd < IndexOfSub && IndexOfAdd < IndexOfMul && IndexOfAdd < IndexOfDiv)
                {
                    //do +
                    Numbers[i] = Convert.ToInt32(GetFunction(text.Substring(0, IndexOfAdd)));
                    CalculationStyle[i] = '+';
                    text = text.Substring(IndexOfAdd + 1, text.Length - (IndexOfAdd + 1));
                    i++;
                }
                else if (IndexOfSub < IndexOfAdd && IndexOfSub < IndexOfMul && IndexOfSub < IndexOfDiv)
                {
                    //do -
                    Numbers[i] = Convert.ToInt32(GetFunction(text.Substring(0, IndexOfSub)));
                    CalculationStyle[i] = '-';
                    text = text.Substring(IndexOfSub + 1, text.Length - (IndexOfSub + 1));
                    i++;
                }
                else if (IndexOfMul < IndexOfAdd && IndexOfMul < IndexOfSub && IndexOfMul < IndexOfDiv)
                {
                    //do *
                    Numbers[i] = Convert.ToInt32(GetFunction(text.Substring(0, IndexOfMul)));
                    CalculationStyle[i] = '*';
                    text = text.Substring(IndexOfMul + 1, text.Length - (IndexOfMul + 1));
                    i++;
                }
                else if (IndexOfDiv < IndexOfAdd && IndexOfDiv < IndexOfSub && IndexOfDiv < IndexOfMul)
                {
                    //do /
                    Numbers[i] = Convert.ToInt32(GetFunction(text.Substring(0, IndexOfDiv)));
                    CalculationStyle[i] = '/';
                    text = text.Substring(IndexOfDiv + 1, text.Length - (IndexOfDiv + 1));
                    i++;
                }
                else
                {
                    if (text.Length < 1)
                    {
                        Loop = false;
                    }
                    else
                    {
                        Numbers[i] = Convert.ToInt32(GetFunction(text));
                        text = "";
                        i++;
                    }
                }
            }

            bool FirstValue = true;
            int CalculatedValue = 0;
            i = 0;

            foreach (char C in CalculationStyle)
            {
                if (FirstValue == true)
                {
                    if (C == '+')
                    {
                        CalculatedValue = Numbers[i] + Numbers[i + 1];
                    }
                    else if (C == '-')
                    {
                        CalculatedValue = Numbers[i] - Numbers[i + 1];
                    }
                    else if (C == '*')
                    {
                        CalculatedValue = Numbers[i] * Numbers[i + 1];
                    }
                    else if (C == '/')
                    {
                        CalculatedValue = Numbers[i] / Numbers[i + 1];
                    }
                    else if (C == '^')
                    {
                        CalculatedValue = Numbers[i] ^ Numbers[i + 1];
                    }
                    i++;
                    FirstValue = false;
                }
                else
                {
                    if (C == '+')
                    {
                        CalculatedValue = CalculatedValue + Numbers[i];
                    }
                    else if (C == '-')
                    {
                        CalculatedValue = CalculatedValue - Numbers[i];
                    }
                    else if (C == '*')
                    {
                        CalculatedValue = CalculatedValue * Numbers[i];
                    }
                    else if (C == '/')
                    {
                        CalculatedValue = CalculatedValue / Numbers[i];
                    }
                    else if (C == '^')
                    {
                        CalculatedValue = CalculatedValue ^ Numbers[i];
                    }
                }
                i++;
            }
            return CalculatedValue.ToString();
        }

        bool Compare(int Parameter1, int Parameter2, int CompareStyle)
        {
            if (CompareStyle != 0)
            {
                if (CompareStyle == 1)
                {
                    if (Parameter1 == Parameter2)
                    {
                        return true;
                    }
                    return false;
                }
                else if (CompareStyle == 2)
                {
                    if (Parameter1 != Parameter2)
                    {
                        return true;
                    }
                    return false;
                }
                else if (CompareStyle == 3)
                {
                    if (Parameter1 >= Parameter2)
                    {
                        return true;
                    }
                    return false;
                }
                else if (CompareStyle == 4)
                {
                    if (Parameter1 <= Parameter2)
                    {
                        return true;
                    }
                    return false;
                }
                else if (CompareStyle == 5)
                {
                    if (Parameter1 > Parameter2)
                    {
                        return true;
                    }
                    return false;
                }
                else if (CompareStyle == 6)
                {
                    if (Parameter1 < Parameter2)
                    {
                        return true;
                    }
                    return false;
                }
            }
            throw new Exception("An error occured in the compare functin");
        }

        public uint GetPixel(int X, int Y)
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            uint pixel = GetPixel(hdc, X, Y);
            ReleaseDC(IntPtr.Zero, hdc);
            return pixel;
        }

        string FilterString(int Line)
        {
            string TempString = CurrentScript[Line].ToLower().Replace("	", ""), NewString = "";
            bool Filter = true;

            if (TempString.Contains("//"))
            {
                TempString = TempString.Remove(TempString.IndexOf("//"));
            }

            foreach (char C in TempString)
            {
                if (C == '\"')
                {
                    if (Filter == false)
                    {
                        Filter = true;
                    }
                    else
                    {
                        Filter = false;
                    }
                }

                if (Filter == true)
                {
                    NewString += C.ToString().Replace(" ", "");
                }
                else
                {
                    NewString += C;
                }
            }
            return NewString;
        }
    }

    class KeyClass
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(long dwFlags, long dx, long dy, long cButtons, long dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        [DllImport("user32.dll")]
        static extern short VkKeyScan(char ch);

        [DllImport("User32.dll")]
        private static extern uint SendInput(uint numberOfInputs, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0, SizeConst = 1)] KEYBOARD_INPUT[] input, int structSize);

        [DllImport("user32.dll")]
        static extern int MapVirtualKey(int uCode, uint uMapType);

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBOARD_INPUT
        {
            public uint type;
            public ushort vk;
            public ushort scanCode;
            public uint flags;
            public uint time;
            public uint extrainfo;
            public uint padding1;
            public uint padding2;
        }
        const uint INPUT_KEYBOARD = 1;
        const int KEY_EXTENDED = 1;
        const uint KEY_SCANCODE = 4;
        const uint KEY_UP = 2;

        const int WM_KEYUP = 0x0101;
        const int WM_KEYDOWN = 0x100;
        const int WM_CHAR = 0x102;

        #region Sendinput things
        public void HoldKey(string text)
        {
            HoldKey(GetSendInputKey(text, false));
        }

        public void ReleaseKey(string text)
        {
            ReleaseKey(GetSendInputKey(text, false));
        }

        public void SendKey(string text)
        {
            text = text.Replace('_', ' ');
            foreach (char C in text)
            {
                Keys KeyToPress = (Keys)VkKeyScan(C);

                sendKey(KeyToPress, true);
                Thread.Sleep(30);
                sendKey(KeyToPress, false);
            }
        }

        public void SendSpecialKey(string text)
        {
            sendKey(GetSendInputKey(text, true), true);
            Thread.Sleep(50);
            sendKey(GetSendInputKey(text, true), false);
        }

        public Keys GetSendInputKey(string text, bool SpecialKey)
        {
            Keys KeyToPress = 0;
            if (text == "space")
            {
                KeyToPress = Keys.Space;
            }
            else if (text == "enter" || text == "Enter")
            {
                KeyToPress = Keys.Enter;
            }
            else if (text == "ctrl" || text == "control")
            {
                KeyToPress = Keys.ControlKey;
            }
            else if (text == "shift")
            {
                KeyToPress = Keys.ShiftKey;
            }
            else if (text == "alt")
            {
                KeyToPress = Keys.Alt;
            }
            else if (text == "return")
            {
                KeyToPress = Keys.Return;
            }
            else if (text == "pagedown")
            {
                KeyToPress = Keys.PageDown;
            }
            else if (text == "pageup")
            {
                KeyToPress = Keys.PageUp;
            }
            else if (text == "home")
            {
                KeyToPress = Keys.Home;
            }
            else if (text == "end")
            {
                KeyToPress = Keys.End;
            }
            else if (text == "insert")
            {
                KeyToPress = Keys.Insert;
            }
            else if (text == "delete")
            {
                KeyToPress = Keys.Delete;
            }
            else if (text == "left" || text == "leftkey")
            {
                KeyToPress = Keys.Left;
            }
            else if (text == "up" || text == "upkey")
            {
                KeyToPress = Keys.Up;
            }
            else if (text == "down" || text == "downkey")
            {
                KeyToPress = Keys.Down;
            }
            else if (text == "right" || text == "rightkey")
            {
                KeyToPress = Keys.Right;
            }
            else if (text == "escape")
            {
                KeyToPress = Keys.Escape;
            }
            else if (text == "backspace")
            {
                KeyToPress = Keys.Back;
            }
            else if (text == "0")
            {
                KeyToPress = Keys.D0;
            }
            else if (text == "1")
            {
                KeyToPress = Keys.D1;
            }
            else if (text == "2")
            {
                KeyToPress = Keys.D2;
            }
            else if (text == "3")
            {
                KeyToPress = Keys.D3;
            }
            else if (text == "4")
            {
                KeyToPress = Keys.D4;
            }
            else if (text == "5")
            {
                KeyToPress = Keys.D5;
            }
            else if (text == "6")
            {
                KeyToPress = Keys.D6;
            }
            else if (text == "7")
            {
                KeyToPress = Keys.D7;
            }
            else if (text == "8")
            {
                KeyToPress = Keys.D8;
            }
            else if (text == "9")
            {
                KeyToPress = Keys.D9;
            }
            else if (text == "a" || text == "A")
            {
                KeyToPress = Keys.A;
            }
            else if (text == "b" || text == "B")
            {
                KeyToPress = Keys.B;
            }
            else if (text == "c" || text == "C")
            {
                KeyToPress = Keys.C;
            }
            else if (text == "d" || text == "D")
            {
                KeyToPress = Keys.D;
            }
            else if (text == "e" || text == "E")
            {
                KeyToPress = Keys.E;
            }
            else if (text == "f" || text == "F")
            {
                KeyToPress = Keys.F;
            }
            else if (text == "g" || text == "G")
            {
                KeyToPress = Keys.G;
            }
            else if (text == "h" || text == "H")
            {
                KeyToPress = Keys.H;
            }
            else if (text == "i" || text == "I")
            {
                KeyToPress = Keys.I;
            }
            else if (text == "j" || text == "J")
            {
                KeyToPress = Keys.J;
            }
            else if (text == "k" || text == "K")
            {
                KeyToPress = Keys.K;
            }
            else if (text == "l" || text == "L")
            {
                KeyToPress = Keys.L;
            }
            else if (text == "m" || text == "M")
            {
                KeyToPress = Keys.M;
            }
            else if (text == "n" || text == "N")
            {
                KeyToPress = Keys.N;
            }
            else if (text == "o" || text == "O")
            {
                KeyToPress = Keys.O;
            }
            else if (text == "p" || text == "P")
            {
                KeyToPress = Keys.P;
            }
            else if (text == "q" || text == "Q")
            {
                KeyToPress = Keys.Q;
            }
            else if (text == "r" || text == "R")
            {
                KeyToPress = Keys.R;
            }
            else if (text == "s" || text == "S")
            {
                KeyToPress = Keys.S;
            }
            else if (text == "t" || text == "T")
            {
                KeyToPress = Keys.T;
            }
            else if (text == "u" || text == "U")
            {
                KeyToPress = Keys.U;
            }
            else if (text == "v" || text == "V")
            {
                KeyToPress = Keys.V;
            }
            else if (text == "w" || text == "W")
            {
                KeyToPress = Keys.W;
            }
            else if (text == "x" || text == "X")
            {
                KeyToPress = Keys.X;
            }
            else if (text == "y" || text == "Y")
            {
                KeyToPress = Keys.Y;
            }
            else if (text == "z" || text == "Z")
            {
                KeyToPress = Keys.Z;
            }
            else if (text == "f1")
            {
                KeyToPress = Keys.F1;
            }
            else if (text == "f2")
            {
                KeyToPress = Keys.F2;
            }
            else if (text == "f3")
            {
                KeyToPress = Keys.F3;
            }
            else if (text == "f4")
            {
                KeyToPress = Keys.F4;
            }
            else if (text == "f5")
            {
                KeyToPress = Keys.F5;
            }
            else if (text == "f6")
            {
                KeyToPress = Keys.F6;
            }
            else if (text == "f7")
            {
                KeyToPress = Keys.F7;
            }
            else if (text == "f8")
            {
                KeyToPress = Keys.F8;
            }
            else if (text == "f9")
            {
                KeyToPress = Keys.F9;
            }
            else if (text == "f10")
            {
                KeyToPress = Keys.F10;
            }
            else if (text == "f11")
            {
                KeyToPress = Keys.F11;
            }
            else if (text == "f12")
            {
                KeyToPress = Keys.F12;
            }
            else if (text == "tab")
            {
                KeyToPress = Keys.Tab;
            }
            else
            {
                if (SpecialKey == true)
                {
                    throw new Exception("Invalid input");
                }
                else
                {
                    KeyToPress = (Keys)VkKeyScan(text[0]);
                }
            }
            return KeyToPress;
        }

        private void HoldKey(Keys key)
        {
            sendKey(key, true);
        }

        private void ReleaseKey(Keys key)
        {
            sendKey(key, false);
        }

        private void sendKey(Keys key, bool press)
        {
            KEYBOARD_INPUT[] input = new KEYBOARD_INPUT[] { new KEYBOARD_INPUT() };
            input[0].type = 1;
            input[0].flags = 0;
            if (press)
            {
                input[0].vk = (ushort)(key & (Keys.OemClear | Keys.LButton));
            }
            else
            {
                input[0].vk = (ushort)key;
                input[0].flags |= 2;
            }
            SendInput(1, input, Marshal.SizeOf(input[0]));
        }

        #endregion

        public void DoLeftClick()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, Cursor.Position.X, Cursor.Position.Y, 0, 0);
        }

        public void DoRightClick()
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, Cursor.Position.X, Cursor.Position.Y, 0, 0);
        }
    }
}