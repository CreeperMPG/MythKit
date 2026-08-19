using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Modern = iNKORE.UI.WPF.Modern.Controls;

namespace MythKit.Pages.About
{
    /// <summary>
    /// AboutIndex.xaml 的交互逻辑
    /// </summary>
    public partial class AboutIndex : UserControl
    {
        public static readonly List<string> Dependencies = new List<string>() { "CosturaFody", "Fody", "iNKOREUIWPF", "iNKOREUIWPFModern", "SystemValueTuple" };
        public AboutIndex()
        {
            InitializeComponent();
            VersionInfoBlock.Text = "MYTHKIT DEV";
            UpdateLogBlock.Text = new System.IO.StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("MythKit.Pages.About.Info.UpdateLog.txt")).ReadToEnd();
            foreach (string dependency in Dependencies)
            {
                var button = new Modern.HyperlinkButton
                {
                    Content = new System.IO.StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream($"MythKit.Pages.About.Info.Dependencies.{dependency}.name.txt")).ReadToEnd()
                };
                button.Click += (s, e) =>
                {
                    var dialog = new Modern.ContentDialog
                    {
                        Title = button.Content.ToString(),
                        Content = new Modern.ScrollViewerEx
                        {
                            Content = new TextBlock
                            {
                                Text = new System.IO.StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream($"MythKit.Pages.About.Info.Dependencies.{dependency}.license.txt")).ReadToEnd(),
                                TextWrapping = TextWrapping.Wrap
                            }
                        },
                        CloseButtonText = "关闭",
                        DefaultButton = Modern.ContentDialogButton.Close
                    };
                    dialog.ShowAsync();
                };
                DependenciesPanel.Children.Add(button);
            }
#if LITE
            VersionInfoBlock.Text += " (轻量版)";
            VersionInfo.Description = "轻量版不支持部分功能。";
#endif
        }
    }
}
