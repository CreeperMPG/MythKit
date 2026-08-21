using MythKit.Tasks;
using System.Windows;

namespace MythKit
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public static TaskManager TaskManagerInstance { get; } = new TaskManager();
    }
}
