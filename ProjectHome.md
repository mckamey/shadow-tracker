# ShadowTracker #

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
of a local copy operation instead of a network operation.  The hash allows moves
to be detected as updates rather than a delete followed by a create.

Supports tracking multiple root folders (i.e. Catalogs) for combining into a
single repository or for sparse tracking of a directory tree.  Moves between
Catalogs are detected and updated appropriately, rather than a delete followed
by a create.

Intended to be part of a larger system.  A simple DB mapping program allows to
be integrated with an existing schema.

If the DB does not exist can provision a new one.

Utilizes persistence ignorance to better integrate into any number of storage
mechanisms. Comes with a Linq-to-Sql implementation by default.  Unit tests
for file actions are performed on an in-memory backing store.