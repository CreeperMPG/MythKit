using iNKORE.UI.WPF.Modern;
using Microsoft.Win32;
using MythKit.Mythware;
using MythKit.Pages.About;
using MythKit.Pages.TaskManager;
using MythKit.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Modern = iNKORE.UI.WPF.Modern.Controls;

namespace MythKit.Pages.Home
{
    /// <summary>
    /// HomeIndex.xaml 的交互逻辑
    /// </summary>
    public partial class HomeIndex : UserControl
    {
        public ObservableCollection<MythwareInstance> MythwareInstances { get; set; }
        public HomeIndex()
        {
            InitializeComponent();
            RefreshSuggestions();
            DataContext = this;
            MythwareInstances = MythwareInstance.GetInstances();
        }
        public void Refresh()
        {
            foreach (var instance in MythwareInstances)
            {
                instance.UpdateState();
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
                if (type.GetCustomAttributes(typeof(SuggestionAttribute), false) is SuggestionAttribute[] suggestions && suggestions.Length > 0)
                {
                    foreach (var suggestion in suggestions)
                    {
                        var isEnabledMethod = type.GetMethod(suggestion.IsEnabled);
                        if (isEnabledMethod != null && isEnabledMethod.IsStatic)
                        {
                            bool isEnabled = (bool)isEnabledMethod.Invoke(null, null);
                            if (isEnabled)
                            {
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

        private void JiYuOperateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button clickedButton)) return;

            if (clickedButton.DataContext is MythwareInstance instance)
            {
                if (instance.State == MythwareState.Running)
                {
                    instance.Kill();
                }
                else if (instance.State == MythwareState.NotRunning)
                {
                    instance.Launch();
                }
                Task.Run(async () =>
                {
                    await Task.Delay(500);
                    instance.UpdateState();
                });
            }
        }

        private void OpenDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button clickedButton)) return;

            if (clickedButton.DataContext is MythwareInstance instance)
            {
                var path = instance.MythwarePath;
                if (!File.Exists(path))
                {
                    Process.Start("explorer.exe", Path.GetDirectoryName(path));
                }
                else
                {
                    Process.Start("explorer.exe", $@"/select, ""{path}""");
                }
            }
        }

        private string TDPassword = null;
        private string GetTDPassword()
        {
            object registryValue = RegUtils.GetRegistryValue(Registry.CurrentUser, "SOFTWARE\\Wow6432Node\\TopDomain\\e-Learning Class Standard\\1.00", "UninstallPasswd");
            if (registryValue == null)
            {
                throw new Exception("未找到注册表项");
            }
            if (registryValue.ToString() != "Passwd[123456]")
            {
                // 极域低版本，密码格式 Passwd<xxxxxx>
                return registryValue.ToString().Substring(6);
            }
            else
            {
                // 高版本加密密码
                // ref => https://github.com/imengyu/JiYuTrainer
                byte[] buffer = RegUtils.GetRegistryValue(Registry.CurrentUser, "SOFTWARE\\Wow6432Node\\TopDomain\\e-Learning Class\\Student", "Knock1") as byte[];
                // 原 C++ 代码中分别异或了 0x50434C45 和 0x454C4350
                // 0x50434C45 ^ 0x454C4350 = 0x150F0F15
                uint xorMask = 0x150F0F15;
                for (int i = 0; i <= buffer.Length - 4; i += 4)
                {
                    uint val = BitConverter.ToUInt32(buffer, i);
                    val ^= xorMask;
                    byte[] decryptedDword = BitConverter.GetBytes(val);
                    Array.Copy(decryptedDword, 0, buffer, i, 4);
                }

                // 获取宽字符串的偏移量
                byte offset = buffer[0];
                if (offset >= buffer.Length)
                    return null;

                // 提取 UTF-16LE 字符串，直到遇到 \0\0 终止符
                int maxBytes = buffer.Length - offset;
                int charCount = 0;
                while (charCount + 1 < maxBytes)
                {
                    // 检查是否到达宽字符空终止符 (\0\0)
                    if (buffer[offset + charCount] == 0 && buffer[offset + charCount + 1] == 0)
                        break;
                    charCount += 2;
                }
                return Encoding.Unicode.GetString(buffer, offset, charCount);
            }
        }
        private void TDPasswordCopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (TDPassword != null)
                Clipboard.SetText(TDPassword);
        }

        private void GetTDPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string password = GetTDPassword();
                TDPasswordBlock.Text = $"密码: {password}";
                TDPassword = password;
                TDPasswordCopyButton.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                TDPasswordBlock.Text = $"获取密码失败: {ex.Message}";
            }
        }
    }
}
