using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
	public static class HelperExtensions
	{
		public static int FirstIndexOf<T>(this IList<T> list, Func<T, bool> action)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (action.Invoke(list[i]))
				{
					return i;
				}
			}
			return -1;
		}

		public static void RemoveFirst<T>(this IList<T> list, Func<T, bool> action)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (action.Invoke(list[i]))
				{
					list.RemoveAt(i);
					return;
				}
			}
		}

		public static T MinValue<T>(this IList<T> list, Func<T, int> action)
		{
			if (action == null || list == null)
			{
				throw new ArgumentNullException();
			}

			int minValue = int.MaxValue;
			T result = default(T);
			for (int i = 0; i < list.Count; i++)
			{
				var item = list[i];
				var val = action.Invoke(item);
				if (val < minValue)
				{
					result = item;
					minValue = val;
				}
			}
			return result;
		}

		public static T MinValue<T>(this IEnumerable<T> enumerable, Func<T, int> action)
		{
			if (action == null || enumerable == null)
			{
				throw new ArgumentNullException();
			}

			int minValue = int.MaxValue;
			T result = default(T);
			foreach (var item in enumerable)
			{
				var val = action.Invoke(item);
				if (val < minValue)
				{
					result = item;
					minValue = val;
				}
			}
			return result;
		}

		public static T MaxValue<T>(this IList<T> list, Func<T, int> action)
		{
			if (action == null || list == null)
			{
				throw new ArgumentNullException();
			}

			int maxValue = int.MinValue;
			T result = default(T);
			for (int i = 0; i < list.Count; i++)
			{
				var item = list[i];
			
				var val = action.Invoke(item);
				if (val > maxValue)
				{
					result = item;
					maxValue = val;
				}
			}
			return result;
		}

		public static T MaxValue<T>(this IEnumerable<T> enumerable, Func<T, int> action)
		{
			if (action == null || enumerable == null)
			{
				throw new ArgumentNullException();
			}

			int maxValue = int.MinValue;
			T result = default(T);
			foreach (var item in enumerable)
			{
				var val = action.Invoke(item);
				if (val > maxValue)
				{
					result = item;
					maxValue = val;
				}
			}
			return result;
		}

	}
}
