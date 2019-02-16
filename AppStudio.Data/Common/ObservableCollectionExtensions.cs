using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppStudio.Data
{
    public static class ObservableCollectionExtensions
    {
        public static void AddRangeUnique<T>(this ObservableCollection<T> oCollection, IEnumerable<T> items)
        {
            if (oCollection != null && items != null)
            {
                for (int i = oCollection.Count - 1; i >= 0; i--)
                {
                    // Removes old unexistant elements from oCollection.
                    if (!items.Contains(oCollection[i], EqualityComparer<T>.Default))
                    {
                        oCollection.RemoveAt(i);
                    }
                }
                foreach (var item in items)
                {
                    // Adds new elements to oCollection.
                    if (!oCollection.Contains(item))
                    {
                        oCollection.Add(item);
                    }
                }
            }
        }
    }
}