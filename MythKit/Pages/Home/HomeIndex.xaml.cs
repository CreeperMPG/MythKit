using iNKORE.UI.WPF.Modern;
using Microsoft.Win32;
using MythKit.Mythware;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
            Button clickedButton = sender as Button;
            if (clickedButton == null) return;

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
            Button clickedButton = sender as Button;
            if (clickedButton == null) return;

            if (clickedButton.DataContext is MythwareInstance instance)
            {
                Process.Start("explorer.exe", $@"/select, ""{instance.MythwarePath}""");
            }
        }
    }
}
