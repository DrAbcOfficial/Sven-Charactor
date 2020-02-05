using SharpCompress.Archives;
using SharpCompress.Readers;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace SvenCharactor
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        const double GRID_TOGGLE_TIME = 0.3;

        const double NOTIFY_HOLD_TIME = 1;
        const string NOTIFY_RUN = "开始解压文件到目录";
        const string NOTIFY_RUN_DONE = "成功解压{0}个文件";
        const string NOTIFY_RUN_FAIL = "文件列表为空";
        const string NOTIFY_DELETE = "选中项已删除";
        const string NOTIFY_DELETE_ERROR = "选中项为空";
        const string NOTIFY_PREVIEW = "成功打开文件预览";
        const string NOTIFY_RESET = "列表已清空";
        const string NOTIFY_SAVE = "配置已保存";
        const string NOTIFY_PAGE_ERROR = "页面切换出错";
        const string NOTIFY_OPEN_CANCEL = "设置目录已取消";
        const string NOTIFY_OPEN_DONE = "已设置程序目录";
        const string NOTIFY_INTIAL_DONE = "成功定位到Sven Co-op安装目录";
        const string NOTIFY_INTIAL_FAILED = "无法定位到Sven Co-op安装目录";
        const string NOTIFY_PLUGIN_DONE = "插件安装完毕";
        const string NOTIFY_PLUGIN_DEDEP = "插件解压完毕";
        const string NOTIFY_PLUGIN_RESET = "所有内容已清除";
        const string NOTIFY_PLUGIN_EMPTY = "必填项为空";
        const string NOTIFY_SPRAY_DONE = "喷漆制作成功";
        const string NOTIFY_SPRAY_DENY = "对文件的访问被拒绝，请手动删除";
        const string NOTIFY_SPRAY_FAIL = "所选文件不为图片";
        const string NOTIFY_BIND_DONE = "保存设置成功";
        const string NOTIFY_ERROR = "发生错误:{0}, 已保存错误报告";

        const uint NOTIFY_LEVEL_MESSAGE = 0;
        const uint NOTIFY_LEVEL_WARN = 1;
        const uint NOTIFY_LEVEL_ERROR = 2;
        const uint NOTIFY_LEVEL_DONE = 3;

        const string WINDOW_REPLACE = "已经存在文件，是否替换";

        const string PLUGIN_MODEL = "\"plugin\"\n{{\n\t\"name\" \"{0}\"\n\t\"script\" \"{1}\"\n{2}}}";

        const string ERROR_REPORT_NAME = "SvenCharactor.error.log";

        const string DIR_SELECT_TITLE = "请选择Sven Co-op路径";

        const string SVEN_REGEDIT_DIR = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 225840";



        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// 最小化窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MiniWindow_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized; //设置窗口最小化
        }
        /// <summary>
        /// 拖动标题栏
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Title_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        /// <summary>
        /// 设置切换动画
        /// </summary>
        /// <param name="strTarget">目标名称</param>
        private void ToggleGrid(Grid oGrid)
        {
            Grid gGrid = null;
            if (RunGrid.Visibility == Visibility.Visible)
                gGrid = RunGrid;
            else if (PluginGrid.Visibility == Visibility.Visible)
                gGrid = PluginGrid;
            else if (PrintGrid.Visibility == Visibility.Visible)
                gGrid = PrintGrid;
            else if (BindGrid.Visibility == Visibility.Visible)
                gGrid = BindGrid;
            else if (ConfigGrid.Visibility == Visibility.Visible)
                gGrid = ConfigGrid;

            if (gGrid == null)
            {
                Notify(NOTIFY_PAGE_ERROR, NOTIFY_LEVEL_ERROR);
                return;
            }

            gGrid.OpacityMask = this.Resources["ClosedBrush"] as System.Windows.Media.LinearGradientBrush;

            DoubleAnimation pAnima = new DoubleAnimation();
            pAnima.From = 1.0;
            pAnima.To = 0.0;
            pAnima.Duration = new Duration(TimeSpan.FromSeconds(GRID_TOGGLE_TIME));

            Storyboard pStory = new Storyboard();
            pStory.Children.Add(pAnima);
            Storyboard.SetTargetName(pAnima, gGrid.Name);
            Storyboard.SetTargetProperty(pAnima, new PropertyPath(OpacityProperty));
            pStory.Completed += delegate { gGrid.Visibility = Visibility.Collapsed; };
            pStory.Completed += delegate
            {
                gGrid.OpacityMask = this.Resources["OpenBrush"] as System.Windows.Media.LinearGradientBrush;

                pAnima = new DoubleAnimation();
                pAnima.From = 0.0;
                pAnima.To = 1.0;
                pAnima.Duration = new Duration(TimeSpan.FromSeconds(GRID_TOGGLE_TIME));

                pStory = new Storyboard();
                pStory.Children.Add(pAnima);
                Storyboard.SetTargetName(pAnima, oGrid.Name);
                Storyboard.SetTargetProperty(pAnima, new PropertyPath(OpacityProperty));
                pStory.Completed += delegate { oGrid.Visibility = Visibility.Visible; };

                pStory.Begin(oGrid);
            };
            pStory.Begin(gGrid);
        }

        /// <summary>
        /// 提示消息类
        /// </summary>
        private class CNotify
        {
            Grid NoticeGrid = new Grid();
            System.Windows.Shapes.Rectangle NoticeAngle = new System.Windows.Shapes.Rectangle();
            Label NoticeText = new Label();

            /// <summary>
            /// 构建函数
            /// </summary>
            /// <param name="_Mother">母Grid</param>
            /// <param name="_Str">发送的字符串</param>
            /// <param name="_Color">底板颜色</param>
            public CNotify(in Grid _Mother, in string _Str, in uint _Color)
            {
                NoticeGrid.HorizontalAlignment = HorizontalAlignment.Left;
                NoticeGrid.VerticalAlignment = VerticalAlignment.Top;
                NoticeGrid.Height = 31.2;
                NoticeGrid.Width = 242.4;
                NoticeGrid.Margin = new Thickness(198.4, 311.4, 0, 0);
                NoticeGrid.Name = "NoticeGrid";

                NoticeAngle.Stroke = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("Gray");
                NoticeAngle.HorizontalAlignment = HorizontalAlignment.Center;
                NoticeAngle.VerticalAlignment = VerticalAlignment.Center;
                NoticeAngle.Height = 31.2;
                NoticeAngle.Width = 242.4;
                NoticeAngle.RadiusX = NoticeAngle.RadiusY = 5;
                NoticeAngle.Name = "NoticeAngle";
                string color = Properties.Settings.Default.MessageColor;
                switch (_Color)
                {
                    case NOTIFY_LEVEL_MESSAGE: break;
                    case NOTIFY_LEVEL_WARN: color = Properties.Settings.Default.WarnColor; break;
                    case NOTIFY_LEVEL_ERROR: color = Properties.Settings.Default.ErrorColor; break;
                    case NOTIFY_LEVEL_DONE: color = Properties.Settings.Default.DoneColor; break;
                    default: break;
                }
                NoticeAngle.Fill = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(color);
                NoticeGrid.Children.Add(NoticeAngle);

                NoticeText.Content = _Str;
                NoticeText.HorizontalAlignment = HorizontalAlignment.Center;
                NoticeText.VerticalAlignment = VerticalAlignment.Center;
                NoticeText.Name = "NoticeText";
                NoticeGrid.Children.Add(NoticeText);

                if (NoticeText.Width >= NoticeGrid.Width)
                    NoticeGrid.Width = NoticeAngle.Width = NoticeText.Width + 20;
                if (NoticeText.Height >= NoticeGrid.Height)
                    NoticeGrid.Height = NoticeAngle.Height = NoticeText.Height + 20;

                _Mother.Children.Add(NoticeGrid);
            }

            /// <summary>
            /// 发送消息方法
            /// </summary>
            public void Send()
            {
                Storyboard pStory = new Storyboard();
                DoubleAnimation pAnima = new DoubleAnimation();
                pAnima.Duration = new Duration(TimeSpan.FromSeconds(NOTIFY_HOLD_TIME));
                pStory.Children.Add(pAnima);
                Storyboard.SetTarget(pAnima, NoticeGrid);
                Storyboard.SetTargetProperty(pAnima, new PropertyPath(OpacityProperty));
                pStory.Completed += delegate
                {
                    pAnima = new DoubleAnimation();
                    pAnima.From = 1.0;
                    pAnima.To = 0.0;
                    pAnima.Duration = new Duration(TimeSpan.FromSeconds(GRID_TOGGLE_TIME));


                    pStory.Children.Add(pAnima);
                    Storyboard.SetTarget(pAnima, NoticeGrid);
                    Storyboard.SetTargetProperty(pAnima, new PropertyPath(OpacityProperty));
                    pStory.Completed += delegate { NoticeGrid.Visibility = Visibility.Collapsed; };
                    pStory.Begin(NoticeGrid);
                };
                pStory.Begin(NoticeGrid);
            }
        }

        /// <summary>
        /// 错误类方法
        /// </summary>
        /// <param name="e"></param>
        public void Error(Exception e)
        {
            try
            {
                Task ts = new Task(delegate
                {
                    using (StreamWriter sw = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), ERROR_REPORT_NAME)))
                    {
                        sw.WriteLine("----------------------");
                        sw.WriteLine("[Date]" + DateTime.Now);
                        sw.WriteLine("[Message]" + e.Message);
                        sw.WriteLine("[StackTrace]" + e.StackTrace);
                        sw.WriteLine("[Source]" + e.Source);
                        sw.WriteLine("[InnerException]" + e.InnerException);
                    }
                });
                ts.Start();
                Dispatcher.BeginInvoke(
                    new Action(delegate
                     {
                         new CNotify(ClientPanel, string.Format(NOTIFY_ERROR, e.Message), NOTIFY_LEVEL_ERROR);
                     }));
            }
            catch (Exception) { }
        }

        public void Error(string str)
        {
            try
            {
                Task ts = new Task(delegate
                {
                    using (StreamWriter sw = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), ERROR_REPORT_NAME)))
                    {
                        sw.WriteLine("----------------------");
                        sw.WriteLine("[Date]" + DateTime.Now);
                        sw.WriteLine("[Message]" + str);
                    }

                });
                ts.Start();
                Dispatcher.BeginInvoke(
                    new Action(delegate
                    {
                        new CNotify(ClientPanel, string.Format(NOTIFY_ERROR, str), NOTIFY_LEVEL_ERROR);
                    }));
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 播放提示窗口
        /// </summary>
        /// <param name="strSz">提示消息</param>
        private void Notify(in string strSz, in uint iMessageLevel = 0)
        {
            new CNotify(ClientPanel, strSz, iMessageLevel).Send();
        }

        /// <summary>
        /// 切换到设置页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfigButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConfigGrid.Visibility == Visibility.Visible)
                return;
            ToggleGrid(ConfigGrid);
        }

        /// <summary>
        /// 切换到运行页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunGridButton_Click(object sender, RoutedEventArgs e)
        {
            if (RunGrid.Visibility == Visibility.Visible)
                return;
            ToggleGrid(RunGrid);
        }

        /// <summary>
        /// 切换到插件页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PluginGridButton_Click(object sender, RoutedEventArgs e)
        {
            if (PluginGrid.Visibility == Visibility.Visible)
                return;
            ToggleGrid(PluginGrid);
        }

        /// <summary>
        /// 切换到喷漆页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintGridButton_Click(object sender, RoutedEventArgs e)
        {
            if (PrintGrid.Visibility == Visibility.Visible)
                return;
            ToggleGrid(PrintGrid);
        }

        /// <summary>
        /// 切换到键位页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BindGridButton_Click(object sender, RoutedEventArgs e)
        {
            if (BindGrid.Visibility == Visibility.Visible)
                return;
            ToggleGrid(BindGrid);
        }

        private void TryDeCommpersion(in string pathStr, in string extenStr, in string dirStr)
        {
            try
            {
                using (Stream stream = File.OpenRead(pathStr))
                {
                    if (Path.GetExtension(pathStr) == ".7z")
                    {
                        var archive = ArchiveFactory.Open(stream);
                        foreach (var entry in archive.Entries)
                        {
                            if (!entry.IsDirectory)
                            {
                                if (Path.GetExtension(entry.Key) == extenStr)
                                {
                                    string exPath = Properties.Settings.Default.SvenDir +
                                        string.Format("\\svencoop{0}\\{1}\\{2}", (Properties.Settings.Default.IsSvencoop ? "" : "_addon"), dirStr, Path.GetFileNameWithoutExtension(entry.Key)) + "\\" + Path.GetFileName(entry.Key);

                                    if (!Directory.Exists(Path.GetDirectoryName(exPath)))
                                        Directory.CreateDirectory(Path.GetDirectoryName(exPath));

                                    using (StreamWriter sw = new StreamWriter(exPath))
                                    {
                                        entry.WriteTo(sw.BaseStream);
                                        sw.Write(sw.BaseStream);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        var reader = ReaderFactory.Open(stream);
                        while (reader.MoveToNextEntry())
                        {
                            if (!reader.Entry.IsDirectory)
                            {
                                string exPath = Properties.Settings.Default.SvenDir +
                                        string.Format("\\svencoop{0}\\{1}\\{2}", (Properties.Settings.Default.IsSvencoop ? "" : "_addon"), dirStr, Path.GetFileNameWithoutExtension(reader.Entry.Key)) + "\\" + Path.GetFileName(reader.Entry.Key);

                                if (!Directory.Exists(Path.GetDirectoryName(exPath)))
                                    Directory.CreateDirectory(Path.GetDirectoryName(exPath));

                                using (StreamWriter sw = new StreamWriter(exPath))
                                {
                                    reader.WriteEntryTo(sw.BaseStream);
                                    sw.Write(sw.BaseStream);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Error(ex);
            }
        }
        /// <summary>
        /// 解压按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.Items.IsEmpty)
            {
                Notify(NOTIFY_RUN_FAIL, NOTIFY_LEVEL_WARN);
                return;
            }
            Notify(NOTIFY_RUN);
            uint uiDoneCount = 0;
            foreach (string s in FileList.Items)
            {
                TryDeCommpersion(s, ".mdl", "models\\player\\");
                uiDoneCount++;
            }
            Notify(string.Format(NOTIFY_RUN_DONE, uiDoneCount), NOTIFY_LEVEL_DONE);
        }

        /// <summary>
        /// 预览按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedItem == null)
            {
                Notify(NOTIFY_DELETE_ERROR, NOTIFY_LEVEL_WARN);
                return;
            }
            System.Diagnostics.Process.Start(FileList.SelectedItem.ToString());
            Notify(NOTIFY_PREVIEW, NOTIFY_LEVEL_DONE);
        }

        /// <summary>
        /// 删除按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedItem == null)
            {
                Notify(NOTIFY_DELETE_ERROR, NOTIFY_LEVEL_WARN);
                return;
            }
            FileList.Items.Remove(FileList.SelectedItem);
            Notify(NOTIFY_DELETE);
        }

        /// <summary>
        /// 重置按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            FileList.Items.Clear();
            Notify(NOTIFY_RESET);
        }

        /// <summary>
        /// 保存按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.IsSvencoop = (bool)IsRootDir.IsChecked;
            Properties.Settings.Default.SvenDir = SvenDir.Text;
            Properties.Settings.Default.Save();
            Notify(NOTIFY_SAVE);
        }

        /// <summary>
        /// 打开文件按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Filter = "Sven Co-op (svencoop.exe)|svencoop.exe";
            dialog.Title = DIR_SELECT_TITLE;

            if (Directory.Exists(SvenDir.Text))
                dialog.InitialDirectory = SvenDir.Text;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

                Properties.Settings.Default.SvenDir = SvenDir.Text = Path.GetDirectoryName(dialog.FileName);
                Notify(NOTIFY_OPEN_DONE);
            }
            else
                Notify(NOTIFY_OPEN_CANCEL, NOTIFY_LEVEL_WARN);
        }

        /// <summary>
        /// 初次打开时从注册表寻找Sven Co-op路径
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Initialized(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Properties.Settings.Default.SvenDir))
            {
                try
                {
                    Microsoft.Win32.RegistryKey localKey = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry64);
                    localKey = localKey.OpenSubKey(SVEN_REGEDIT_DIR);
                    if (localKey == null)
                    {
                        localKey = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry32);
                        localKey = localKey.OpenSubKey(SVEN_REGEDIT_DIR);
                    }
                    Properties.Settings.Default.SvenDir = SvenDir.Text = localKey.GetValue("InstallLocation").ToString();
                    Notify(NOTIFY_INTIAL_DONE);
                }
                catch (Exception)
                {
                    Notify(NOTIFY_INTIAL_FAILED, NOTIFY_LEVEL_ERROR);
                }
            }
            else
                SvenDir.Text = Properties.Settings.Default.SvenDir;

            IsRootDir.IsChecked = Properties.Settings.Default.IsSvencoop;
            Properties.Settings.Default.LastLaunch = DateTime.Now;

            if (Properties.Settings.Default.BindedKeys == null)
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("Key", typeof(string));
                dt.Columns.Add("Val", typeof(string));

                Properties.Settings.Default.BindedKeys = dt;
            }
            BindDataGrid.ItemsSource = Properties.Settings.Default.BindedKeys.DefaultView;
            Properties.Settings.Default.Save();
        }
        /// <summary>
        /// 是不是压缩文件啊
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool IsCompressed(in string path)
        {
            return Path.GetExtension(path) == ".7z" || Path.GetExtension(path) == ".rar" || Path.GetExtension(path) == ".zip";
        }
        /// <summary>
        /// 文件拖拽操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DropArea_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                foreach (string s in (Array)e.Data.GetData(DataFormats.FileDrop))
                {
                    if (!IsCompressed(s))
                        continue;
                    if (!FileList.Items.Contains(s))
                        FileList.Items.Add(s);
                }
            }
        }
        /// <summary>
        /// Url
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink link = sender as Hyperlink;
            Process.Start(new ProcessStartInfo(link.NavigateUri.AbsoluteUri));
        }

        string[][] colorKVPairs =
             {
            new string [] { "#87CEFA","#4169E1"},
            new string [] { "Pink","#FF1493"},
            new string [] { "#FA8072", "Red"},
            new string [] { "#00FA9A", "Green"},
            new string [] {"#EEE8AA", "#FFD700"},
            new string [] {"NavajoWhite", "#FF8C00"},
            new string [] { "#FFD4D4D4","Gray"}
        };
        uint colorindex = 0;
        /// <summary>
        /// 彩蛋
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Color_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

            Properties.Settings.Default.LightColor = colorKVPairs[colorindex][0];
            Properties.Settings.Default.DeepColor = colorKVPairs[colorindex][1];
            if (colorindex == colorKVPairs.Length - 1)
                colorindex = 0;
            else
                colorindex++;
        }

        bool isNight = false;
        private void Night_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!isNight)
                Properties.Settings.Default.NoneColor = "#FF484848";
            else
                Properties.Settings.Default.NoneColor = "White";
            isNight = !isNight;
        }
        /// <summary>
        /// 清空
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PluginResetButton_Click(object sender, RoutedEventArgs e)
        {
            PluginNameText.Text = string.Empty;
            PluginLocText.Text = string.Empty;
            PluginComText.Text = string.Empty;

            PluginMetaText.Text = string.Empty;

            Notify(NOTIFY_PLUGIN_RESET);
        }
        /// <summary>
        /// 修改default_plugin.txt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PluginRunButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(PluginLocText.Text) || string.IsNullOrEmpty(PluginNameText.Text))
            {
                Notify(NOTIFY_PLUGIN_EMPTY, NOTIFY_LEVEL_ERROR);
                return;
            }
            string svenDir = Properties.Settings.Default.SvenDir + "/svencoop/default_plugins.txt";
            string dirStr = "\t" + PluginMetaText.Text.Replace("\n", "\n\t");
            Task ts = new Task(
                delegate
                {
                    string tempStr;
                    using (StreamReader sr = new StreamReader(svenDir))
                    {
                        tempStr = sr.ReadToEnd();
                    }
                    tempStr = tempStr.Substring(0, tempStr.LastIndexOf("}")) + dirStr + "\n}";
                    using (StreamWriter sw = new StreamWriter(svenDir))
                    {
                        sw.Write(tempStr);
                    }
                });
            ts.Start();
            ts.Wait();
            Notify(svenDir, NOTIFY_LEVEL_DONE);
            Notify(NOTIFY_PLUGIN_DONE, NOTIFY_LEVEL_DONE);
        }

        /// <summary>
        /// 修改Meta窗口
        /// </summary>
        private void getPluginMeta()
        {
            PluginMetaText.Text = string.Format(PLUGIN_MODEL, PluginNameText.Text, PluginLocText.Text,
                string.IsNullOrEmpty(PluginComText.Text) ? string.Empty : string.Format("\t\"concommandns\" \"{0}\"\n", PluginComText.Text));
        }

        /// <summary>
        /// 一堆文本框更改
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PluginNameText_TextChanged(object sender, TextChangedEventArgs e)
        {
            getPluginMeta();
        }

        private void PluginLocText_TextChanged(object sender, TextChangedEventArgs e)
        {
            getPluginMeta();
        }

        private void PluginComText_TextChanged(object sender, TextChangedEventArgs e)
        {
            getPluginMeta();
        }

        private void PluginMetaText_TextChanged(object sender, TextChangedEventArgs e)
        {
            string[] tempStr = PluginMetaText.Text.Replace("{", string.Empty).Replace("}", string.Empty).Replace("\"", string.Empty).Replace("\t", string.Empty).Replace("plugin", string.Empty).Split('\n');
            foreach (string s in tempStr)
            {
                if (s.StartsWith("name"))
                    PluginNameText.Text = s.Replace("name ", string.Empty);
                else if (s.StartsWith("script"))
                    PluginLocText.Text = s.Replace("script ", string.Empty);
                else if (s.StartsWith("concommandns"))
                    PluginComText.Text = s.Replace("concommandns ", string.Empty);
            }
        }

        private string[] getPluginLoc()
        {
            string[] tempStr = PluginLocText.Text.Split(PluginLocText.Text.Contains("\\") ? '\\' : '/');
            return tempStr;
        }
        /// <summary>
        /// 替换第一个字符
        /// </summary>
        /// <param name="strTemp">远字符串</param>
        /// <param name="beReplace">被替换字符串</param>
        /// <param name="doReplace">替换字符串</param>
        /// <returns></returns>
        private string Replace(in string strTemp, in string beReplace, in string doReplace)
        {
            System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(beReplace);
            if (reg.IsMatch(strTemp))
                return reg.Replace(strTemp, doReplace, 1);
            return strTemp;
        }
        /// <summary>
        /// 插件文件拖拽操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PluginArea_Drop(object sender, DragEventArgs e)
        {
            if (string.IsNullOrEmpty(PluginLocText.Text) || string.IsNullOrEmpty(PluginNameText.Text))
            {
                Notify(NOTIFY_PLUGIN_EMPTY, NOTIFY_LEVEL_ERROR);
                return;
            }

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string str = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
                if (!IsCompressed(str))
                    return;

                string tempDir = string.Empty;
                string[] tempAry = getPluginLoc();
                string realDir = string.Empty;

                for (uint i = 0; i < tempAry.Length - 1; i++)
                {
                    realDir += tempAry[i];
                }

                //寻找文件目录
                try
                {
                    Task ts = new Task(
                        delegate
                        {
                            using (Stream stream = File.OpenRead(str))
                            {
                                if (Path.GetExtension(str) == ".7z")
                                {
                                    var archive = ArchiveFactory.Open(stream);
                                    foreach (var entry in archive.Entries)
                                    {
                                        if (!entry.IsDirectory)
                                        {
                                            if (Path.GetExtension(entry.Key) == ".as")
                                            {
                                                if (Path.GetFileNameWithoutExtension(entry.Key) == tempAry[tempAry.Length - 1])
                                                {
                                                    tempDir = Path.GetDirectoryName(entry.Key);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    var reader = ReaderFactory.Open(stream);
                                    while (reader.MoveToNextEntry())
                                    {
                                        if (!reader.Entry.IsDirectory)
                                        {
                                            if (Path.GetExtension(reader.Entry.Key) == ".as")
                                            {
                                                if (Path.GetFileNameWithoutExtension(reader.Entry.Key) == tempAry[tempAry.Length - 1])
                                                {
                                                    tempDir = Path.GetDirectoryName(reader.Entry.Key);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        );
                    ts.Start();
                    ts.Wait();

                    ts = new Task(
                        delegate
                        {
                            using (Stream stream = File.OpenRead(str))
                            {
                                if (Path.GetExtension(str) == ".7z")
                                {
                                    var archive = ArchiveFactory.Open(stream);
                                    foreach (var entry in archive.Entries)
                                    {
                                        if (!entry.IsDirectory)
                                        {
                                            if (Path.GetDirectoryName(entry.Key).Contains(tempDir))
                                            {
                                                string exPath = Properties.Settings.Default.SvenDir + string.Format("\\svencoop{0}\\{1}\\{2}",
                                                    (Properties.Settings.Default.IsSvencoop ? "" : "_addon"),
                                                    "scripts\\plugins\\" + realDir, Replace(entry.Key, tempDir, string.Empty));
                                                if (!Directory.Exists(Path.GetDirectoryName(exPath)))
                                                    Directory.CreateDirectory(Path.GetDirectoryName(exPath));

                                                using (StreamWriter sw = new StreamWriter(exPath))
                                                {
                                                    entry.WriteTo(sw.BaseStream);
                                                    sw.Write(sw.BaseStream);
                                                }
                                            }
                                        }

                                    }
                                }
                                else
                                {
                                    var reader = ReaderFactory.Open(stream);
                                    while (reader.MoveToNextEntry())
                                    {
                                        if (!reader.Entry.IsDirectory)
                                        {
                                            if (Path.GetDirectoryName(reader.Entry.Key).Contains(tempDir))
                                            {
                                                string exPath = Properties.Settings.Default.SvenDir + string.Format("\\svencoop{0}\\{1}\\{2}",
                                                    (Properties.Settings.Default.IsSvencoop ? "" : "_addon"),
                                                    "scripts\\plugins\\" + realDir, Replace(reader.Entry.Key, tempDir, string.Empty));
                                                if (!Directory.Exists(Path.GetDirectoryName(exPath)))
                                                    Directory.CreateDirectory(Path.GetDirectoryName(exPath));

                                                using (StreamWriter sw = new StreamWriter(exPath))
                                                {
                                                    reader.WriteEntryTo(sw.BaseStream);
                                                    sw.Write(sw.BaseStream);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        );
                    ts.Start();
                    ts.Wait();
                    Process.Start(Properties.Settings.Default.SvenDir + string.Format("\\svencoop{0}\\{1}\\",
                                                    (Properties.Settings.Default.IsSvencoop ? "" : "_addon"),
                                                    "scripts\\plugins\\" + realDir));
                    Notify(NOTIFY_PLUGIN_DEDEP, NOTIFY_LEVEL_DONE);

                }
                catch (Exception ex)
                {
                    Error(ex);
                }
            }
        }
        bool IsImage(in string sz)
        {
            return Path.GetExtension(sz) == ".jpg" || Path.GetExtension(sz) == ".bmp" || Path.GetExtension(sz) == ".png" || Path.GetExtension(sz) == ".tiff";
        }
        /// <summary>
        /// 图片处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageArea_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string str = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
                    if (!IsImage(str))
                    {
                        Notify(NOTIFY_SPRAY_FAIL, NOTIFY_LEVEL_ERROR);
                        return;
                    }
                    string outputPath = Properties.Settings.Default.SvenDir + string.Format("\\svencoop{0}\\", (Properties.Settings.Default.IsSvencoop ? "" : "_addon"));
                    outputPath = Path.Combine(outputPath, "tempdecal.wad");
                    if (File.Exists(outputPath))
                    {
                        if (CheckWindow.Show(WINDOW_REPLACE) != CheckWindowReturn.RETURN_OK)
                            return;

                        if (File.GetAttributes(outputPath).ToString().IndexOf("ReadOnly") != -1)
                        {
                            Notify(NOTIFY_SPRAY_DENY, NOTIFY_LEVEL_ERROR);
                            return;
                        }
                    }

                    Task ts = new Task(delegate { HLTools.WAD3Loader.CreateWad(outputPath, new string[] { str }, new string[] { "{logo" }); });
                    ts.Start();
                    ts.Wait();

                    if (IsPrintReadOnly.IsChecked == true)
                        File.SetAttributes(outputPath, FileAttributes.ReadOnly);
                    Notify(NOTIFY_SPRAY_DONE, NOTIFY_LEVEL_DONE);
                }
            }
            catch (Exception ex)
            {
                Error(ex);
            }
        }

        private void SaveBindButton_Click(object sender, RoutedEventArgs e)
        {
            string outputPath = Properties.Settings.Default.SvenDir + string.Format("\\svencoop{0}\\", (Properties.Settings.Default.IsSvencoop ? "" : "_addon"));
            outputPath = Path.Combine(outputPath, "bindedkey.cfg");
            if(!File.Exists(outputPath))
            {
                string autoExec = Properties.Settings.Default.SvenDir + string.Format("\\svencoop{0}\\", (Properties.Settings.Default.IsSvencoop ? "" : "_addon"));
                autoExec = Path.Combine(autoExec, "autoexec.cfg");
                using (StreamWriter sw = new StreamWriter(autoExec, true))
                {
                    sw.WriteLine("exec bindedkey.cfg");
                }
            }
            string tempStr = string.Empty;
            foreach (DataRow row in Properties.Settings.Default.BindedKeys.Rows)
            {
                if(!string.IsNullOrEmpty(row["Key"].ToString()) && !string.IsNullOrEmpty(row["Val"].ToString()))
                    tempStr += string.Format("bind {0} {1}\n", row["Key"], row["Val"]);
            }
            using (StreamWriter sw = new StreamWriter(outputPath))
            {
                sw.Write(tempStr);
            }
            Properties.Settings.Default.Save();
            Notify(NOTIFY_BIND_DONE,NOTIFY_LEVEL_DONE);
        }

        private void BindAddButton_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(BindKey.Text) || string.IsNullOrEmpty(BindVal.Text))
            {
                Notify(NOTIFY_PLUGIN_EMPTY, NOTIFY_LEVEL_ERROR);
                return;
            }
            DataRow row = Properties.Settings.Default.BindedKeys.NewRow();
            row["Key"] = BindKey.Text;
            string Val = string.Empty;
            switch(BindKind.SelectedIndex)
            {
                case 0: Val += "\"say " + BindVal.Text + "\"";break;
                case 1:Val = BindVal.Text;break;
                default: Val = BindVal.Text; break;
            }
            row["Val"] = Val;
            Properties.Settings.Default.BindedKeys.Rows.Add(row);
        }
    }
}