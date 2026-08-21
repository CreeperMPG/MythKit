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
    public class TestTask : IManagedTask
    {
        private readonly Random _random = new Random();
        private readonly int _durationSeconds;

        public string TaskName => "测试任务";
        public TaskState State { get; set; } = TaskState.Idle;
        public int Progress { get; set; }
        public FrameworkElement DetailContentView { get; set; }
        public bool IsDetailViewReady { get; set; }

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
                StateMessageUpdated?.Invoke("任务启动...");

                int totalSteps = _durationSeconds * 2;
                for (int step = 0; step <= totalSteps; step++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int progress = (int)((double)step / totalSteps * 100);
                    Progress = progress;
                    ProgressUpdated?.Invoke(progress);

                    // 使用 if-else 代替关系模式
                    string message;
                    if (progress < 20)
                        message = "初始化中...";
                    else if (progress < 40)
                        message = "正在加载数据...";
                    else if (progress < 60)
                        message = "处理核心逻辑...";
                    else if (progress < 80)
                        message = "生成报告...";
                    else if (progress < 100)
                        message = "收尾工作...";
                    else
                        message = "即将完成";

                    StateMessageUpdated?.Invoke($"{message} ({progress}%)");

                    int delayMs = _random.Next(200, 800);
                    await Task.Delay(delayMs, cancellationToken);

                    if (progress == 50 && !cancellationToken.IsCancellationRequested)
                    {
                        StateMessageUpdated?.Invoke("⚠️ 遇到非关键错误，自动恢复...");
                        await Task.Delay(1000, cancellationToken);
                        StateMessageUpdated?.Invoke("✅ 恢复成功，继续执行");
                    }
                }

                State = TaskState.Completed;
                StateUpdated?.Invoke(State);
                StateMessageUpdated?.Invoke("🎉 测试任务完成！");
            }
            catch (OperationCanceledException)
            {
                State = TaskState.Faulted;
                StateUpdated?.Invoke(State);
                StateMessageUpdated?.Invoke("❌ 任务已被用户取消");
                throw;
            }
            catch (Exception ex)
            {
                State = TaskState.Faulted;
                StateUpdated?.Invoke(State);
                StateMessageUpdated?.Invoke($"💥 任务异常: {ex.Message}");
                throw;
            }
        }
    }
}
