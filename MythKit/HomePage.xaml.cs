using iNKORE.UI.WPF.Modern;
using iNKORE.UI.WPF.Modern.Media.Animation;
using MythKit.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MythKit
{
    public partial class HomePage : UserControl
    {
        public static Dictionary<Type, object> NavPages = new Dictionary<Type, object>();
        public ObservableCollection<TaskEntry> TaskList => App.TaskManagerInstance.TaskList;
        public HomePage()
        {
            InitializeComponent();
            bool isWindows11OrLater = Environment.OSVersion.Version.Build >= 22000;
            if (isWindows11OrLater)
            {
                NavView.Resources[ThemeKeys.NavigationViewContentBackgroundKey] = new SolidColorBrush
                {
                    Color = Colors.Transparent
                };
                NavView.Resources[ThemeKeys.ExpanderHeaderBorderBrushKey] = new SolidColorBrush
                {
                    Color = Colors.Transparent
                };
                NavView.Resources[ThemeKeys.NavigationViewContentGridBorderThicknessKey] = new Thickness(0.0, 0.0, 0.0, 0.0);
            }
            NavigationInit();
            DataContext = this;
#if LITE
            UDPAttackViewItem.IsEnabled = false;
#endif
        }

        public void NavigationInit()
        {
            NavView.ItemInvoked += (sender, args) =>
            {
                string invokedItemTag = args.InvokedItemContainer?.Tag?.ToString();
                if (invokedItemTag != null)
                {
                    Type targetType = Type.GetType(invokedItemTag);
                    NavView.Header = args.InvokedItemContainer?.Content as string;
                    if (targetType != null)
                    {
                        AppFrameNavigate(targetType, args.RecommendedNavigationTransitionInfo);
                    }
                }
            };
            NavView.SelectedItem = NavView.MenuItems[0];
            NavView.Header = "主页";
            AppFrameNavigate(typeof(Pages.Home.HomeIndex), null);
        }

        private void AppFrameNavigate(Type navPageType, NavigationTransitionInfo transitionInfo)
        {
            if (navPageType == null) return;

            if (ContentFrame.Content?.GetType() == navPageType)
            {
                return;
            }

            if (NavPages.TryGetValue(navPageType, out var cachedPage))
            {
                ContentFrame.Navigate(cachedPage);
            }
            else
            {
                var newPageInstance = Activator.CreateInstance(navPageType);
                if (newPageInstance != null)
                {
                    NavPages[navPageType] = newPageInstance;
                    ContentFrame.Navigate(newPageInstance, transitionInfo);
                }
            }
        }
    }

}
