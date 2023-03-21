using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using System.Text.RegularExpressions;

namespace VivadoErrorLogUtil
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        Win32.WindowFinder WindowFinder;
        String ProjectRootDir;
        String ProjectName;
        String CurrentLogFile;
        Dictionary<String, DateTime> LogFiles;
        public MainWindow()
        {
            InitializeComponent();
            LogI("START");
            LogI("(c)mmitti 2018-2023");
            LogI("vivado error log tool");
            LogI("///////////////////////");
            WindowFinder = new Win32.WindowFinder();
            WindowFinder.OnVivadoFound += OnVivadoFound;
            WindowFinder.RequestSearch();
            LogFiles = new Dictionary<string, DateTime>();
            Task.Run(() => {
                MainLoop();
            });
        }

        class FileOpenCommand : ICommand
        {
            [System.Runtime.InteropServices.DllImport("shell32.dll")]
            private static extern int FindExecutable(string lpFile, string lpDirectory, System.Text.StringBuilder lpResult);

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public void Execute(object parameter)
            {
                var proc = new System.Diagnostics.Process();
                var path_patch = Regex.Match(parameter as String, @"([a-z]\:(/.*)+.*)\:(\d+)");
                var file_name = path_patch.Groups[1].Value;
                var line_number = path_patch.Groups[3].Value;

                System.Text.StringBuilder exe_path = new System.Text.StringBuilder(255);
                if (FindExecutable(file_name, null, exe_path) > 32 && Regex.IsMatch(exe_path.ToString(), @".*Microsoft VS Code\\Code.exe"))
                {
                    proc.StartInfo.FileName = exe_path.ToString();
                    proc.StartInfo.Arguments = $"--goto {file_name}:{line_number}";
                }
                else
                {
                    proc.StartInfo.FileName = file_name;
                    proc.StartInfo.UseShellExecute = true;
                }
                proc.Start();

            }
        }

        private async void MainLoop()
        {
            while (true)
            {
                await Task.Run(() =>
                {
                    Thread.Sleep(1000);
                });
                await MainTask();
            }
        }

        private async Task MainTask()
        {
            if (ProjectRootDir == null || ProjectName == null) return;
            try
            {
                var sim_dir = Path.Combine(ProjectRootDir, ProjectName + ".sim");
                var dirs = Directory.GetDirectories(sim_dir);
                foreach (var dir in dirs)
                {
                    foreach (var log_name in new string[] { "elaborate", "xvlog" })
                    {
                        var file_path = Path.Combine(dir, "behav", "xsim", log_name + ".log");
                        if (File.Exists(file_path) && !LogFiles.ContainsKey(file_path))
                        {
                            LogFiles[file_path] = DateTime.MinValue;
                            LogI(file_path + " found");
                        }
                    }
                }

                string[] logs = null;


                foreach (var key in LogFiles.Keys.ToList())
                {
                    var ts = File.GetLastWriteTime(key);
                    if (LogFiles[key] < ts)
                    {
                        LogFiles[key] = ts;

                        if (CurrentLogFile == key)
                        {
                            await Dispatcher.BeginInvoke(new Action(() =>
                            {
                                MainLog.Inlines.Clear();
                                FileName.Inlines.Clear();
                                FileName.Inlines.Add("No Error Log");
                            }));
                            CurrentLogFile = null;
                            LogI("clear");
                        }

                        string[] tmp;
                        try
                        {
                            using (var f = File.OpenRead(key))
                            {
                                using (var s = new StreamReader(f))
                                {
                                    var lines = await s.ReadToEndAsync();
                                    tmp = lines.Split(new char[] { '\n', '\r' });
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            continue;
                        }

                        foreach (var l in tmp)
                        {
                            if (l.Contains("ERROR"))
                            {
                                CurrentLogFile = key;
                                logs = tmp;
                                LogI("load:" + key);
                                break;
                            }
                        }
                    }
                }
                if (logs != null)
                {
                    await Dispatcher.BeginInvoke(new Action(() =>
                    {
                        FileName.Inlines.Clear();

                        var m = Regex.Match(CurrentLogFile.Replace('\\', '/').ToString(), @"^(?<root>.*\.sim/)(?<sim_dir>[^/]*)(?<xsim>/behav/xsim/)(?<logname>[^\.]*).log$");
                        if (m.Success)
                        {
                            FileName.Inlines.Add(new Run(m.Groups["sim_dir"].Value) { Foreground = Brushes.Blue });
                            FileName.Inlines.Add("/");
                            FileName.Inlines.Add(new Run(m.Groups["logname"].Value) { Foreground = Brushes.Blue });
                            FileName.Inlines.Add(new Run("@" + LogFiles[CurrentLogFile].ToString()) { FontSize = 14 });
                        }

                        MainLog.Inlines.Clear();

                        void write_message(String text, Brush color)
                        {
                            if (color == Brushes.Black)
                            {
                                MainLog.Inlines.Add(new Run(text));
                            }
                            else
                            {

                                MainLog.Inlines.Add(new Run(text) { Foreground = color });
                            }
                        }

                        foreach (var l in logs)
                        {
                            var color = Brushes.Black;
                            if (l.Contains("ERROR"))
                            {
                                color = Brushes.Red;
                            }
                            else if (l.Contains("WARNING"))
                            {
                                color = Brushes.Orange;
                            }

                            var path_patch = Regex.Match(l, @"(.*)([a-z]\:(/.*)+.*\:\d+)(.*)");
                            if (path_patch.Success && path_patch.Groups.Count >= 3)
                            {
                                write_message(path_patch.Groups[1].Value, color);
                                MainLog.Inlines.Add(new Hyperlink(new Run(path_patch.Groups[2].Value))
                                {
                                    CommandParameter = path_patch.Groups[2].Value,
                                    Command = new FileOpenCommand()
                                });
                                write_message(path_patch.Groups[4].Value, color);
                            }
                            else
                            {
                                write_message(l, color);
                            }
                            MainLog.Inlines.Add(new LineBreak());
                        }
                        MainLogScroll.ScrollToEnd();
                        Activate();
                        Topmost = true;
                        Topmost = false;
                    }));
                }
            }catch(Exception ex) {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    SystemLog.Inlines.Add(new Run(ex.ToString()) { Foreground = Brushes.Blue });
                    SystemLog.Inlines.Add(new LineBreak());
                    SystemLogScroll.ScrollToEnd();
                }));
            }
        }

        private void LogI(string s)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SystemLog.Inlines.Add(new Run(s) { Foreground = Brushes.Blue });
                SystemLog.Inlines.Add(new LineBreak());
                SystemLogScroll.ScrollToEnd();
            }));
        }
        private void LogE(string s)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SystemLog.Inlines.Add(new Run(s) { Foreground = Brushes.Red });
                SystemLog.Inlines.Add(new LineBreak());
                SystemLogScroll.ScrollToEnd();
            }));
        }

        private void OnVivadoFound(string project_name, string project_path)
        {
            LogI("Vivado Found:" + project_name);
            ProjectPath.Content = project_path;
            ProjectRootDir = Path.GetDirectoryName(project_path);
            ProjectName = project_name;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            WindowFinder.RequestSearch();
        }
    }
}
