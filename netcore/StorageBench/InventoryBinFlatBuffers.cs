using System;
using System.Collections.Generic;
using FlatBuffers;
using LightningDB;

namespace SimCluster {
    public sealed class InventoryBinFlatBuffers {
        public static void BenchAdd(int n) {
            using (var env = Utils.NewEnv()) {
                var db = env.CreateDB();

                //prepare 
                var key = new byte[] {1};

                CreateBin(env, db, key);

                // Update random item in the bin
                var br = new Bin();

                for (uint i = 0; i < n; i++) {
                    using (var tx = env.BeginTransaction(Const.WriteTx)) {
                        var data = tx.Get(db, key);

                        var buf = new ByteBuffer(data);
                        var bin = Bin.GetRootAsBin(buf, br);

                        var search = i % Const.BinItemCount + Const.ProductIDOffset;


                        for (int j = 0; j < bin.ItemsLength; j++) {
                            var it = bin.Items(j).Value;
                            if (it.ItemID == search) {
                                it.MutateCount(it.Count + 1);
                                break;
                            }
                        }

                        tx.Put(db, key, data);
                        tx.Commit();
                    }
                }
            }
        }

        private static void CreateBin(LightningEnvironment env, LightningDatabase db, byte[] key) {
            var builder = new FlatBufferBuilder(1024);
            Bin.StartItemsVector(builder, Const.BinItemCount);
            for (uint i = 0; i < Const.BinItemCount; i++) {
                var itemID = i + Const.ProductIDOffset;
                BinItem.CreateBinItem(builder, itemID, 0, i + 1, ItemType.Product);
            }

            var bins = builder.EndVector();
            var code = builder.CreateString(Const.BinCode);
            var bin = Bin.CreateBin(builder, bins, ItemType.Bin, Const.Flag, code, 1, Const.SaleID);
            builder.Finish(bin.Value);

            using (var tx = env.BeginTransaction()) {
                tx.Put(db, key, builder.ToSegment());
                tx.Commit();
            }
        }


        public static void BenchAddRemove(int n) {
            using (var env = Utils.NewEnv()) {
                var db = env.CreateDB();


                //prepare 
                var key = new byte[] {1};

                CreateBin(env, db, key);
                // Remove random item from the bin.
                // if at zero - discard
                // if missing - add 10


                var bin = new Bin();
                var builder = new FlatBufferBuilder(1024);
                for (uint i = 0; i < n; i++) {
                    using (var tx = env.BeginTransaction(Const.WriteTx)) {
                        ArraySegment<byte> data = tx.Get(db, key);

                        var br = Bin.GetRootAsBin(new ByteBuffer(data.Array), bin);

                        var search = i % Const.BinItemCount + Const.ProductIDOffset;

                        var binCount = br.ItemsLength;
                        var found = false;
                        for (int j = 0; j < binCount; j++) {
                            var bi = br.Items(j).Value;
                            if (bi.ItemID == search) {
                                var count = bi.Count;
                                if (count > 1) {
                                    bi.MutateCount(count - 1);
                                    found = true;
                                    break;
                                }

                                if (count == 1) {
                                    // write new without this item
                                    // TODO: use garbage collected storage
                                    builder.Clear();
                                    Bin.StartItemsVector(builder, binCount - 1);
                                    for (int k = 0; k < binCount; k++) {
                                        var old = br.Items(k).Value;
                                        var id = old.ItemID;
                                        if (id != search) {
                                            BinItem.CreateBinItem(builder, id, old.ShipmentID, old.Count, old.Type);
                                        }
                                    }

                                    var bins = builder.EndVector();
                                    var code = builder.CreateString(br.Code);
                                    var nb = Bin.CreateBin(builder, bins, br.Type, br.Flags, code, br.Subtype,
                                        br.SaleID);
                                    builder.Finish(nb.Value);
                                    data = builder.ToSegment();
                                    found = true;
                                    break;
                                }
                            }
                        }

                        if (!found) {
                            // replenish
                            // TODO: use garbage collected storage
                            builder.Clear();
                            Bin.StartItemsVector(builder, binCount + 1);
                            for (int k = 0; k < binCount; k++) {
                                var old = br.Items(k).Value;
                                BinItem.CreateBinItem(builder, old.ItemID, old.ShipmentID, old.Count, old.Type);
                            }

                            BinItem.CreateBinItem(builder, search, i, Const.RestockCount, ItemType.Product);

                            var bins = builder.EndVector();
                            var code = builder.CreateString(br.Code);
                            var nb = Bin.CreateBin(builder, bins, br.Type, br.Flags, code, br.Subtype, br.SaleID);
                            builder.Finish(nb.Value);
                            data = builder.ToSegment();
                        }

                        tx.Put(db, key, data);
                        tx.Commit();
                    }
                }
            }
        }

        public static void BenchRead(int n) {
            using (var env = Utils.NewEnv()) {
                var db = env.CreateDB();

                //prepare 
                var key = new byte[] {1};

                CreateBin(env, db, key);
                // Update random item in the bin


                ulong counter = 0;
                var br = new Bin();
                using (var tx = env.BeginTransaction(TransactionBeginFlags.ReadOnly)) {
                    for (uint i = 0; i < n; i++) {
                        tx.Reset();
                        tx.Renew();

                        var data = tx.Get(db, key);

                        var buf = new ByteBuffer(data);
                        var bin = Bin.GetRootAsBin(buf, br);

                        var search = i % Const.BinItemCount + Const.ProductIDOffset;

                        for (int j = 0; j < bin.ItemsLength; j++) {
                            var it = bin.Items(j).Value;
                            if (it.ItemID == search) {
                                counter += it.Count;
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}