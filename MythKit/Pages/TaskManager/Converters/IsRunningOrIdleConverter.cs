using MythKit.Tasks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace MythKit.Pages.TaskManager.Converters
{
    public class IsRunningOrIdleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskState status)
            {
                // 当任务状态为 Running 或 Idle 时，返回 true（可启用）
                return status == TaskState.Running || status == TaskState.Idle;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
