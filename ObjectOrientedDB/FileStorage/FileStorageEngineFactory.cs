using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace ObjectOrientedDB.FileStorage
{
    public class FileStorageEngineFactory
    {
        public static FileStorageEngine Create(string path, long dataBytes, long indexSize)
        {
            var indexBytes = Marshal.SizeOf(typeof(Index.Metadata)) + indexSize * Marshal.SizeOf(typeof(BSTNode));
            Directory.CreateDirectory(path);
            var indexFile = CreateFile(path + "/index", indexBytes);
            var dataFile = CreateFile(path + "/data", dataBytes);
            return new FileStorageEngine(new Index(indexFile), new Datastore(dataFile));
        }

        private static MemoryMappedFile CreateFile(string path, long size)
        {
            return MemoryMappedFile.CreateFromFile(path, FileMode.Create, path, size);
        }

        public static FileStorageEngine Open(string path)
        {
            var indexFile = OpenFile(path + "/index");
            var dataFile = OpenFile(path + "/data");
            return new FileStorageEngine(new Index(indexFile), new Datastore(dataFile));
        }

        private static MemoryMappedFile OpenFile(string path)
        {
            return MemoryMappedFile.CreateFromFile(path, FileMode.Open, path);
        }
    }
}
