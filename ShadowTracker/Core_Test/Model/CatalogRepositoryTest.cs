using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

using IgnorantPersistence.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Shadow.Model.Test
{
	[TestClass()]
	public class CatalogRepositoryTest
	{
		#region Fields

		private CatalogRepository repos;

		#endregion Fields

		[TestInitialize()]
		public void CatalogInitialize()
		{
			const string FakeRoot = @"C:\FakeRoot\";

			Catalog[] catalogs =
			{
				new Catalog
				{
					ID = 1L,
					Path = FakeRoot,
				}
			};

			CatalogEntry[] entries =
			{
				new CatalogEntry
				{
					Attributes = FileAttributes.ReadOnly,
					CreatedDate = new DateTime(2009, 8, 21, 23, 42, 37, DateTimeKind.Local),
					ModifiedDate = new DateTime(2009, 8, 21, 23, 42, 37, DateTimeKind.Local),
					Name = "Foo.txt",
					Parent = "/",
					Signature = "0123456789ABCDEF0123456789ABCDEF01234567",
					CatalogID = 1L
				}
			};

			MemoryUnitOfWork unitOfWork = new MemoryUnitOfWork();
			unitOfWork.PopulateTable<Catalog>(Catalog.PathComparer, catalogs);
			unitOfWork.PopulateTable<CatalogEntry>(CatalogEntry.PathComparer, entries);

			this.repos = new CatalogRepository(unitOfWork);
		}

		//[TestMethod()]
		//public void AddOrUpdateTest()
		//{
		//    Assert.Inconclusive("Verify the correctness of this test method.");

		//    CatalogRepository target = this.repos;
		//    CatalogEntry entry = null; // TODO: Initialize to an appropriate value
		//    target.AddOrUpdate(entry);
		//}

		//[TestMethod()]
		//public void UpdateTest()
		//{
		//    Assert.Inconclusive("Verify the correctness of this test method.");

		//    CatalogRepository target = this.repos;
		//    CatalogEntry entry = null; // TODO: Initialize to an appropriate value
		//    CatalogEntry original = null; // TODO: Initialize to an appropriate value
		//    target.Update(entry, original);
		//}

		[TestMethod]
		public void MoveEntryTest()
		{
			Assert.IsTrue(this.repos.EntryExists(1L, "/Foo.txt"));
			Assert.IsFalse(this.repos.EntryExists(1L, "/Bar.txt"));

			this.repos.MoveEntry(1L, "/Foo.txt", "/Bar.txt");

			Assert.IsFalse(this.repos.EntryExists(1L, "/Foo.txt"));
			Assert.IsTrue(this.repos.EntryExists(1L, "/Bar.txt"));
		}

		[TestMethod]
		public void DeleteEntryByPathTest()
		{
			const string path = "/Foo.txt";

			Assert.IsTrue(this.repos.EntryExists(1L, path));

			this.repos.DeleteEntryByPath(1L, path);

			Assert.IsFalse(this.repos.EntryExists(1L, path));
		}
	}
}
