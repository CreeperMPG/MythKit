using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace MythKit.Utils
{
    public static class WPFUtils
    {
        public static T FindVisualChild<T>(this DependencyObject parent, string name = null) where T : FrameworkElement
        {
            if (parent == null) return null;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typed &&
                    (name == null || typed.Name == name))
                    return typed;

                var result = child.FindVisualChild<T>(name);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
