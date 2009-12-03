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
		#region Fields

		private PriorityQueue<int> queue;
		private Func<int, int, bool> comparer;

		#endregion Fields

		#region Init

		[TestInitialize()]
		public void InitQueue()
		{
			this.comparer = (a, b) => a > b;
			this.queue = new PriorityQueue<int>(this.comparer);
		}

		#endregion Init

		#region Enqueue/Dequeue Tests

		[TestMethod]
		public void TestRandomPushPop()
		{
			int[] values = { 0, 5, 4, 3, 8, 7, 2, 1, 0, 9, 8, 1, 5, 6 };

			foreach (int value in values)
			{
				this.queue.Enqueue(value);
			}

			// Assert that the values come off in descending order
			foreach (int expected in values.OrderByDescending(n => n))
			{
				int actual = this.queue.Dequeue();
				Assert.AreEqual(expected, actual, "Sequences are not same");
			}
		}

		[TestMethod]
		public void TestReverseSortedPushPop()
		{
			int[] values = { 9, 9, 8, 8, 7, 6, 5, 4, 3, 3, 2, 1, 0, 0 };

			foreach (int value in values)
			{
				this.queue.Enqueue(value);
			}

			// Assert that the values come off in descending order
			foreach (int expected in values.OrderByDescending(n => n))
			{
				int actual = this.queue.Dequeue();
				Assert.AreEqual(expected, actual, "Sequences are not same");
			}
		}

		[TestMethod]
		public void TestPreSortedPushPop()
		{
			int[] values = { 0, 0, 1, 2, 3, 3, 4, 4, 5, 6, 7, 8, 8, 9, 9 };

			foreach (int value in values)
			{
				this.queue.Enqueue(value);
			}

			// Assert that the values come off in descending order
			foreach (int expected in values.OrderByDescending(n => n))
			{
				int actual = this.queue.Dequeue();
				Assert.AreEqual(expected, actual, "Sequences are not same");
			}
		}

		#endregion Enqueue/Dequeue Tests

		#region Peek Tests

		[TestMethod]
		public void TestPeek()
		{
			int[] values = { 0, 5, 4, 3, 8, 7, 2, 1, 0, 9, 8, 1, 5, 6 };

			foreach (int value in values)
			{
				this.queue.Enqueue(value);
			}

			// Assert that the values come off in descending order
			while (this.queue.Count > 0)
			{
				int expected = this.queue.Peek();
				int actual = this.queue.Dequeue();
				Assert.AreEqual(actual, expected, "Sequences are not same");
			}
		}

		#endregion Peek Tests

		#region Clear Tests

		[TestMethod]
		public void TestClear()
		{
			int[] values = { 0, 5, 4, 3, 8, 7, 2, 1, 0, 9, 8, 1, 5, 6 };

			foreach (int value in values)
			{
				this.queue.Enqueue(value);
			}

			Assert.AreEqual(this.queue.Count, values.Length);

			this.queue.Clear();

			Assert.AreEqual(this.queue.Count, 0);

			try
			{
				int val = this.queue.Peek();

				Assert.Fail("Queue still contained items.");
			}
			catch (InvalidOperationException) { }

			try
			{
				int val = this.queue.Dequeue();

				Assert.Fail("Queue still contained items.");
			}
			catch (InvalidOperationException) { }
		}

		#endregion Clear Tests

		#region Enumeration Tests

		[TestMethod]
		public void TestEnumerating()
		{
			int[] values = { 0, 5, 4, 3, 8, 7, 2, 1, 0, 9, 8, 1, 5, 6 };

			foreach (int value in values)
			{
				this.queue.Enqueue(value);
			}

			Assert.IsTrue(this.queue.SequenceEqual(values.OrderByDescending(n => n)), "Sequences are not same");
		}

		#endregion Enumeration Tests
	}
}
