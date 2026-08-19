using iNKORE.UI.WPF.Modern;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Reflection;
using Modern = iNKORE.UI.WPF.Modern.Controls;
using System.Windows.Media.Effects;

namespace MythKit.Pages.Home
{
    /// <summary>
    /// HomeIndex.xaml 的交互逻辑
    /// </summary>
    public partial class HomeIndex : UserControl
    {
        public HomeIndex()
        {
            InitializeComponent();
            string targetDirectoryPath = string.Empty;
            using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\TopDomain\\e-Learning Class Standard\\1.00"))
            {
                bool isRegistryKeyAvailable = registryKey != null;
                if (isRegistryKeyAvailable)
                {
                    object registryValue = registryKey.GetValue("TargetDirectory");
                    bool isRegistryValueAvailable = registryValue != null;
                    if (isRegistryValueAvailable)
                    {
                        targetDirectoryPath = registryValue.ToString();
                        IconCannotFind.Visibility = Visibility.Collapsed;
                        bool isStudentMainRunning = Process.GetProcessesByName("StudentMain").Any();
                        if (isStudentMainRunning)
                        {
                            IconRunning.Visibility = Visibility.Visible;
                            JiYuDetectState.Text = "正在运行";
                            JiYuOperate.Content = "关闭极域";
                        }
                        else
                        {
                            IconClosed.Visibility = Visibility.Visible;
                            JiYuDetectState.Text = "未在运行";
                            JiYuOperate.Content = "打开极域";
                        }
                        JiYuPathDisplay.Visibility = Visibility.Visible;
                        JiYuPathDisplay.Text = "Path: " + targetDirectoryPath;
                        JiYuOperate.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        JiYuDetectState.Text = "未找到极域";
                    }
                }
                else
                {
                    JiYuDetectState.Text = "未找到极域";
                }
            }
            RefreshSuggestions();
        }
        private void RefreshSuggestions()
        {
            SuggestionsPanel.Children.Clear();
            // 遍历程序集中的所有类型
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                // 获取类型上的所有 Suggestion 特性
                var suggestions = type.GetCustomAttributes(typeof(SuggestionAttribute), false) as SuggestionAttribute[];
                if (suggestions != null && suggestions.Length > 0)
                {
                    foreach (var suggestion in suggestions)
                    {
                        // 获取 IsEnabled 方法
                        var isEnabledMethod = type.GetMethod(suggestion.IsEnabled);
                        if (isEnabledMethod != null && isEnabledMethod.IsStatic)
                        {
                            // 调用 IsEnabled 方法
                            bool isEnabled = (bool)isEnabledMethod.Invoke(null, null);
                            if (isEnabled)
                            {
                                // 创建建议的 UI 元素
                                var panel = new StackPanel();

                                // 添加标题
                                var titleBlock = new TextBlock
                                {
                                    Text = suggestion.Name,
                                    FontSize = 16,
                                    TextWrapping = TextWrapping.Wrap,
                                    FontWeight = FontWeights.Bold,
                                    Margin = new Thickness(0, 0, 0, 4)
                                };
                                panel.Children.Add(titleBlock);

                                // 添加描述
                                var descBlock = new TextBlock
                                {
                                    Text = suggestion.Description,
                                    TextWrapping = TextWrapping.Wrap,
                                    Margin = new Thickness(0, 0, 0, 8)
                                };
                                panel.Children.Add(descBlock);

                                // 添加按钮
                                var button = new Button
                                {
                                    Content = suggestion.ButtonText,
                                    Margin = new Thickness(0, 4, 0, 0)
                                };

                                // 获取并绑定回调方法
                                var callbackMethod = type.GetMethod(suggestion.ButtonCallback);
                                if (callbackMethod != null && callbackMethod.IsStatic)
                                {
                                    button.Click += (s, e) =>
                                    {
                                        callbackMethod.Invoke(null, null);
                                        RefreshSuggestions();
                                    };
                                }

                                panel.Children.Add(button);

                                // 添加建议到面板
                                AddSuggestion(panel);
                            }
                        }
                    }
                }
            }
            if (SuggestionsPanel.Children.Count == 0)
            {
                SuggestionsBox.Visibility = Visibility.Collapsed;
            }
        }

        private void JiYuOperate_Click(object sender, RoutedEventArgs e)
        {
            bool isJiYuRunning = JiYuDetectState.Text == "正在运行";
            if (isJiYuRunning)
            {
                Process[] runningProcesses = Process.GetProcessesByName("StudentMain");
                foreach (Process process in runningProcesses)
                {
                    process.Kill();
                }
                IconRunning.Visibility = Visibility.Collapsed;
                IconClosed.Visibility = Visibility.Visible;
                JiYuDetectState.Text = "未在运行";
                JiYuOperate.Content = "打开极域";
            }
            else
            {
                try
                {
                    string targetDirectoryPath = string.Empty;
                    using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\TopDomain\\e-Learning Class Standard\\1.00"))
                    {
                        bool isRegistryKeyAvailable = registryKey != null;
                        if (isRegistryKeyAvailable)
                        {
                            object registryValue = registryKey.GetValue("TargetDirectory");
                            bool isRegistryValueAvailable = registryValue != null;
                            if (isRegistryValueAvailable)
                            {
                                targetDirectoryPath = registryValue.ToString();
                                Process.Start(Path.Combine(targetDirectoryPath, "StudentMain.exe"));
                                JiYuDetectState.Text = "正在运行";
                                JiYuOperate.Content = "关闭极域";
                                IconRunning.Visibility = Visibility.Visible;
                                IconClosed.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                }
                catch
                {
                    JiYuDetectState.Text = "打开极域失败";
                }
            }
        }

        private void SuggestionsPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (SuggestionsPanel.Children.Count == 0)
            {
                return;
            }
            int width = (int)e.NewSize.Width;
            int num = Math.Min((int)Math.Round(Math.Max(width, 100) / 200.0), SuggestionsPanel.Children.Count);
            SuggestionsPanel.ItemWidth = Math.Min(width / num, 250.0);
        }
        private void AddSuggestion(UIElement suggestion)
        {
            if (suggestion != null && !SuggestionsPanel.Children.Contains(suggestion))
            {
                Border border = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)), // 50% 透明白色
                    BorderBrush = new SolidColorBrush(Colors.Gray), // 灰色描边
                    BorderThickness = new Thickness(0), // 描边厚度为1
                    Child = suggestion,
                    Padding = new Thickness(16), // 内边距为8
                    CornerRadius = new CornerRadius(8), // 圆角半径为8
                    Margin = new Thickness(8),
                    Effect = new DropShadowEffect
                    {
                        Color = Colors.Black,
                        BlurRadius = 38,
                        ShadowDepth = 16,
                        Opacity = 0.2
                    }
                };

                SuggestionsPanel.Children.Add(border);
            }
        }
    }
}
