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
        private Modern.HyperlinkButton GenerateLicenseButton(string dependencyName, string licenseText)
        {
            var button = new Modern.HyperlinkButton
            {
                Content = dependencyName
            };
            button.Click += (s, e) =>
            {
                var dialog = new Modern.ContentDialog
                {
                    Title = dependencyName,
                    Content = new Modern.ScrollViewerEx
                    {
                        Content = new TextBlock
                        {
                            Text = licenseText,
                            TextWrapping = TextWrapping.Wrap
                        }
                    },
                    CloseButtonText = "关闭",
                    DefaultButton = Modern.ContentDialogButton.Close
                };
                dialog.ShowAsync();
            };
            return button;
        }
        public static readonly List<string> Dependencies = new List<string>() { "CosturaFody", "Fody", "iNKOREUIWPF", "iNKOREUIWPFModern" };
        public static readonly List<string> MicrosoftMITDependenciesNames = new List<string>() { "System.ValueTuple" };
        public AboutIndex()
        {
            InitializeComponent();
            VersionInfoBlock.Text = "1.2.0 Snapshot 3";
            UpdateLogBlock.Text = new System.IO.StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("MythKit.Pages.About.Info.UpdateLog.txt")).ReadToEnd();
            foreach (string dependency in Dependencies)
            {
                DependenciesPanel.Children.Add(GenerateLicenseButton(
                    new System.IO.StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream($"MythKit.Pages.About.Info.Dependencies.{dependency}.name.txt")).ReadToEnd(),
                    new System.IO.StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream($"MythKit.Pages.About.Info.Dependencies.{dependency}.license.txt")).ReadToEnd()
                ));
            }
            foreach (string dependency in MicrosoftMITDependenciesNames)
            {
                DependenciesPanel.Children.Add(GenerateLicenseButton(
                    dependency,
                    new System.IO.StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream($"MythKit.Pages.About.Info.Dependencies.MicrosoftMIT.txt")).ReadToEnd()
                ));
            }
#if LITE
            VersionInfoBlock.Text += " (轻量版)";
            VersionInfo.Description = "轻量版不支持部分功能。";
#endif
        }
    }
}
