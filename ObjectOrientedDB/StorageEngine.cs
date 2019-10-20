using System;

namespace ObjectOrientedDB
{
    /// <summary>
    /// Interface for the storage engine, which takes care of indexing and data storage.
    /// </summary>
    public interface StorageEngine
    {
        /// <summary>
        /// Finds an entry and reads data from the datastore.
        /// </summary>
        /// <param name="guid">GUID of the data to read</param>
        /// <returns>Read data</returns>
        byte[] Read(Guid guid);

        /// <summary>
        /// Inserts new data into the datastore and updates the index.
        /// </summary>
        /// <param name="guid">GUID the id of the data to be used</param>
        /// <param name="data">The data to sotre</param>
        void Insert(Guid guid, byte[] data);

        /// <summary>
        /// Updates an existing record in the datastore.
        /// </summary>
        /// <param name="guid">The ID of the record to update</param>
        /// <param name="data">The new data which will replace the existing entry</param>
        void Update(Guid guid, byte[] data);

        /// <summary>
        /// Deletes an entry form the database.
        /// </summary>
        /// <param name="guid">The ID of the record to delete</param>
        void Delete(Guid guid);
    }
}