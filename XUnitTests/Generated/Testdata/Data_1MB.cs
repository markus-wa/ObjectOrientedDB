// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

namespace Testdata
{

    using global::System;
    using global::FlatBuffers;

    public struct Data_1MB : IFlatbufferObject
    {
        private Table __p;
        public ByteBuffer ByteBuffer { get { return __p.bb; } }
        public static Data_1MB GetRootAsData_1MB(ByteBuffer _bb) { return GetRootAsData_1MB(_bb, new Data_1MB()); }
        public static Data_1MB GetRootAsData_1MB(ByteBuffer _bb, Data_1MB obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
        public void __init(int _i, ByteBuffer _bb) { __p.bb_pos = _i; __p.bb = _bb; }
        public Data_1MB __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

        public Data_1KB? Inner(int j) { int o = __p.__offset(4); return o != 0 ? (Data_1KB?)(new Data_1KB()).__assign(__p.__indirect(__p.__vector(o) + j * 4), __p.bb) : null; }
        public int InnerLength { get { int o = __p.__offset(4); return o != 0 ? __p.__vector_len(o) : 0; } }

        public static Offset<Data_1MB> CreateData_1MB(FlatBufferBuilder builder,
            VectorOffset innerOffset = default(VectorOffset))
        {
            builder.StartObject(1);
            Data_1MB.AddInner(builder, innerOffset);
            return Data_1MB.EndData_1MB(builder);
        }

        public static void StartData_1MB(FlatBufferBuilder builder) { builder.StartObject(1); }
        public static void AddInner(FlatBufferBuilder builder, VectorOffset innerOffset) { builder.AddOffset(0, innerOffset.Value, 0); }
        public static VectorOffset CreateInnerVector(FlatBufferBuilder builder, Offset<Data_1KB>[] data) { builder.StartVector(4, data.Length, 4); for (int i = data.Length - 1; i >= 0; i--) builder.AddOffset(data[i].Value); return builder.EndVector(); }
        public static VectorOffset CreateInnerVectorBlock(FlatBufferBuilder builder, Offset<Data_1KB>[] data) { builder.StartVector(4, data.Length, 4); builder.Add(data); return builder.EndVector(); }
        public static void StartInnerVector(FlatBufferBuilder builder, int numElems) { builder.StartVector(4, numElems, 4); }
        public static Offset<Data_1MB> EndData_1MB(FlatBufferBuilder builder)
        {
            int o = builder.EndObject();
            return new Offset<Data_1MB>(o);
        }
        public static void FinishData_1MBBuffer(FlatBufferBuilder builder, Offset<Data_1MB> offset) { builder.Finish(offset.Value); }
        public static void FinishSizePrefixedData_1MBBuffer(FlatBufferBuilder builder, Offset<Data_1MB> offset) { builder.FinishSizePrefixed(offset.Value); }
    };


}
