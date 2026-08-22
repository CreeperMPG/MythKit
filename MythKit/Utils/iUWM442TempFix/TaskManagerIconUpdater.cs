using iNKORE.UI.WPF.Modern.Common.IconKeys;
using iNKORE.UI.WPF.Modern.Controls;
using MythKit.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace MythKit.Utils.iUWM442TempFix
{
    internal class TaskManagerIconUpdater
    {
        public static readonly DependencyProperty StateProperty =
            DependencyProperty.RegisterAttached(
                "State",
                typeof(TaskState),
                typeof(TaskManagerIconUpdater),
                new PropertyMetadata(TaskState.Idle, OnStateChanged));

        public static void SetState(DependencyObject element, TaskState value)
            => element.SetValue(StateProperty, value);

        public static TaskState GetState(DependencyObject element)
            => (TaskState)element.GetValue(StateProperty);

        private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is FontIcon icon))
                return;

            var state = (TaskState)e.NewValue;
            switch (state)
            {
                case TaskState.Idle:
                    icon.Icon = FluentSystemIcons.Flag_20_Regular;
                    break;
                case TaskState.Running:
                    icon.Icon = FluentSystemIcons.Play_20_Regular;
                    break;
                case TaskState.Completed:
                    icon.Icon = FluentSystemIcons.Check_20_Regular;
                    break;
                case TaskState.Faulted:
                    icon.Icon = FluentSystemIcons.ErrorCircle_20_Regular;
                    break;
                case TaskState.Cancelled:
                    icon.Icon = FluentSystemIcons.Delete_20_Regular;
                    break;
            }
        }
    }
}
