using MythKit.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MythKit.Pages.TaskManager
{
    // AI-GENERATED TEST TASK IMPLEMENTATION
    public class TestTask : IManagedTask
    {
        private readonly Random _random = new Random();
        private readonly int _durationSeconds;

        public string TaskName => "测试任务";
        public TaskState State { get; set; } = TaskState.Idle;
        public int Progress { get; set; }
        public FrameworkElement DetailContentView { get; set; }
        public bool IsDetailViewReady { get; set; }

        public bool IsIndeterminate => false;

        public event Action<int> ProgressUpdated;
        public event Action<string> StateMessageUpdated;
        public event Action<TaskState> StateUpdated;

        /// <param name="durationSeconds">任务预计运行秒数（模拟总时长）</param>
        public TestTask(int durationSeconds = 30)
        {
            _durationSeconds = durationSeconds;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                State = TaskState.Running;
                StateUpdated?.Invoke(State);
                StateMessageUpdated?.Invoke("LAUNCHING...");

                int totalSteps = _durationSeconds * 2;
                for (int step = 0; step <= totalSteps; step++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int progress = (int)((double)step / totalSteps * 100);
                    Progress = progress;
                    ProgressUpdated?.Invoke(progress);

                    StateMessageUpdated?.Invoke($"{progress}%");

                    int delayMs = _random.Next(200, 800);
                    await Task.Delay(delayMs, cancellationToken);

                    if (progress == 50 && !cancellationToken.IsCancellationRequested)
                    {
                        StateMessageUpdated?.Invoke("⚠️ DELAY TEST...");
                        await Task.Delay(1000, cancellationToken);
                        StateMessageUpdated?.Invoke("✅ CONTINUE");
                    }
                }

                State = TaskState.Completed;
                StateUpdated?.Invoke(State);
                StateMessageUpdated?.Invoke("🎉 COMPLETED");
            }
            catch (OperationCanceledException)
            {
                StateMessageUpdated?.Invoke("❌ CANCELLED");
                State = TaskState.Cancelled;
                StateUpdated?.Invoke(State);
                throw;
            }
            catch (Exception ex)
            {
                State = TaskState.Faulted;
                StateUpdated?.Invoke(State);
                StateMessageUpdated?.Invoke($"💥 EXCEPTION: {ex.Message}");
                throw;
            }
        }
    }
}
