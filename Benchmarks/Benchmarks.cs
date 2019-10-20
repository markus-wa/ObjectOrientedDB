using FlatBuffers;
using Microsoft.Xunit.Performance;
using Microsoft.Xunit.Performance.Api;
using ObjectOrientedDB;
using ObjectOrientedDB.FileStorage;
using ObjectOrientedDB.FlatBuffers;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using Testdata;
using System.Runtime.CompilerServices;

namespace Benchmarks
{
    public class Benchmarks
    {
        const long SIZE_15GB = 15L * 1024 * 1024 * 1024;

        public static List<IFlatbufferObject> InputData = new List<IFlatbufferObject>();

        public static void Main(string[] args)
        {
            using (XunitPerformanceHarness p = new XunitPerformanceHarness(args))
            {
                string entryAssemblyPath = Assembly.GetEntryAssembly().Location;
                p.RunBenchmarks(entryAssemblyPath);
            }
        }

        static Benchmarks()
        {
            {
                var builder = new FlatBufferBuilder(8);
                var offset = Data_8b.CreateData_8b(builder);
                builder.Finish(offset.Value);
                InputData.Add(Data_8b.GetRootAsData_8b(builder.DataBuffer));
            }

            {
                var builder = new FlatBufferBuilder(128);

                List<Offset<Data_8b>> inner8b = new List<Offset<Data_8b>>();
                for (var i = 0; i < 16; i++)
                {
                    inner8b.Add(Data_8b.CreateData_8b(builder));
                }

                var innerOffset = Data_128b.CreateInnerVector(builder, inner8b.ToArray());
                var offset = Data_128b.CreateData_128b(builder, innerOffset);
                builder.Finish(offset.Value);
                InputData.Add(Data_128b.GetRootAsData_128b(builder.DataBuffer));
            }

            {
                var builder = new FlatBufferBuilder(128);

                List<Offset<Data_128b>> inner128b = new List<Offset<Data_128b>>();
                for (var i = 0; i < 8; i++)
                {
                    List<Offset<Data_8b>> inner8b = new List<Offset<Data_8b>>();
                    for (var j = 0; j < 16; j++)
                    {
                        inner8b.Add(Data_8b.CreateData_8b(builder));
                    }
                    var inner8bOffset = Data_128b.CreateInnerVector(builder, inner8b.ToArray());
                    inner128b.Add(Data_128b.CreateData_128b(builder, inner8bOffset));
                }

                var innerOffset = Data_1KB.CreateInnerVector(builder, inner128b.ToArray());
                var offset = Data_1KB.CreateData_1KB(builder, innerOffset);
                builder.Finish(offset.Value);
                InputData.Add(Data_1KB.GetRootAsData_1KB(builder.DataBuffer));
            }

            {
                var builder = new FlatBufferBuilder(128);

                List<Offset<Data_1KB>> inner1kb = new List<Offset<Data_1KB>>();
                for (var i = 0; i < 1024; i++)
                {
                    List<Offset<Data_128b>> inner128b = new List<Offset<Data_128b>>();
                    for (var j = 0; j < 8; j++)
                    {
                        List<Offset<Data_8b>> inner8b = new List<Offset<Data_8b>>();
                        for (var k = 0; k < 16; k++)
                        {
                            inner8b.Add(Data_8b.CreateData_8b(builder));
                        }
                        var inner8bOffset = Data_128b.CreateInnerVector(builder, inner8b.ToArray());
                        inner128b.Add(Data_128b.CreateData_128b(builder, inner8bOffset));
                    }
                    var inner128bOffset = Data_1KB.CreateInnerVector(builder, inner128b.ToArray());
                    inner1kb.Add(Data_1KB.CreateData_1KB(builder, inner128bOffset));
                }

                var innerOffset = Data_1MB.CreateInnerVector(builder, inner1kb.ToArray());
                var offset = Data_1MB.CreateData_1MB(builder, innerOffset);
                builder.Finish(offset.Value);
                InputData.Add(Data_1MB.GetRootAsData_1MB(builder.DataBuffer));
            }
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Insert_100_At_0(int dataIndex)
        {
            Run(Insert, dataIndex, 0);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Insert_100_At_10_000(int dataIndex)
        {
            Run(Insert, dataIndex, 10_000);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void Insert_100_At_100_000(int dataIndex)
        {
            Run(Insert, dataIndex, 100_000);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(2)]
        public void Insert_100_At_1_000_000(int dataIndex)
        {
            Run(Insert, dataIndex, 1_000_000);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Update_100_At_0(int dataIndex)
        {
            Run(Update, dataIndex, 0);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Update_100_At_10_000(int dataIndex)
        {
            Run(Update, dataIndex, 10_000);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void Update_100_At_100_000(int dataIndex)
        {
            Run(Update, dataIndex, 100_000);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(2)]
        public void Update_100_At_1_000_000(int dataIndex)
        {
            Run(Update, dataIndex, 1_000_000);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Delete_100_At_0(int dataIndex)
        {
            Run(Delete, dataIndex, 0);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Delete_100_At_10_000(int dataIndex)
        {
            Run(Delete, dataIndex, 10_000);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void Delete_100_At_100_000(int dataIndex)
        {
            Run(Delete, dataIndex, 100_000);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(2)]
        public void Delete_100_At_1_000_000(int dataIndex)
        {
            Run(Delete, dataIndex, 1_000_000);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(0)]
        public void Read_100_8b_At_0(int dataIndex)
        {
            Run(Read<Data_8b>, dataIndex, 0);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(1)]
        public void Read_100_128b_At_0(int dataIndex)
        {
            Run(Read<Data_128b>, dataIndex, 0);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(2)]
        public void Read_100_1KB_At_0(int dataIndex)
        {
            Run(Read<Data_1KB>, dataIndex, 0);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(3)]
        public void Read_100_1MB_At_0(int dataIndex)
        {
            Run(Read<Data_1MB>, dataIndex, 0);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(0)]
        public void Read_100_8b_At_1_000(int dataIndex)
        {
            Run(Read<Data_8b>, dataIndex, 1_000);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(1)]
        public void Read_100_128b_At_1_000(int dataIndex)
        {
            Run(Read<Data_128b>, dataIndex, 1_000);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(2)]
        public void Read_100_1KB_At_1_000(int dataIndex)
        {
            Run(Read<Data_1KB>, dataIndex, 1_000);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(3)]
        public void Read_100_1MB_At_1_000(int dataIndex)
        {
            Run(Read<Data_1MB>, dataIndex, 1_000);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(0)]
        public void Read_100_8b_At_10_000(int dataIndex)
        {
            Run(Read<Data_8b>, dataIndex, 10_000);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(1)]
        public void Read_100_128b_At_10_000(int dataIndex)
        {
            Run(Read<Data_128b>, dataIndex, 10_000);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(2)]
        public void Read_100_1KB_At_10_000(int dataIndex)
        {
            Run(Read<Data_1KB>, dataIndex, 10_000);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(3)]
        public void Read_100_1MB_At_10_000(int dataIndex)
        {
            Run(Read<Data_1MB>, dataIndex, 10_000);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(0)]
        public void Read_100_8b_At_100_000(int dataIndex)
        {
            Run(Read<Data_8b>, dataIndex, 100_000);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(1)]
        public void Read_100_128b_At_100_000(int dataIndex)
        {
            Run(Read<Data_128b>, dataIndex, 100_000);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(2)]
        public void Read_100_1KB_At_100_000(int dataIndex)
        {
            Run(Read<Data_1KB>, dataIndex, 100_000);
        }

        [Benchmark(InnerIterationCount = 100)]
        [InlineData(2)]
        public void Read_100_1kb_At_1_000_000(int dataIndex)
        {
            Run(Read<Data_1KB>, dataIndex, 1_000_000);
        }

        private void Run(Action<BenchmarkIteration, ObjectDB<IFlatbufferObject>, IFlatbufferObject> action, int dataIndex, int dataSize)
        {
            var data = InputData[dataIndex];
            var factories = new List<Func<ByteBuffer, IFlatbufferObject>> {
                    (bb) => Data_8b.GetRootAsData_8b(bb),
                    (bb) => Data_128b.GetRootAsData_128b(bb),
                    (bb) => Data_1KB.GetRootAsData_1KB(bb),
                    (bb) => Data_1MB.GetRootAsData_1MB(bb),
                };

            var nRecords = dataSize;
            var buffer = 100_000;
            using (var db = new ObjectDB<IFlatbufferObject>(FileStorageEngineFactory.Create("benchmark", SIZE_15GB, nRecords + buffer), new FlatBufferSerializer(factories)))
            {
                // set up data
                for (int i = 0; i < nRecords; i++)
                {
                    db.Insert(data);

                    if (i % 10000 == 0 && i > 0)
                    {
                        Console.WriteLine(i);
                    }
                }

                // run benchmark
                foreach (BenchmarkIteration iter in Benchmark.Iterations)
                {
                    action(iter, db, data);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Insert(BenchmarkIteration iter, ObjectDB<IFlatbufferObject> db, IFlatbufferObject data)
        {
            using (iter.StartMeasurement())
            {
                for (var i = 0; i < Benchmark.InnerIterationCount; i++)
                {
                    db.Insert(data);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Read<T>(BenchmarkIteration iter, ObjectDB<IFlatbufferObject> db, IFlatbufferObject data) where T : IFlatbufferObject
        {
            List<Guid> ids = new List<Guid>();
            for (var i = 0; i < Benchmark.InnerIterationCount; i++)
            {
                ids.Add(db.Insert(data));
            }

            using (iter.StartMeasurement())
            {
                foreach (var id in ids)
                {
                    db.Read<T>(id);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Update(BenchmarkIteration iter, ObjectDB<IFlatbufferObject> db, IFlatbufferObject data)
        {
            List<Guid> ids = new List<Guid>();
            for (var i = 0; i < Benchmark.InnerIterationCount; i++)
            {
                ids.Add(db.Insert(data));
            }

            using (iter.StartMeasurement())
            {
                foreach (var id in ids)
                {
                    db.Update(id, data);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Delete(BenchmarkIteration iter, ObjectDB<IFlatbufferObject> db, IFlatbufferObject data)
        {
            List<Guid> ids = new List<Guid>();
            for (var i = 0; i < Benchmark.InnerIterationCount; i++)
            {
                ids.Add(db.Insert(data));
            }

            using (iter.StartMeasurement())
            {
                foreach (var id in ids)
                {
                    db.Delete(id);
                }
            }
        }

    }

}
