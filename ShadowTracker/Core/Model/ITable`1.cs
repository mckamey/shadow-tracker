using System;
using System.Collections.Generic;
using System.Linq;

namespace Shadow.Model
{
	public interface ITable<T> :
		IQueryable<T>,
		IEnumerable<T>
	{
		/// <summary>
		/// Adds an item
		/// </summary>
		/// <param name="item"></param>
		void Add(T item);

		/// <summary>
		/// Updates an item
		/// </summary>
		/// <param name="item"></param>
		void Update(T item);

		/// <summary>
		/// Deletes an item
		/// </summary>
		/// <param name="item"></param>
		void Remove(T item);

		/// <summary>
		/// Deletes a set of items
		/// </summary>
		/// <param name="match"></param>
		void RemoveWhere(Predicate<T> match);
	}
}
