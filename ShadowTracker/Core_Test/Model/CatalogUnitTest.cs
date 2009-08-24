using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Shadow.Model;

namespace Shadow.Model_Test
{
	[TestClass]
	public class CatalogUnitTest
	{
		#region Fields

		private TestContext testContextInstance;
		private CatalogRepository catalog;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public CatalogUnitTest()
		{
		}

		#endregion Init

		#region Properties

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
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

		[TestMethod]
		public void Test_DeleteEntryByPath()
		{
			Assert.IsTrue(this.catalog.Exists(n => n.Path == "Foo.txt"));

			this.catalog.DeleteEntryByPath("Foo.txt");

			Assert.IsFalse(this.catalog.Exists(n => n.Path == "Foo.txt"));
		}

		[TestMethod]
		public void Test_FastMoveByPath()
		{
			Assert.IsTrue(this.catalog.Exists(n => n.Path == "Foo.txt"));
			Assert.IsFalse(this.catalog.Exists(n => n.Path == "Bar.txt"));

			this.catalog.RenameEntry("Foo.txt", "Bar.txt");

			Assert.IsFalse(this.catalog.Exists(n => n.Path == "Foo.txt"));
			Assert.IsTrue(this.catalog.Exists(n => n.Path == "Bar.txt"));
		}
	}
}
