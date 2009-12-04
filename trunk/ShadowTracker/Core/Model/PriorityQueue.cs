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

		private readonly object SyncLock = new object();

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
			: this(capacity, Comparer<T>.Default)
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="comparer">comparer</param>
		public PriorityQueue(IComparer<T> comparer)
			: this(MinCapacity, comparer)
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="capacity">initial capacity</param>
		/// <param name="comparer">comparer</param>
		public PriorityQueue(int capacity, IComparer<T> comparer)
			: this(capacity, delegate(T a, T b) { return comparer.Compare(a, b) > 0; })
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
		/// <param name="collection"></param>
		/// <param name="higherPriorityPredicate">predicate that returns true if the first argument is higher priority than the second; if equal or lesser priority returns false</param>
		public PriorityQueue(IEnumerable<T> collection, Func<T, T, bool> higherPriorityPredicate)
			: this(MinCapacity, higherPriorityPredicate)
		{
			this.AddRange(collection);
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

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="queue"></param>
		public PriorityQueue(PriorityQueue<T> queue)
		{
			this.HigherPriority = queue.HigherPriority;
			this.count = queue.count;
			this.version = queue.version;
			this.data = new T[queue.data.Length];

			Array.Copy(queue.data, this.data, this.count);
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

		#region List Methods

		/// <summary>
		/// Adds the elements of the specified collection to the queue.
		/// </summary>
		/// <param name="values"></param>
		public void AddRange(IEnumerable<T> collection)
		{
			foreach (T value in collection)
			{
				this.Enqueue(value);
			}
		}

		/// <summary>
		/// Determines whether an element is in the queue.
		/// </summary>
		/// <param name="item"></param>
		/// <returns>true if item is found in the queue; otherwise, false</returns>
		public bool Contains(T item)
		{
			return this.Contains(item, EqualityComparer<T>.Default);
		}

		/// <summary>
		/// Determines whether an element is in the queue.
		/// </summary>
		/// <param name="item"></param>
		/// <returns>true if item is found in the queue; otherwise, false</returns>
		public bool Contains(T item, EqualityComparer<T> comparer)
		{
			if (comparer == null)
			{
				throw new ArgumentNullException("comparer");
			}

			if (item == null)
			{
				for (int j=0; j<this.count; j++)
				{
					if (this.data[j] == null)
					{
						return true;
					}
				}

				return false;
			}

			for (int i=0; i<this.count; i++)
			{
				if (comparer.Equals(this.data[i], item))
				{
					return true;
				}
			}

			return false;
		}

		#endregion List Methods

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
			get { return false; }
		}

		/// <summary>
		/// Gets an object that can be used to synchronize access to the queue.
		/// </summary>
		public object SyncRoot
		{
			get { return this.SyncLock; }
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
			return new Enumerator(this);
		}

		/// <summary>
		/// Clones the internal storage and enumerates by Dequeueing
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public class Enumerator : IEnumerator<T>
		{
			#region Fields

			private readonly PriorityQueue<T> Original;
			private readonly int Version;

			private PriorityQueue<T> Enumerated;
			private T current;

			#endregion Fields

			#region Init

			/// <summary>
			/// Ctor
			/// </summary>
			/// <param name="queue"></param>
			internal Enumerator(PriorityQueue<T> queue)
			{
				this.Original = queue;
				this.Version = this.Original.version;
				this.Enumerated = new PriorityQueue<T>(this.Original);
			}

			#endregion Init

			#region IEnumerator<T> Members

			public T Current
			{
				get { return this.current; }
			}

			#endregion IEnumerator<T> Members

			#region IDisposable Members

			void IDisposable.Dispose()
			{
			}

			#endregion IDisposable Members

			#region IEnumerator Members

			object IEnumerator.Current
			{
				get { return this.current; }
			}

			public bool MoveNext()
			{
				if (this.Version != this.Original.version)
				{
					throw new InvalidOperationException("Collection was modified after the enumerator was instantiated.");
				}

				if (this.Enumerated.Count < 1)
				{
					return false;
				}

				this.current = this.Enumerated.Dequeue();
				return true;
			}

			public void Reset()
			{
				if (this.Version != this.Original.version)
				{
					throw new InvalidOperationException("Collection was modified after the enumerator was instantiated.");
				}

				this.Enumerated = new PriorityQueue<T>(this.Original);
			}

			#endregion IEnumerator Members
		}

		#endregion IEnumerable<T> Members
	}
}
