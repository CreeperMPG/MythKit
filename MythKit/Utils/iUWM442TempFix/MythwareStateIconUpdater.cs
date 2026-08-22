using iNKORE.UI.WPF.Modern.Common.IconKeys;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MythKit.Mythware;

namespace MythKit.Utils.iUWM442TempFix
{
    internal class MythwareStateIconUpdater
    {
        public static readonly DependencyProperty StateProperty =
            DependencyProperty.RegisterAttached(
                "State",
                typeof(MythwareState),
                typeof(MythwareStateIconUpdater),
                new PropertyMetadata(MythwareState.Invalid, OnStateChanged));

        public static void SetState(DependencyObject element, MythwareState value)
            => element.SetValue(StateProperty, value);

        public static MythwareState GetState(DependencyObject element)
            => (MythwareState)element.GetValue(StateProperty);

        private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is FontIcon icon))
                return;

            var state = (MythwareState)e.NewValue;
            switch (state)
            {
                case MythwareState.Running:
                    icon.Icon = FluentSystemIcons.CheckboxChecked_16_Filled;
                    break;
                case MythwareState.NotRunning:
                    icon.Icon = FluentSystemIcons.List_16_Filled;
                    break;
                case MythwareState.Invalid:
                    icon.Icon = FluentSystemIcons.Question_16_Filled;
                    break;
            }
        }
    }
}
