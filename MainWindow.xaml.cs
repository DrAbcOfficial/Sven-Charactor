using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;


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
        const string NOTIFY_ERROR = "发生错误:{0}, 已保存错误报告";
        const uint NOTIFY_LEVEL_MESSAGE = 0;
        const uint NOTIFY_LEVEL_WARN = 1;
        const uint NOTIFY_LEVEL_ERROR = 2;
        const uint NOTIFY_LEVEL_DONE = 3;

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
            else if (ConfigGrid.Visibility == Visibility.Visible)
                gGrid = ConfigGrid;

            if (gGrid == null)
            {
                Notify(NOTIFY_PAGE_ERROR, NOTIFY_LEVEL_ERROR);
                return;
            }

            gGrid.OpacityMask = this.Resources["ClosedBrush"] as LinearGradientBrush;

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
                gGrid.OpacityMask = this.Resources["OpenBrush"] as LinearGradientBrush;

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

                NoticeAngle.Stroke = (Brush)new BrushConverter().ConvertFromString("Gray");
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
                NoticeAngle.Fill = (Brush)new BrushConverter().ConvertFromString(color);
                NoticeGrid.Children.Add(NoticeAngle);

                NoticeText.Content = _Str;
                NoticeText.HorizontalAlignment = HorizontalAlignment.Center;
                NoticeText.VerticalAlignment = VerticalAlignment.Center;
                NoticeText.Name = "NoticeText";
                NoticeGrid.Children.Add(NoticeText);

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
            Task ts = new Task(delegate
            {
                StreamWriter sw = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), ERROR_REPORT_NAME));
                sw.WriteLine("----------------------");
                sw.WriteLine("[Date]" + DateTime.Now);
                sw.WriteLine("[Message]" + e.Message);
                sw.WriteLine("[StackTrace]" + e.StackTrace);
                sw.WriteLine("[Source]" + e.Source);
                sw.WriteLine("[InnerException]" + e.InnerException);
            });
            ts.Wait();
            new CNotify(ClientPanel, string.Format(NOTIFY_ERROR, e.Message), NOTIFY_LEVEL_ERROR);
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
        /// 解压按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            try
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
                    using (Stream stream = File.OpenRead(s))
                    {
                        if (Path.GetExtension(s) == ".7z")
                        {
                            var archive = ArchiveFactory.Open(stream);
                            foreach (var entry in archive.Entries)
                            {
                                if (!entry.IsDirectory)
                                {
                                    if (Path.GetExtension(entry.Key) == ".mdl")
                                    {
                                        entry.WriteToDirectory(Properties.Settings.Default.SvenDir +
                                            string.Format("\\svencoop{0}\\models\\player\\{1}\\", (Properties.Settings.Default.IsSvencoop ? "" : "_addon"), Path.GetFileNameWithoutExtension(entry.Key)),
                                            new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                                        uiDoneCount++;
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
                                    if (Path.GetExtension(reader.Entry.Key) == ".mdl")
                                    {
                                        reader.WriteEntryToDirectory(Properties.Settings.Default.SvenDir +
                                            string.Format("\\svencoop{0}\\models\\player\\{1}\\{1}.mdl", (Properties.Settings.Default.IsSvencoop ? "" : "_addon"), Path.GetFileNameWithoutExtension(reader.Entry.Key)),
                                            new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                                        uiDoneCount++;
                                    }
                                }
                            }
                        }
                    }
                }
                Notify(string.Format(NOTIFY_RUN_DONE, uiDoneCount), NOTIFY_LEVEL_DONE);
            }
            catch (Exception ex)
            {
                Error(ex);
            }
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
    }
}
