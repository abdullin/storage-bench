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

                        var search = i % Const.BinItemCount + 10000;

                        
                        for (int j = 0; j < bin.BinsLength; j++) {
                            var it = bin.Bins(j).Value;
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
            Bin.StartBinsVector(builder, Const.BinItemCount);
            for (uint i = 0; i < Const.BinItemCount; i++) {
                BinItem.CreateBinItem(builder, ItemType.Product, i+10000, i+1);
            }
            
            var bins = builder.EndVector();
            var bin = Bin.CreateBin(builder, ItemType.Bin, bins);
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
                        
                        var search = i % Const.BinItemCount + 10000;

                        var binCount = br.BinsLength;
                        var found = false;
                        for (int j = 0; j < binCount; j++) {
                            var bi = br.Bins(j).Value;
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
                                    Bin.StartBinsVector(builder, binCount - 1);
                                    for (int k = 0; k < binCount; k++) {
                                        var old = br.Bins(k).Value;
                                        var id = old.ItemID;
                                        if (id != search) {
                                            BinItem.CreateBinItem(builder, ItemType.Product, id, old.Count);
                                        }
                                    }

                                    var bins = builder.EndVector();
                                    var nb = Bin.CreateBin(builder, ItemType.Bin, bins);
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
                            Bin.StartBinsVector(builder, binCount + 1);
                            for (int k = 0; k < binCount; k++) {
                                var old = br.Bins(k).Value;
                                var id = old.ItemID;
                                BinItem.CreateBinItem(builder, ItemType.Product, id, old.Count);
                            }

                            BinItem.CreateBinItem(builder, ItemType.Product, search, Const.RestockCount);

                            var bins = builder.EndVector();
                            var nb = Bin.CreateBin(builder, ItemType.Bin, bins);
                            builder.Finish(nb.Value);
                            data = builder.ToSegment();
                            
                        }
                        
                        tx.Put(db, key, data);
                        tx.Commit();

                    
                        
                    }
                    
                }
            }
        }


        struct BinItemDto {
            public readonly ulong ItemID;
            public readonly uint Count;

            public BinItemDto(ulong itemId, uint count) {
                ItemID = itemId;
                Count = count;
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
                for (uint i = 0; i < n; i++) {
                    
                    using (var tx = env.BeginTransaction(TransactionBeginFlags.ReadOnly)) {
                        var data = tx.Get(db, key);

                        var buf = new ByteBuffer(data);
                        var bin = Bin.GetRootAsBin(buf, br);

                        var search = i % Const.BinItemCount + 10000;

                        for (int j = 0; j < bin.BinsLength; j++) {
                            var it = bin.Bins(j).Value;
                            if (it.ItemID == search) {
                                counter += it.Count;
                                break;
                            }
                        }
                    }
                }
            }
        }



        private static bool AddMutate(Bin bin, uint search, uint count) {
            for (int j = 0; j < bin.BinsLength; j++) {
                var it = bin.Bins(j).Value;
                if (it.ItemID == search) {
                    it.MutateCount(it.Count + count);
                    return true;
                }
            }

            return false;


        }
    }
}