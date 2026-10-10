using iNKORE.UI.WPF.Modern;
using iNKORE.UI.WPF.Modern.Media.Animation;
using MythKit.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MythKit
{
    public partial class HomePage : UserControl, INotifyPropertyChanged
    {
        public static Dictionary<Type, object> NavPages = new Dictionary<Type, object>();
        public static event Action HomePageLoadedEvent;
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        private int _availableTaskCount;
        public int AvailableTaskCount
        {
            get => _availableTaskCount;
            set
            {
                if (_availableTaskCount != value)
                {
                    _availableTaskCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public static HomePage Instance;
        public HomePage()
        {
            InitializeComponent();
            Instance = this;
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
            ReplayAttackViewItem.IsEnabled = false;
#endif
            HomePageLoadedEvent.Invoke();
            Attach();
        }
        public void UpdateAvailableTaskCount()
        {
            AvailableTaskCount = App.TaskManagerInstance.TaskList.Count(task => task.State != TaskState.Completed && task.State != TaskState.Cancelled);
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

        public void AppFrameNavigate(Type navPageType, NavigationTransitionInfo transitionInfo)
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
        private readonly HashSet<TaskEntry> _subscribedTasks = new HashSet<TaskEntry>();

        private void Attach()
        {
            App.TaskManagerInstance.TaskList.CollectionChanged += OnTaskListCollectionChanged;

            foreach (var task in App.TaskManagerInstance.TaskList)
                SubscribeTask(task);
        }

        private void OnTaskListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var task in _subscribedTasks.ToList())
                    UnsubscribeTask(task);

                foreach (var task in App.TaskManagerInstance.TaskList)
                    SubscribeTask(task);
            }
            else
            {
                if (e.OldItems != null)
                {
                    foreach (TaskEntry task in e.OldItems)
                        UnsubscribeTask(task);
                }

                if (e.NewItems != null)
                {
                    foreach (TaskEntry task in e.NewItems)
                        SubscribeTask(task);
                }
            }

            UpdateAvailableTaskCount();
        }

        private void SubscribeTask(TaskEntry task)
        {
            if (_subscribedTasks.Add(task))
            {
                task.PropertyChanged += OnTaskPropertyChanged;
            }
        }

        private void UnsubscribeTask(TaskEntry task)
        {
            if (_subscribedTasks.Remove(task))
            {
                task.PropertyChanged -= OnTaskPropertyChanged;
            }
        }

        private void OnTaskPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TaskEntry.State))
            {
                UpdateAvailableTaskCount();
            }
        }
    }

}
