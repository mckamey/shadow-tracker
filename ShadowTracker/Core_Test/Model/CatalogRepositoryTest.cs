﻿using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Shadow.Model;

namespace Shadow.Model.Test
{
	[TestClass()]
	public class CatalogRepositoryTest
	{
		#region Fields

		private TestContext testContextInstance;
		private CatalogRepository catalog;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public CatalogRepositoryTest()
		{
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets or sets the test context which provides
		/// information about and functionality for the current test run.
		/// </summary>
		public TestContext TestContext
		{
			get{ return this.testContextInstance; }
			set { this.testContextInstance = value; }
		}

		#endregion Properties

		[TestInitialize()]
		public void CatalogInitialize()
		{
			CatalogEntry[] entries =
			{
				new CatalogEntry
				{
					Attributes = FileAttributes.ReadOnly,
					CreatedDate = new DateTime(2009, 8, 21, 23, 42, 37, DateTimeKind.Local),
					ModifiedDate = new DateTime(2009, 8, 21, 23, 42, 37, DateTimeKind.Local),
					Path = "Foo.txt",
					Signature = ""
				}
			};

			this.catalog = new CatalogRepository(entries);
		}

		[TestMethod()]
		[DeploymentItem("ShadowTracker.Core.dll")]
		public void CalcEntryDeltaTest()
		{
			Assert.Inconclusive("Verify the correctness of this test method.");

			CatalogRepository_Accessor target = new CatalogRepository_Accessor(); // TODO: Initialize to an appropriate value
			CatalogEntry entry = null; // TODO: Initialize to an appropriate value
			CatalogEntry meta = null; // TODO: Initialize to an appropriate value
			CatalogEntry metaExpected = null; // TODO: Initialize to an appropriate value
			CatalogEntry data = null; // TODO: Initialize to an appropriate value
			CatalogEntry dataExpected = null; // TODO: Initialize to an appropriate value
			DeltaAction expected = new DeltaAction(); // TODO: Initialize to an appropriate value
			DeltaAction actual;
			actual = target.CalcEntryDelta(entry, out meta, out data);
			Assert.AreEqual(metaExpected, meta);
			Assert.AreEqual(dataExpected, data);
			Assert.AreEqual(expected, actual);
		}

		[TestMethod()]
		public void CloneEntryTest()
		{
			Assert.Inconclusive("Verify the correctness of this test method.");

			CatalogRepository target = new CatalogRepository(); // TODO: Initialize to an appropriate value
			CatalogEntry entry = null; // TODO: Initialize to an appropriate value
			CatalogEntry data = null; // TODO: Initialize to an appropriate value
			target.CloneEntry(entry, data);
		}

		[TestMethod]
		public void DeleteEntryByPathTest()
		{
			Assert.IsTrue(this.catalog.Exists(n => n.Path == "Foo.txt"));

			this.catalog.DeleteEntryByPath("Foo.txt");

			Assert.IsFalse(this.catalog.Exists(n => n.Path == "Foo.txt"));
		}

		[TestMethod()]
		public void ExistsTest()
		{
			Assert.Inconclusive("Verify the correctness of this test method.");

			CatalogRepository target = new CatalogRepository(); // TODO: Initialize to an appropriate value
			Expression<Func<CatalogEntry, bool>> predicate = null; // TODO: Initialize to an appropriate value
			bool expected = false; // TODO: Initialize to an appropriate value
			bool actual;
			actual = target.Exists(predicate);
			Assert.AreEqual(expected, actual);
		}

		[TestMethod()]
		public void GetExistingPathsTest()
		{
			Assert.Inconclusive("Verify the correctness of this test method.");

			CatalogRepository target = new CatalogRepository(); // TODO: Initialize to an appropriate value
			IQueryable<string> expected = null; // TODO: Initialize to an appropriate value
			IQueryable<string> actual;
			actual = target.GetExistingPaths();
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void RenameEntryTest()
		{
			Assert.IsTrue(this.catalog.Exists(n => n.Path == "Foo.txt"));
			Assert.IsFalse(this.catalog.Exists(n => n.Path == "Bar.txt"));

			this.catalog.RenameEntry("Foo.txt", "Bar.txt");

			Assert.IsFalse(this.catalog.Exists(n => n.Path == "Foo.txt"));
			Assert.IsTrue(this.catalog.Exists(n => n.Path == "Bar.txt"));
		}

		[TestMethod()]
		public void UpdateMetaTest()
		{
			Assert.Inconclusive("Verify the correctness of this test method.");

			CatalogRepository target = new CatalogRepository(); // TODO: Initialize to an appropriate value
			CatalogEntry entry = null; // TODO: Initialize to an appropriate value
			CatalogEntry original = null; // TODO: Initialize to an appropriate value
			target.UpdateMeta(entry, original);
		}

		[TestMethod()]
		public void UpdateDataTest()
		{
			Assert.Inconclusive("Verify the correctness of this test method.");

			CatalogRepository target = new CatalogRepository(); // TODO: Initialize to an appropriate value
			CatalogEntry entry = null; // TODO: Initialize to an appropriate value
			CatalogEntry original = null; // TODO: Initialize to an appropriate value
			CatalogEntry data = null; // TODO: Initialize to an appropriate value
			target.UpdateData(entry, original, data);
		}

		[TestMethod()]
		public void SyncTest()
		{
			Assert.Inconclusive("Verify the correctness of this test method.");

			CatalogRepository target = new CatalogRepository(); // TODO: Initialize to an appropriate value
			CatalogRepository that = null; // TODO: Initialize to an appropriate value
			target.Sync(that);
		}

		[TestMethod()]
		[DeploymentItem("ShadowTracker.Core.dll")]
		public void FindMatchTest()
		{
			Assert.Inconclusive("Verify the correctness of this test method.");

			CatalogRepository_Accessor target = new CatalogRepository_Accessor(); // TODO: Initialize to an appropriate value
			CatalogEntry target1 = null; // TODO: Initialize to an appropriate value
			CatalogEntry meta = null; // TODO: Initialize to an appropriate value
			CatalogEntry metaExpected = null; // TODO: Initialize to an appropriate value
			CatalogEntry data = null; // TODO: Initialize to an appropriate value
			CatalogEntry dataExpected = null; // TODO: Initialize to an appropriate value
			CatalogRepository_Accessor.MatchRank expected = null; // TODO: Initialize to an appropriate value
			CatalogRepository_Accessor.MatchRank actual;
			actual = target.FindMatch(target1, out meta, out data);
			Assert.AreEqual(metaExpected, meta);
			Assert.AreEqual(dataExpected, data);
			Assert.AreEqual(expected, actual);
		}

		[TestMethod()]
		public void ApplyChangesTest()
		{
			Assert.Inconclusive("Verify the correctness of this test method.");

			CatalogRepository target = new CatalogRepository(); // TODO: Initialize to an appropriate value
			CatalogEntry entry = null; // TODO: Initialize to an appropriate value
			target.ApplyChanges(entry);
		}

		[TestMethod()]
		public void AddEntryTest()
		{
			Assert.Inconclusive("Verify the correctness of this test method.");

			CatalogRepository target = new CatalogRepository(); // TODO: Initialize to an appropriate value
			CatalogEntry entry = null; // TODO: Initialize to an appropriate value
			target.AddEntry(entry);
		}
	}
}