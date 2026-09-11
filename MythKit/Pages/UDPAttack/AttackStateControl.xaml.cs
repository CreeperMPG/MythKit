using MythKit.Tasks;
using System;
using System.Collections.Generic;
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

namespace MythKit.Pages.UDPAttack
{
    /// <summary>
    /// AttackStateControl.xaml 的交互逻辑
    /// </summary>
    public partial class AttackStateControl : UserControl
    {
        public AttackManagedTask AttackTask { get; private set; }
        public AttackStateControl(AttackManagedTask attackManagedTask)
        {
            InitializeComponent();
            AttackTask = attackManagedTask;
            if (attackManagedTask.IsIndeterminate)
            {
                AttackProgressText.Text = "正在进行无限次攻击";
            }
            DataContext = this;
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            AttackTask.Paused = !AttackTask.Paused;
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Text = "";
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            App.TaskManagerInstance.StopTask(AttackTask);
        }
    }
}
