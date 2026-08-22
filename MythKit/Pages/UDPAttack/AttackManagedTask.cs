using MythKit.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MythKit.Pages.UDPAttack
{
    internal class AttackManagedTask : IManagedTask
    {
        public string TaskName => throw new NotImplementedException();

        public Tasks.TaskState State { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int Progress { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public FrameworkElement DetailContentView { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsDetailViewReady { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsIndeterminate => throw new NotImplementedException();

        public event Action<int> ProgressUpdated;
        public event Action<string> StateMessageUpdated;
        public event Action<TaskState> StateUpdated;

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
