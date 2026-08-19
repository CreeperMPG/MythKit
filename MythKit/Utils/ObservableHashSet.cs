using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MythKit.Utils
{
    public class ObservableHashSet<T> : ObservableCollection<T>
    {
        private readonly HashSet<T> _hashSet;

        public ObservableHashSet() : base()
        {
            _hashSet = new HashSet<T>();
        }

        public ObservableHashSet(IEnumerable<T> collection) : base(collection)
        {
            _hashSet = new HashSet<T>(collection);
        }

        public new void Add(T item)
        {
            if (_hashSet.Add(item))
            {
                base.Add(item);
            }
        }

        public new bool Remove(T item)
        {
            if (_hashSet.Remove(item))
            {
                return base.Remove(item);
            }
            return false;
        }
    }
}
