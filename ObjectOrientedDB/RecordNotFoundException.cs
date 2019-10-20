using System;

namespace ObjectOrientedDB
{
    /// <summary>
    /// Thrown when a record cannot be found in the database because it doesn't exist or was deleted.
    /// </summary>
    public class RecordNotFoundException : Exception
    {
        public RecordNotFoundException(string message) : base(message) { }
    }
}