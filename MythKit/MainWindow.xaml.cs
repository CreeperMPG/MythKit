using iNKORE.UI.WPF.Modern.Controls.Helpers;
using iNKORE.UI.WPF.Modern.Controls.Primitives;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using Modern = iNKORE.UI.WPF.Modern.Controls;

namespace MythKit
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        // 导入 Win32 API
        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);
        public static MainWindow Instance;
        public void ShowState(string message, string title = "", Modern.InfoBarSeverity severity = Modern.InfoBarSeverity.Informational)
        {
            Dispatcher.Invoke(() =>
            {
                StateInfo.IsOpen = true;
                StateInfo.Message = message;
                StateInfo.Title = title;
                StateInfo.Severity = severity;
            });
        }
        public MainWindow()
        {
            Instance = this;
            InitializeComponent();
            /* 设置窗口为不可被捕获
            Loaded += (s, e) =>
            {
#if !DEBUG
                SetWindowDisplayAffinity(new WindowInteropHelper(this).Handle, 0x00000011);
#endif
            };*/
            //Task.Run(Server.CheckAndStartServer);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            WindowHelper.SetUseModernWindowStyle(this, true);
            int OSVersionBuild = Environment.OSVersion.Version.Build;
            if (OSVersionBuild >= 10240 && OSVersionBuild < 22000)
            {
                WindowHelper.SetSystemBackdropType(this, iNKORE.UI.WPF.Modern.Helpers.Styles.BackdropType.Acrylic10);
            }
            else
            {
                WindowHelper.SetSystemBackdropType(this, iNKORE.UI.WPF.Modern.Helpers.Styles.BackdropType.Mica);
            }
            TitleBar.SetHeight(this, 36);
#endif
        }
    }

    //private void CommandExecutingAbility_True(object sender, CanExecuteRoutedEventArgs e)
    //{
    //    e.CanExecute = true;
    //}
    //private TabItem GenerateNewTab(FrameworkElement content, string header = "新标签页", Modern.IconElement icon = null)
    //{
    //    var tab = new TabItem
    //    {
    //        Header = header,
    //        Content = content,
    //        Background = new SolidColorBrush(Colors.Transparent),
    //    };
    //    TabItemHelper.SetIcon(tab, icon);
    //    return tab;
    //}
    //private void CreateNewTab(FrameworkElement content, string header = "新标签页", Modern.IconElement icon = null)
    //{
    //    var tab = GenerateNewTab(content, header, icon);
    //    AppTabView.Items.Add(tab);
    //    AppTabView.SelectedItem = tab;
    //}

    //private void AddTabCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    //{
    //    CreateNewTab(new HomePage(), "新标签页", new Modern.FontIcon { Icon = FluentSystemIcons.TabAdd_20_Filled });
    //}
}

