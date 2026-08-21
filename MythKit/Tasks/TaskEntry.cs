using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MythKit.Tasks
{
    public class TaskEntry : INotifyPropertyChanged
    {
        private TaskStatus _status;
        private int _progress;
        public Guid Id { get; }
        public IManagedTask Task { get; }
        public string TaskName => Task.TaskName;
        public TaskStatus Status
        {
            get => _status;
            private set { _status = value; OnPropertyChanged(); }
        }
        public int Progress
        {
            get => _progress;
            private set { _progress = value; OnPropertyChanged(); }
        }
        public FrameworkElement DetailContentView => Task.DetailContentView;
        public bool IsDetailViewReady => Task.IsDetailViewReady;

        public TaskEntry(Guid id, IManagedTask task)
        {
            Id = id;
            Task = task;

            // 初始化状态
            _status = task.Status;
            _progress = task.Progress;

            Task.ProgressUpdated += (p) =>
                Application.Current.Dispatcher.Invoke(() => Progress = p);

            Task.StatusUpdated += (s) =>
                Application.Current.Dispatcher.Invoke(() => Status = s);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
