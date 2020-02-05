using System.Windows;
using System.Windows.Input;

namespace SvenCharactor
{
    public enum CheckWindowReturn
    {
        RETURN_NONE = 0,
        RETURN_OK = 1,
        RETURN_NO = 2
    }
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class CheckWindow : Window
    {
        CheckWindowReturn Result;
        public CheckWindow(in string _Str)
        {
            InitializeComponent();
            Text.Content = _Str;
            Result = CheckWindowReturn.RETURN_NONE;
            this.ShowDialog();
        }


        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Result = CheckWindowReturn.RETURN_OK;
            this.Close();
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            Result = CheckWindowReturn.RETURN_NO;
            this.Close();
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
        /// 显示提示框
        /// </summary>
        /// <param name="messageBoxText">提示消息</param>
        /// <returns></returns>
        public static CheckWindowReturn Show(string messageBoxText)
        {
            CheckWindow window = new CheckWindow(messageBoxText)
            {
                Owner = Application.Current.MainWindow,
                Topmost = true
            }; 
            return window.Result;
        }
    }
}
