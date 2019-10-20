using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace ObjectOrientedDB.FileStorage
{
    /// <summary>
    /// <para>Factory class for FileStorageEngine.</para>
    /// <para>Used to create (and open existing) databses as FileStorageEngine instances.</para>
    /// </summary>
    public class FileStorageEngineFactory
    {
        public static FileStorageEngine Create(string path, long dataBytes, long indexSize)
        {
            var indexBytes = Marshal.SizeOf(typeof(Index.Metadata)) + indexSize * Marshal.SizeOf(typeof(IndexEntry));
            Directory.CreateDirectory(path);
            var indexFile = CreateFile(path + "/index", indexBytes);
            var dataFile = CreateFile(path + "/data", dataBytes);
            return NewInstance(indexFile, dataFile);
        }

        private static MemoryMappedFile CreateFile(string path, long size)
        {
            return MemoryMappedFile.CreateFromFile(path, FileMode.Create, path, size);
        }

        public static FileStorageEngine Open(string path)
        {
            var indexFile = OpenFile(path + "/index");
            var dataFile = OpenFile(path + "/data");
            return NewInstance(indexFile, dataFile);
        }

        private static MemoryMappedFile OpenFile(string path)
        {
            return MemoryMappedFile.CreateFromFile(path, FileMode.Open, path);
        }

        internal static FileStorageEngine NewInstance(MemoryMappedFile indexFile, MemoryMappedFile dataFile)
        {
            return new FileStorageEngine(new Index(indexFile), new Datastore(dataFile));
        }
    }
}
