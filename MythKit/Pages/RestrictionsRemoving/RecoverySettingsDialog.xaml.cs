using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Modern = iNKORE.UI.WPF.Modern.Controls;

namespace MythKit.Pages.RestrictionsRemoving
{
    /// <summary>
    /// RecoverySettingsDialog.xaml 的交互逻辑
    /// </summary>
    public partial class RecoverySettingsDialog : Modern.ContentDialog, INotifyPropertyChanged
    {
        private List<Tuple<string, bool, string>> _recoveryResults;
        public List<Tuple<string, bool, string>> RecoveryResults
        {
            get
            {
                return _recoveryResults;
            }
            set
            {
                _recoveryResults = value;
                OnPropertyChanged();
                if (value.Count != 0)
                {
                    CloseButtonText = "关闭";
                    ResultPanel.Visibility = Visibility.Visible;
                    PendingPanel.Visibility = Visibility.Collapsed;
                }
            }
        }
        public RecoverySettingsDialog()
        {
            InitializeComponent();
            DataContext = this;
            using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
            {
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                AdminPrompt.Visibility = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
