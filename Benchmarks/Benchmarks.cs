using FlatBuffers;
using Microsoft.Xunit.Performance;
using Microsoft.Xunit.Performance.Api;
using ObjectOrientedDB;
using ObjectOrientedDB.FileStorage;
using ObjectOrientedDB.FlatBuffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Testdata;
using System.Runtime.CompilerServices;

namespace Benchmarks
{
    public class Benchmarks
    {
        const long SIZE_10GB = 10L * 1024 * 1024 * 1024;

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
        }

        public static IEnumerable<object[]> DataIndices()
        {
            var args = new int[] { 0, 1, 2 };
            foreach (var arg in args)
                yield return new object[] { arg };
        }

        [Benchmark(InnerIterationCount = 10)]
        [MemberData(nameof(DataIndices))]
        public void Insert_10(int dataIndex)
        {
            Run(Insert, dataIndex);
        }

        [Benchmark(InnerIterationCount = 1000)]
        [MemberData(nameof(DataIndices))]
        public void Insert_1_000(int dataIndex)
        {
            Run(Insert, dataIndex);
        }

        [Benchmark(InnerIterationCount = 100_000)]
        [MemberData(nameof(DataIndices))]
        public void Insert_100_000(int dataIndex)
        {
            Run(Insert, dataIndex);
        }

        /*
        [Benchmark(InnerIterationCount = 10_000_000)]
        [MemberData(nameof(DataIndices))]
        public void Insert_10_000_000(int dataIndex)
        {
            Run(Insert, dataIndex);
        }
        */

        private void Run(Action<ObjectDB<IFlatbufferObject>, IFlatbufferObject> action, int dataIndex)
        {
            var data = InputData[dataIndex];
            var factories = new List<Func<ByteBuffer, IFlatbufferObject>> {
                    (bb) => Data_8b.GetRootAsData_8b(bb),
                    (bb) => Data_128b.GetRootAsData_128b(bb),
                    (bb) => Data_1KB.GetRootAsData_1KB(bb),
                    (bb) => Data_1MB.GetRootAsData_1MB(bb),
                };
            foreach (BenchmarkIteration iter in Benchmark.Iterations)
            {
                using (var db = new ObjectDB<IFlatbufferObject>(FileStorageEngine.Create("benchmark", SIZE_10GB), new FlatBufferSerializer(factories)))
                using (iter.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        action(db, data);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Insert(ObjectDB<IFlatbufferObject> db, IFlatbufferObject data)
        {
            db.Insert(data);
        }

    }

}
