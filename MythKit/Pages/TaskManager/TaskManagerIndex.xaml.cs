using MythKit.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Modern = iNKORE.UI.WPF.Modern.Controls;

namespace MythKit.Pages.TaskManager
{
    /// <summary>
    /// TaskManagerIndex.xaml 的交互逻辑
    /// </summary>
    public partial class TaskManagerIndex : UserControl
    {
        public ObservableCollection<TaskEntry> TaskList => App.TaskManagerInstance.TaskList;
        public TaskManagerIndex()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void InformationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is TaskEntry taskEntry)
            {
                Modern.ContentDialog dialog = new Modern.ContentDialog
                {
                    Title = taskEntry.TaskName,
                    Content = taskEntry.DetailContentView,
                    CloseButtonText = "关闭"
                };
                dialog.ShowAsync();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is TaskEntry taskEntry)
            {
                App.TaskManagerInstance.StopTask(taskEntry.Id);
            }
        }
    }
}
