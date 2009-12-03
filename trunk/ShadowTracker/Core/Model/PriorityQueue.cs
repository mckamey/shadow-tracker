using System;
using System.Collections;
using System.Collections.Generic;

namespace Shadow.Model
{
	/// <summary>
	/// A queue which always returns the highest priority element.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class PriorityQueue<T> :
		IEnumerable<T>,
		ICollection,
		IEnumerable
	{
		#region Constants

		private const int MinCapacity = 4;
		private const int GrowthRate = 2;
		private const double TrimThreshold = 0.9;

		#endregion Constants

		#region Fields

		private T[] data;
		private int count;
		private int version;

		private readonly Func<T, T, bool> HigherPriority;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public PriorityQueue()
			: this(MinCapacity)
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="capacity">initial capacity</param>
		public PriorityQueue(int capacity)
			: this(capacity, delegate(T a, T b) { return Comparer<T>.Default.Compare(a, b) == 1; })
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="higherPriorityPredicate">predicate that returns true if the first argument is higher priority than the second; if equal or lesser priority returns false</param>
		public PriorityQueue(Func<T, T, bool> higherPriorityPredicate)
			: this(MinCapacity, higherPriorityPredicate)
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="capacity">initial capacity</param>
		/// <param name="higherPriorityPredicate">predicate that returns true if the first argument is higher priority than the second; if equal or lesser priority returns false</param>
		public PriorityQueue(int capacity, Func<T, T, bool> higherPriorityPredicate)
		{
			this.HigherPriority = higherPriorityPredicate;
			this.SetCapacity(capacity);
		}

		#endregion Init

		#region Queue Methods

		/// <summary>
		/// Adds an element to the end of the queue and lets it trickle up to the correct position
		/// </summary>
		/// <param name="value"></param>
		public void Enqueue(T value)
		{
			// ensure there is enough room to add more
			if (this.count >= this.data.Length)
			{
				// grow internal storage
				this.SetCapacity(GrowthRate * this.count);
			}

			// insert value at end of queue
			int index = this.count;
			this.data[index] = value;
			this.count++;

			// trickle up to correct position
			int parent = (index - 1) / 2;
			while (index > 0 && this.HigherPriority(this.data[index], this.data[parent]))
			{
				this.Swap(index, parent);
				index = parent;
				parent = (index - 1) / 2;
			}

			this.version++;
		}

		/// <summary>
		/// Returns the element with the highest priority in the queue without removing it
		/// </summary>
		public T Peek()
		{
			if (this.count <= 0)
			{
				throw new InvalidOperationException("queue is empty");
			}

			return this.data[0];
		}

		/// <summary>
		/// Removes and returns the element with the highest priority in the queue
		/// </summary>
		/// <returns></returns>
		public T Dequeue()
		{
			if (this.count <= 0)
			{
				throw new InvalidOperationException("queue is empty");
			}

			T current = this.data[0];

			// move the last item into root and trickle down to correct position
			this.count--;
			this.data[0] = this.data[this.count];
			this.RebuildHeap();
			this.version++;

			return current;
		}

		/// <summary>
		/// Removes all elements from the queue
		/// </summary>
		public void Clear()
		{
			this.count = 0;
			this.version++;
		}

		/// <summary>
		/// Sets the capacity to the actual number of elements in the queue,
		/// if that number is less than 90 percent of current capacity
		/// </summary>
		public void TrimExcess()
		{
			int threshold = (int)(this.data.Length * TrimThreshold);
			if (this.count < threshold)
			{
				this.SetCapacity(this.count);
			}
		}

		/// <summary>
		/// Sets the capacity of the underlying storage
		/// </summary>
		/// <param name="capacity"></param>
		private void SetCapacity(int capacity)
		{
			// bound capacity between MinCapacity and the current count
			capacity = Math.Max(this.count, Math.Max(MinCapacity, capacity));

			T[] array = new T[capacity];
			if (this.count > 0)
			{
				Array.Copy(this.data, 0, array, 0, this.count);
			}
			this.data = array;
		}

		#endregion Queue Methods

		#region Rebuild Heap Methods

		/// <summary>
		/// Ripples the value at the given index down to the correct position
		/// </summary>
		private void RebuildHeap()
		{
			// this could be passed in as a parameter for arbitrary rebuilding
			int index = 0;

			while (this.count > index)
			{
				// calc the indices of the children for the given parent index
				int left = 2 * index + 1;
				int right = left + 1;

				// find the highest priority between left and right children
				int priority;
				if (right >= this.count)
				{
					if (left >= this.count)
					{
						// index is leaf node, no more trickle down
						break;
					}

					// no right node, must be left
					priority = left;
				}
				else
				{
					if (this.HigherPriority(this.data[left], this.data[right]))
					{
						priority = left;
					}
					else
					{
						priority = right;
					}
				}

				if (this.HigherPriority(this.data[index], this.data[priority]))
				{
					// index is already the highest priority of the three nodes
					break;
				}

				// highest priority child is higher than index, swap and continue
				this.Swap(index, priority);
				index = priority;
			}
		}

		/// <summary>
		/// Swaps the values at the two indices.
		/// </summary>
		/// <param name="a">index a</param>
		/// <param name="b">index b</param>
		private void Swap(int a, int b)
		{
			T temp = this.data[b];
			this.data[b] = this.data[a];
			this.data[a] = temp;
		}

		#endregion Rebuild Heap Methods

		#region ICollection Members

		/// <summary>
		/// Copies the elements to an existing Array, starting at the specified starting index.
		/// </summary>
		/// <param name="array">destination array</param>
		/// <param name="index">starting index for copying</param>
		public void CopyTo(Array array, int index)
		{
			int i = index;
			foreach (T item in this)
			{
				array.SetValue(item, i);
				i++;
			}
		}

		/// <summary>
		/// Gets the number of elements contained in the queue.
		/// </summary>
		public int Count
		{
			get { return this.count; }
		}

		/// <summary>
		/// Gets a value indicating whether access to the queue is synchronized (thread safe). 
		/// </summary>
		public bool IsSynchronized
		{
			get { return this.data.IsSynchronized; }
		}

		/// <summary>
		/// Gets an object that can be used to synchronize access to the queue.
		/// </summary>
		public object SyncRoot
		{
			get { return this.data.SyncRoot; }
		}

		#endregion ICollection Members

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<T>)this.data).GetEnumerator();
		}

		#endregion IEnumerable Members

		#region IEnumerable<T> Members

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			// TODO: ensure that this is returned in priority order
			for (int i=0; i< this.count; i++)
			{
				yield return this.data[i];
			}
		}

		#endregion IEnumerable<T> Members
	}
}
