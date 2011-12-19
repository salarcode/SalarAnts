
namespace SalarAnts.Defination
{
	public class LinkedItem<T>
	{
		public LinkedItem(T item, T next, T previous)
		{
			Value = item;
			Next = next;
			Previous = previous;
		}

		public T Value { get; set; }
		public T Next { get; private set; }
		public T Previous { get; private set; }
	}
}
