using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MythKit.Tasks
{
    public enum TaskStatus { Idle, Running, Completed, Faulted }

    public interface IManagedTask
    {
        string TaskName { get; }
        TaskStatus Status { get; set; }
        int Progress { get; set; }

        // 详情控件（UI线程赋值）
        FrameworkElement DetailContentView { get; set; }
        bool IsDetailViewReady { get; set; }

        // 事件通知（用于UI更新）
        event Action<int> ProgressUpdated;
        event Action<string> StatusMessageUpdated;
        event Action<TaskStatus> StatusUpdated;

        Task ExecuteAsync(CancellationToken cancellationToken);
    }
}
