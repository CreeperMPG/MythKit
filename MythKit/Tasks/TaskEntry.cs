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
        private TaskState _state;
        private string _stateMessage;
        private int _progress;
        public Guid Id { get; }
        public IManagedTask Task { get; }
        public string TaskName => Task.TaskName;
        public bool IsIndeterminate => Task.IsIndeterminate;
        public bool IsUIIndeterminate => Task.IsIndeterminate && State == TaskState.Running;
        public TaskState State
        {
            get => _state;
            private set { _state = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsUIIndeterminate)); }
        }
        public string StateMessage
        {
            get => _stateMessage;
            private set { _stateMessage = value; OnPropertyChanged(); }
        }
        public int Progress
        {
            get => _progress;
            private set { _progress = value; OnPropertyChanged(); }
        }
        public FrameworkElement DetailContentView => Task.DetailContentView;
        public bool IsDetailViewReady => Task.IsDetailViewReady;

        private DateTime _lastUiProgressUpdate = DateTime.MinValue;
        private DateTime _lastUiStateMessageUpdate = DateTime.MinValue;
        private readonly int _updateIntervalMs = 50;
        public TaskEntry(Guid id, IManagedTask task)
        {
            Id = id;
            Task = task;

            // 初始化状态
            _state = TaskState.Idle;

            Task.ProgressUpdated += (p) =>
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var now = DateTime.UtcNow;
                    if ((now - _lastUiProgressUpdate).TotalMilliseconds >= _updateIntervalMs)
                    {
                        _lastUiProgressUpdate = now;
                        Application.Current.Dispatcher.Invoke(() => Progress = p);
                    }
                    else
                    {
                        _progress = p;
                    }
                });

            Task.StateUpdated += (s) =>
                Application.Current.Dispatcher.Invoke(() =>
                {
                    State = s;
                    if (s == TaskState.Completed)
                    {
                        Progress = 100;
                    }
                });

            Task.StateMessageUpdated += (msg) =>
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var now = DateTime.UtcNow;
                    if ((now - _lastUiStateMessageUpdate).TotalMilliseconds >= _updateIntervalMs)
                    {
                        _lastUiStateMessageUpdate = now;
                        Application.Current.Dispatcher.Invoke(() => StateMessage = msg);
                    }
                    else
                    {
                        _stateMessage = msg;
                    }
                });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
