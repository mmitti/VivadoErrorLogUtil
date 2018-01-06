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
            LogI("(c)mmitti 2018");
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
            var sim_dir = Path.Combine(ProjectRootDir, ProjectName + ".sim");
            var dirs = Directory.GetDirectories(sim_dir);
            foreach(var dir in dirs)
            {
                foreach(var log_name in new string[]{"elaborate", "xvlog" })
                {
                    var file_path = Path.Combine(dir, "behav", "xsim", log_name + ".log");
                    if (File.Exists(file_path) && !LogFiles.ContainsKey(file_path)){
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
                    }catch(Exception e)
                    {
                        continue;
                    }
                       
                    foreach(var l in tmp)
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
                        FileName.Inlines.Add(new Run("@" + LogFiles[CurrentLogFile].ToString()) { FontSize=14});
                    }

                    MainLog.Inlines.Clear();
                    foreach(var l in logs)
                    {
                        if (l.Contains("ERROR"))
                        {
                            MainLog.Inlines.Add(new Run(l) { Foreground = Brushes.Red });
                        }else if (l.Contains("WARNING"))
                        {
                            MainLog.Inlines.Add(new Run(l) { Foreground = Brushes.Orange });
                        }
                        else
                        {
                            MainLog.Inlines.Add(new Run(l));
                        }
                        MainLog.Inlines.Add(new LineBreak());
                    }
                    MainLogScroll.ScrollToEnd();
                    Activate();
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
