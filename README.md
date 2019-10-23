# ObjectOrientedDB

A library for creating simple, object-oriented database instances in C#.

[![DocFx](https://img.shields.io/badge/DocFx-reference-blue)](https://markus-wa.github.io/ObjectOrientedDB/index.html)
[![Build Status](https://travis-ci.com/markus-wa/ObjectOrientedDB.svg?branch=master)](https://travis-ci.com/markus-wa/ObjectOrientedDB)
[![License](https://img.shields.io/badge/license-MIT-blue.svg?style=flat)](LICENSE.md)

## Disclaimer
This was developed as part of a graduation project at [TEKO](https://www.teko.ch/de) and is intended for academic purposes only.

Do not use this for any production software.

## Quick Start Notes:
1. Install the latest version of the library from NuGet: https://www.nuget.org/packages/ObjectOrientedDB/
2. Create or open a database
3. Insert, read, update or delete data

## Example

```c#
using ObjectOrientedDB;
using ObjectOrientedDB.FileStorage;
using System;

namespace Example
{
    class Program
    {
        const long SIZE_1G = 1L * 1024 * 1024 * 1024;
        const long MAX_ENTRIES = 1_000_000;

        static void Main(string[] args)
        {
            Guid guid;

            // create a database
            var pathToDataDir = "db-data";
            var fileStorageEngine = FileStorageEngineFactory.Create(pathToDataDir, SIZE_1G, MAX_ENTRIES);
            using (var db = new ObjectDB<object>(fileStorageEngine, new BinaryFormatterSerializer()))
            {
                // create
                var data = new MyData(1, "hello world");
                Console.WriteLine("inserting object: " + data);
                guid = db.Insert(data);
                Console.WriteLine("GUID of inserted object={0}", guid);

                // read
                data = db.Read<MyData>(guid);
                Console.WriteLine("read object: {0}", data);

                // update
                data.Text = "very interesting text";
                Console.WriteLine("updating object: {0}", data);
                db.Update(guid, data);
            }

            // re-open existing databse
            using (var db = new ObjectDB<object>(FileStorageEngineFactory.Open(pathToDataDir), new BinaryFormatterSerializer()))
            {
                // read after re-opening the db
                var data = db.Read<MyData>(guid);
                Console.WriteLine("object after re-opening the DB: {0}", data);

                // delete
                Console.WriteLine("deleting object");
                db.Delete(guid);

                // RecordNotFoundException after deletion
                try
                {
                    db.Read<MyData>(guid);
                }
                catch (RecordNotFoundException)
                {
                    Console.WriteLine("object was deleted");
                }
            }
        }
    }

    [Serializable]
    internal class MyData
    {
        public int Id { get; set; }
        public string Text { get; set; }

        public MyData(int Id, string Text)
        {
            this.Id = Id;
            this.Text = Text;
        }

        override public string ToString()
        {

            return String.Format("Data[id={0}, text='{1}']", Id, Text);
        }
    }
}
```
