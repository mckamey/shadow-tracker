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
		#region Setup

		public PriorityQueue<int> InitQueue()
		{
			return new PriorityQueue<int>((a, b) => a > b);
		}

		#endregion Setup

		#region Enqueue/Dequeue Tests

		[TestMethod]
		public void TestRandomPushPop()
		{
			PriorityQueue<int> queue = this.InitQueue();

			int[] values = { 0, 5, 4, 3, 8, 7, 2, 1, 0, 9, 8, 1, 5, 6 };

			foreach (int value in values)
			{
				queue.Enqueue(value);
			}

			// Assert that the values come off in descending order
			foreach (int expected in values.OrderByDescending(n => n))
			{
				int actual = queue.Dequeue();
				Assert.AreEqual(expected, actual, "Sequences are not same");
			}
		}

		[TestMethod]
		public void TestReverseSortedPushPop()
		{
			PriorityQueue<int> queue = this.InitQueue();

			int[] values = { 9, 9, 8, 8, 7, 6, 5, 4, 3, 3, 2, 1, 0, 0 };

			foreach (int value in values)
			{
				queue.Enqueue(value);
			}

			// Assert that the values come off in descending order
			foreach (int expected in values.OrderByDescending(n => n))
			{
				int actual = queue.Dequeue();
				Assert.AreEqual(expected, actual, "Sequences are not same");
			}
		}

		[TestMethod]
		public void TestPreSortedPushPop()
		{
			PriorityQueue<int> queue = this.InitQueue();

			int[] values = { 0, 0, 1, 2, 3, 3, 4, 4, 5, 6, 7, 8, 8, 9, 9 };

			foreach (int value in values)
			{
				queue.Enqueue(value);
			}

			// Assert that the values come off in descending order
			foreach (int expected in values.OrderByDescending(n => n))
			{
				int actual = queue.Dequeue();
				Assert.AreEqual(expected, actual, "Sequences are not same");
			}
		}

		#endregion Enqueue/Dequeue Tests

		#region Peek Tests

		[TestMethod]
		public void TestPeek()
		{
			PriorityQueue<int> queue = this.InitQueue();

			int[] values = { 0, 5, 4, 3, 8, 7, 2, 1, 0, 9, 8, 1, 5, 6 };

			foreach (int value in values)
			{
				queue.Enqueue(value);
			}

			// Assert that the values come off in descending order
			while (queue.Count > 0)
			{
				int expected = queue.Peek();
				int actual = queue.Dequeue();
				Assert.AreEqual(actual, expected, "Sequences are not same");
			}
		}

		#endregion Peek Tests

		#region Clear Tests

		[TestMethod]
		public void TestClear()
		{
			PriorityQueue<int> queue = this.InitQueue();

			int[] values = { 0, 5, 4, 3, 8, 7, 2, 1, 0, 9, 8, 1, 5, 6 };

			foreach (int value in values)
			{
				queue.Enqueue(value);
			}

			Assert.AreEqual(queue.Count, values.Length);

			queue.Clear();

			Assert.AreEqual(queue.Count, 0);

			try
			{
				int val = queue.Peek();

				Assert.Fail("Queue still contained items.");
			}
			catch (InvalidOperationException) { }

			try
			{
				int val = queue.Dequeue();

				Assert.Fail("Queue still contained items.");
			}
			catch (InvalidOperationException) { }
		}

		#endregion Clear Tests

		#region Enumeration Tests

		[TestMethod]
		public void TestEnumerating()
		{
			PriorityQueue<int> queue = this.InitQueue();

			int[] values = { 0, 5, 4, 3, 8, 7, 2, 1, 0, 9, 8, 1, 5, 6 };

			foreach (int value in values)
			{
				queue.Enqueue(value);
			}

			Assert.IsTrue(queue.SequenceEqual(values.OrderByDescending(n => n)), "Sequences are not same");
		}

		[TestMethod]
		public void TestCopyTo()
		{
			PriorityQueue<int> queue = this.InitQueue();

			int[] values = { 0, 5, 4, 3, 8, 7, 2, 1, 0, 9, 8, 1, 5, 6 };

			foreach (int value in values)
			{
				queue.Enqueue(value);
			}

			int[] actual = new int[queue.Count];
			queue.CopyTo(actual, 0);

			Assert.IsTrue(actual.SequenceEqual(values.OrderByDescending(n => n)), "Sequences are not same");
		}

		#endregion Enumeration Tests
	}
}
