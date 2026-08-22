using MythKit.Pages.About;
using MythKit.Pages.TaskManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MythKit.Pages.Home.Suggestions
{
    [Suggestion("任务测试", "测试 1.2.0 Snapshot 2 加入的任务功能", nameof(IsEnabled), "添加测试任务", nameof(ButtonCallback))]
    internal static class TaskTestSuggestion
    {
        public static bool IsEnabled() => true;
        public static void ButtonCallback()
        {
            var task = new TestTask(durationSeconds: 20);
            var detailView = new AboutIndex();
            detailView.DataContext = task;
            task.DetailContentView = detailView;
            task.IsDetailViewReady = true;

            App.TaskManagerInstance.StartTask(task);
        }
    }
}
