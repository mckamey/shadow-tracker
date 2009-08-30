using System;
using System.IO;

namespace Shadow.Model
{
	public interface IUnitOfWork
	{
		void SubmitChanges();

		ITable<CatalogEntry> GetEntries();

		void SetDiagnosticsLog(TextWriter writer);
	}
}
