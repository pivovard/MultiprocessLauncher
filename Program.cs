using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WindowsInput;
using WindowsInput.Native;

namespace WinFormsApp1
{
    internal static class Program
    {
        private const int SW_SHOWMAXIMIZED = 3;

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOZORDER = 0x0004;

        static void Main(string[] args)
        {
            //-p "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe" -a "--new-window www.google.com" -screen 1

            if (args.Length > 0)
            {
                if (args[0] == "-f" || args[0] == "--file")
                {
                    ReadFile(args[1]);
                }
                else if (args[0] == "-p" || args[0] == "--proc")
                {
                    ParseLine(args);
                }
                else
                {
                    Help();
                }
            }
            else
            {
                Console.WriteLine("Invalid number of the arguments!\n");
                Help();
            }
        }

        private static void ReadFile(string path)
        {
            try
            {
                IEnumerable<string> file = File.ReadLines(path);
                foreach (var line in file)
                {
                    var args = SplitArguments(line);

                    if (args.Length > 0)
                    {
                        ParseLine(args);
                    }
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"Unable to read the config file {path}!");
            }
        }

        static string[] SplitArguments(string commandLine)
        {
            char[] parmChars = commandLine.ToCharArray();
            bool inQuote = false;
            for (int index = 0; index < parmChars.Length; index++)
            {
                if (parmChars[index] == '"')
                    inQuote = !inQuote;
                if (!inQuote && parmChars[index] == ' ')
                    parmChars[index] = '\n';
            }
            return (new string(parmChars)).Split('\n');
        }

        private static void ParseLine(string[] command)  
        {
            string proc = command[1];
            string args = "";
            uint screen = 0;

            uint left = 0;
            uint up = 0;
            uint right = 0;
            uint down = 0;

            for(int i = 2; i < command.Count(); i++)
            {
                if(command[i] == "-a" || command[i] == "--args")
                {
                    args = command[i+1];
                }
                if (command[i] == "-s" || command[i] == "--screen")
                {
                    screen = uint.Parse(command[i + 1]);
                }

                if (command[i] == "-left")
                {
                    left = uint.Parse(command[i + 1]);
                }
                if (command[i] == "-up")
                {
                    up = uint.Parse(command[i + 1]);
                }
                if (command[i] == "-right")
                {
                    right = uint.Parse(command[i + 1]);
                }
                if (command[i] == "-down")
                {
                    down = uint.Parse(command[i + 1]);
                }
            }

            try
            {
                var process = Process.Start(proc, args);
                Thread.Sleep(2000);

                if (process.MainWindowHandle == IntPtr.Zero)
                {
                    //get process name from path
                    string pname = proc.Split(new char[] { '/', '\\' }).Last().Split('.').First();
                    process = Process.GetProcessesByName(pname).First(p => p.MainWindowHandle != IntPtr.Zero);
                }

                SetWindowPosition(process, screen);
                MoveWindow(left, up, right, down);
            }
            catch (Win32Exception)
            {
                Console.WriteLine($"File not found at {proc}!");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine($"Getting window handler of process failed!");
            }
        }

        private static void SetWindowPosition(Process process, uint screen)
        {
            process.WaitForInputIdle(5000);
            if (System.Windows.Forms.Screen.AllScreens.Length > screen)
            {
                SetWindowPos(process.MainWindowHandle,
                    IntPtr.Zero,
                    Screen.AllScreens[screen].WorkingArea.Left,
                    Screen.AllScreens[screen].WorkingArea.Top,
                    0, 0, SWP_NOSIZE | SWP_NOZORDER);
            }

            ShowWindow(process.MainWindowHandle, SW_SHOWMAXIMIZED);
        }

        private static void MoveWindow(uint left = 0, uint up = 0, uint right = 0, uint down = 0)
        {
            InputSimulator sim = new InputSimulator();

            for (int i = 0; i < left; i++)
            {
                sim.Keyboard.ModifiedKeyStroke(new List<VirtualKeyCode> { VirtualKeyCode.LWIN, VirtualKeyCode.LSHIFT }, VirtualKeyCode.LEFT);
            }
            for (int i = 0; i < up; i++)
            {
                sim.Keyboard.ModifiedKeyStroke(new List<VirtualKeyCode> { VirtualKeyCode.LWIN, VirtualKeyCode.LSHIFT }, VirtualKeyCode.UP);
            }
            for (int i = 0; i < right; i++)
            {
                sim.Keyboard.ModifiedKeyStroke(new List<VirtualKeyCode> { VirtualKeyCode.LWIN, VirtualKeyCode.LSHIFT }, VirtualKeyCode.RIGHT);
            }
            for (int i = 0; i < down; i++)
            {
                sim.Keyboard.ModifiedKeyStroke(new List<VirtualKeyCode> { VirtualKeyCode.LWIN, VirtualKeyCode.LSHIFT }, VirtualKeyCode.DOWN);
            }

            //Maximize it
            sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LWIN, VirtualKeyCode.UP);
        }

        private static List<IntPtr> GetHandlers(string name)
        {
            List<Process> existingProcesses = Process.GetProcessesByName(name).ToList();
            List<IntPtr> handlers = new List<IntPtr> { };
            foreach (var existingProcess in existingProcesses)
            {
                if (!existingProcess.HasExited) handlers.Add(existingProcess.MainWindowHandle);
            }
            return handlers;
        }

        private static void Help()
        {
            Console.WriteLine("Process command: -p <path> -a <args> -left <int> -up <int> -right <int> -down <int> (-p is mandatory).\n");
            Console.WriteLine("\t\t-f --file <path> \t Path to the file with a list of processes to run.");
            Console.WriteLine("\t\t-p --proc <process> \t Process to run.");
            Console.WriteLine("\t\t-a --args <arguments> \t Run process with arguments.");
            Console.WriteLine("\t\t-s --screen <screen> \t Move process to screen <screen>");
            Console.WriteLine();
            Console.WriteLine("\t\t-left  <int> \t\t Move left  by <int>.");
            Console.WriteLine("\t\t-up    <int> \t\t Move up    by <int>.");
            Console.WriteLine("\t\t-right <int> \t\t Move right by <int>.");
            Console.WriteLine("\t\t-down  <int> \t\t Move down  by <int>.");
        }

    }
}