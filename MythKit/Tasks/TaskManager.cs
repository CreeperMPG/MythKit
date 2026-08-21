using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MythKit.Tasks
{
    public class TaskManager : IDisposable
    {
        private readonly ConcurrentDictionary<Guid, (CancellationTokenSource Cts, Task RunningTask)> _activeTasks = new ConcurrentDictionary<Guid, (CancellationTokenSource Cts, Task RunningTask)>();
        public ObservableCollection<TaskEntry> TaskList { get; } = new ObservableCollection<TaskEntry>();

        public Guid StartTask(IManagedTask task)
        {
            var id = Guid.NewGuid();

            var entry = new TaskEntry(id, task);

            var cts = new CancellationTokenSource();
            var runningTask = Task.Run(() => task.ExecuteAsync(cts.Token), cts.Token);

            if (_activeTasks.TryAdd(id, (cts, runningTask)))
            {
                Application.Current.Dispatcher.Invoke(() => TaskList.Add(entry));
                _ = CleanupTaskAsync(id, runningTask, entry);
            }
            return id;
        }

        public void StopTask(Guid taskId)
        {
            if (_activeTasks.TryRemove(taskId, out var entry))
            {
                entry.Cts.Cancel();
            }
        }

        private async Task CleanupTaskAsync(Guid id, Task runningTask, TaskEntry entry)
        {
            try { await runningTask; }
            catch { /* 忽略 */ }
            finally
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    entry.Task.DetailContentView?.ClearValue(FrameworkElement.DataContextProperty);
                    entry.Task.DetailContentView = null;
                    TaskList.Remove(entry);
                });
                _activeTasks.TryRemove(id, out _);
            }
        }

        private readonly CancellationTokenSource _globalCts = new CancellationTokenSource();

        public void Dispose()
        {
            _globalCts.Cancel();
            _globalCts.Dispose();
        }
    }
}
