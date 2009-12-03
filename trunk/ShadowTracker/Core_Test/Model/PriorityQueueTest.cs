using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Shadow.Model.Test
{
	/// <summary>
	/// Summary description for PriorityQueueTest
	/// </summary>
	[TestClass]
	public class PriorityQueueTest
	{
		private PriorityQueue<int> queue;
		private Func<int, int, bool> comparer;

		[TestInitialize()]
		public void InitQueue()
		{
			this.comparer = (a, b) => a > b;
			this.queue = new PriorityQueue<int>(this.comparer);
		}

		[TestMethod]
		public void TestRandom()
		{
			int[] values = { 0, 5, 4, 3, 8, 7, 2, 1, 0, 9, 8, 1, 5, 6 };

			foreach (int value in values)
			{
				this.queue.Enqueue(value);
			}

			foreach (int value in values.OrderByDescending(n => n))
			{
				Assert.AreEqual(value, this.queue.Dequeue());
			}
		}

		[TestMethod]
		public void TestReverseSorted()
		{
			int[] values = { 9, 9, 8, 8, 7, 6, 5, 4, 3, 3, 2, 1, 0, 0 };

			foreach (int value in values)
			{
				this.queue.Enqueue(value);
			}

			foreach (int value in values.OrderByDescending(n => n))
			{
				Assert.AreEqual(value, this.queue.Dequeue());
			}
		}

		[TestMethod]
		public void TestPreSorted()
		{
			int[] values = { 0, 0, 1, 2, 3, 3, 4, 4, 5, 6, 7, 8, 8, 9, 9 };

			foreach (int value in values)
			{
				this.queue.Enqueue(value);
			}

			foreach (int value in values.OrderByDescending(n => n))
			{
				Assert.AreEqual(value, this.queue.Dequeue());
			}
		}
	}
}
