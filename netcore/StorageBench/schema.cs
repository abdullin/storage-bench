// automatically generated by the FlatBuffers compiler, do not modify

using System;
using FlatBuffers;

public enum ItemType : byte
{
 Undefined = 0,
 Bin = 1,
 Box = 2,
 Product = 3,
 Kit = 4,
 Container = 5,
 Location = 6,
};

public enum BinFlags : byte
{
 None = 0,
 Missing = 1,
 Damaged = 2,
 Reserved = 4,
 HasSale = 8,
};

public struct BinItem : IFlatbufferObject
{
  private Struct __p;
  public ByteBuffer ByteBuffer { get { return __p.bb; } }
  public void __init(int _i, ByteBuffer _bb) { __p.bb_pos = _i; __p.bb = _bb; }
  public BinItem __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

  public ulong ItemID { get { return __p.bb.GetUlong(__p.bb_pos + 0); } }
  public void MutateItemID(ulong ItemID) { __p.bb.PutUlong(__p.bb_pos + 0, ItemID); }
  public ulong ShipmentID { get { return __p.bb.GetUlong(__p.bb_pos + 8); } }
  public void MutateShipmentID(ulong ShipmentID) { __p.bb.PutUlong(__p.bb_pos + 8, ShipmentID); }
  public uint Count { get { return __p.bb.GetUint(__p.bb_pos + 16); } }
  public void MutateCount(uint Count) { __p.bb.PutUint(__p.bb_pos + 16, Count); }
  public ItemType Type { get { return (ItemType)__p.bb.Get(__p.bb_pos + 20); } }
  public void MutateType(ItemType Type) { __p.bb.Put(__p.bb_pos + 20, (byte)Type); }

  public static Offset<BinItem> CreateBinItem(FlatBufferBuilder builder, ulong ItemID, ulong ShipmentID, uint Count, ItemType Type) {
    builder.Prep(8, 24);
    builder.Pad(3);
    builder.PutByte((byte)Type);
    builder.PutUint(Count);
    builder.PutUlong(ShipmentID);
    builder.PutUlong(ItemID);
    return new Offset<BinItem>(builder.Offset);
  }
};

public struct Bin : IFlatbufferObject
{
  private Table __p;
  public ByteBuffer ByteBuffer { get { return __p.bb; } }
  public static Bin GetRootAsBin(ByteBuffer _bb) { return GetRootAsBin(_bb, new Bin()); }
  public static Bin GetRootAsBin(ByteBuffer _bb, Bin obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public void __init(int _i, ByteBuffer _bb) { __p.bb_pos = _i; __p.bb = _bb; }
  public Bin __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

  public BinItem? Items(int j) { int o = __p.__offset(4); return o != 0 ? (BinItem?)(new BinItem()).__assign(__p.__vector(o) + j * 24, __p.bb) : null; }
  public int ItemsLength { get { int o = __p.__offset(4); return o != 0 ? __p.__vector_len(o) : 0; } }
  public ItemType Type { get { int o = __p.__offset(6); return o != 0 ? (ItemType)__p.bb.Get(o + __p.bb_pos) : ItemType.Undefined; } }
  public bool MutateType(ItemType Type) { int o = __p.__offset(6); if (o != 0) { __p.bb.Put(o + __p.bb_pos, (byte)Type); return true; } else { return false; } }
  public BinFlags Flags { get { int o = __p.__offset(8); return o != 0 ? (BinFlags)__p.bb.Get(o + __p.bb_pos) : BinFlags.None; } }
  public bool MutateFlags(BinFlags Flags) { int o = __p.__offset(8); if (o != 0) { __p.bb.Put(o + __p.bb_pos, (byte)Flags); return true; } else { return false; } }
  public string Code { get { int o = __p.__offset(10); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
  public ArraySegment<byte>? GetCodeBytes() { return __p.__vector_as_arraysegment(10); }
  public uint Subtype { get { int o = __p.__offset(12); return o != 0 ? __p.bb.GetUint(o + __p.bb_pos) : (uint)0; } }
  public bool MutateSubtype(uint Subtype) { int o = __p.__offset(12); if (o != 0) { __p.bb.PutUint(o + __p.bb_pos, Subtype); return true; } else { return false; } }
  public ulong SaleID { get { int o = __p.__offset(14); return o != 0 ? __p.bb.GetUlong(o + __p.bb_pos) : (ulong)0; } }
  public bool MutateSaleID(ulong SaleID) { int o = __p.__offset(14); if (o != 0) { __p.bb.PutUlong(o + __p.bb_pos, SaleID); return true; } else { return false; } }

  public static Offset<Bin> CreateBin(FlatBufferBuilder builder,
      VectorOffset ItemsOffset = default(VectorOffset),
      ItemType Type = ItemType.Undefined,
      BinFlags Flags = BinFlags.None,
      StringOffset CodeOffset = default(StringOffset),
      uint Subtype = 0,
      ulong SaleID = 0) {
    builder.StartObject(6);
    Bin.AddSaleID(builder, SaleID);
    Bin.AddSubtype(builder, Subtype);
    Bin.AddCode(builder, CodeOffset);
    Bin.AddItems(builder, ItemsOffset);
    Bin.AddFlags(builder, Flags);
    Bin.AddType(builder, Type);
    return Bin.EndBin(builder);
  }

  public static void StartBin(FlatBufferBuilder builder) { builder.StartObject(6); }
  public static void AddItems(FlatBufferBuilder builder, VectorOffset ItemsOffset) { builder.AddOffset(0, ItemsOffset.Value, 0); }
  public static void StartItemsVector(FlatBufferBuilder builder, int numElems) { builder.StartVector(24, numElems, 8); }
  public static void AddType(FlatBufferBuilder builder, ItemType Type) { builder.AddByte(1, (byte)Type, 0); }
  public static void AddFlags(FlatBufferBuilder builder, BinFlags Flags) { builder.AddByte(2, (byte)Flags, 0); }
  public static void AddCode(FlatBufferBuilder builder, StringOffset CodeOffset) { builder.AddOffset(3, CodeOffset.Value, 0); }
  public static void AddSubtype(FlatBufferBuilder builder, uint Subtype) { builder.AddUint(4, Subtype, 0); }
  public static void AddSaleID(FlatBufferBuilder builder, ulong SaleID) { builder.AddUlong(5, SaleID, 0); }
  public static Offset<Bin> EndBin(FlatBufferBuilder builder) {
    int o = builder.EndObject();
    return new Offset<Bin>(o);
  }
};
