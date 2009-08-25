ShadowTracker

A file system change tracking service intended to keep a database up-to-date
with the latest files on disk. Built as a .NET Windows Service which monitors
a file system incrementally pumping changes to update a data store based upon
the file state.

Upon startup performs a slow "trickle-update" to check for changes since
service was last shutdown.  During operation, tracks the file system making
changes as it happens.

Uses SHA1 for determining if a file is identical to another.  Useful for when
files need to be sync'd across a network.  Having the hash allows optimization
of a local copy operation instead of a network operation.

Utilizes persistence ignorance to better integrate into any number of storage
mechanisms. Comes with a Linq-to-Sql implementation by default.  Unit tests
for file actions are performed on an in-memory backing store.

Intended to be part of a larger system.

http://msdn.microsoft.com/en-us/library/bb386907.aspx

command used for initial mapping test:
	sqlmetal /pluralize /context:CatalogDataContext /namespace:Shadow.Model /code:junk.cs /map:ShadowDB.map ShadowDB.mdf