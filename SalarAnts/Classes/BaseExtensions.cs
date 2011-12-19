using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public static class BaseExtensions
    {
        /// <summary>
        /// Performs the specified action on each element of the IEnumerable.
        /// </summary>
        public static void ForEachAction<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            if (action == null || enumerable == null)
            {
                throw new ArgumentNullException();
            }

            foreach (var item in enumerable)
            {
                action.Invoke(item);
            }
        }

        /// <summary>
        /// Performs the specified action on each element of the IEnumerable.
        /// </summary>
        public static void ForEachAction<T>(this IList<T> list, Action<T> action)
        {
            if (action == null || list == null)
            {
                throw new ArgumentNullException();
            }

            for (int i = 0; i < list.Count; i++)
            {
                action.Invoke(list[i]);
            }
        }

        /// <summary>
        /// Removes the all the elements that match the conditions defined by the specified predicate.
        /// </summary>
        public static void RemoveFromIList<TSource>(this IList<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null || predicate == null)
            {
                throw new ArgumentNullException();
            }
            for (int i = source.Count - 1; i >= 0; i--)
                if (predicate.Invoke(source[i]))
                {
                    source.RemoveAt(i);
                }
        }

        /// <summary>
        /// Adds range of items to the collection 
        /// </summary>
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> enumerable)
        {
            foreach (var item in enumerable)
            {
                collection.Add(item);
            }
        }
        public static IList<TSource> Where<TSource>(this IList<TSource> list, Func<TSource, bool> predicate)
        {
            if (list == null || predicate == null)
                throw new ArgumentNullException();
            var result = new List<TSource>();

            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (predicate(item))
                {
                    result.Add(item);
                }
            }
            return result;
        }

        public static bool Any<TSource>(this IList<TSource> list, Func<TSource, bool> predicate)
        {
            if (list == null || predicate == null)
                throw new ArgumentNullException();

            for (int i = 0; i < list.Count; i++)
            {
                if (predicate(list[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public static int Count<TSource>(this IList<TSource> list, Func<TSource, bool> predicate)
        {
            if (list == null || predicate == null)
                throw new ArgumentNullException();
            int num = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (predicate(list[i]))
                {
                    num++;
                }
            }
            return num;
        }

        public static TSource FirstOrDefaultFast<TSource>(this IList<TSource> list, Func<TSource, bool> predicate)
        {
            if (list == null || (predicate == null))
            {
                throw new ArgumentNullException();
            }
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (predicate(item))
                {
                    return item;
                }
            }
            return default(TSource);
        }


    }
}
