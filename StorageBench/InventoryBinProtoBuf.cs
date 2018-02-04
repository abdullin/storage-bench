using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using LightningDB;

namespace SimCluster {
    /// <summary>
    /// We store records in /tenant/warehouse/obj/ID -> DTO
    /// </summary>
    public sealed class InventoryBinProtoBuf  {
        public static void BenchAddRemove(int n) {
            using (var env = Utils.NewEnv()) {
                var db = env.CreateDB();
                
                
                //prepare 
                var key = new byte[] {1};
                 
                
                CreateBin(env, db, key);

                // Remove random item from the bin.
                // if at zero - discard
                // if missing - add 10
                for (uint i = 0; i < n; i++) {
                    using (var tx = env.BeginTransaction(Const.WriteTx)) {
                        var data = tx.Get(db, key);
                        var obj = Utils.Deserialize<Bin>(data);
                        var search = i % Const.BinItemCount + 10000;

                        if (!TryPickOne(obj, search)) {
                           Add(obj, search, Const.RestockCount); 
                        }
                        tx.Put(db, key, Utils.Serialize(obj));
                        tx.Commit();
                    }
                    
                }
            }
        }

        private static void CreateBin(LightningEnvironment env, LightningDatabase db, byte[] key) {
            using (var tx = env.BeginTransaction()) {
                var bin = new Bin {
                    ItemType = ItemType.Bin,
                    Items = new List<BinItem>() { }
                };
                for (ulong i = 0; i < Const.BinItemCount; i++) {
                    bin.Items.Add(new BinItem(ItemType.Product, i + 10000, (uint) (i + 1)));
                }

                tx.Put(db, key, Utils.Serialize(bin));
                tx.Commit();
            }
        }

        static bool TryPickOne(Bin bin, ulong itemID) {
            for (int i = 0; i < bin.Items.Count; i++) {
                var binItem = bin.Items[i];
                if (binItem.ItemID == itemID) {
                    if (binItem.Count > 1) {
                        bin.Items[i] = binItem.Remove(1);
                        return true;
                    }

                    if (binItem.Count == 1) {
                        bin.Items.RemoveAt(i);
                        return true;
                    }

                    return false;
                }
            }

            return false;
        }

        static void Add(Bin bin, ulong itemID, uint count) {
            for (int i = 0; i < bin.Items.Count; i++) {
                var binItem = bin.Items[i];
                if (binItem.ItemID == itemID) {
                        bin.Items[i] = binItem.Add(1);
                    return;

                }
            }
            bin.Items.Add(new BinItem(ItemType.Product, itemID, count));
        }

        public static void BenchRead(int n) {
            using (var env = Utils.NewEnv()) {
                var db = env.CreateDB();
                
                
                //prepare 
                var key = new byte[] {1};

                CreateBin(env, db, key);

                
                ulong counter = 0UL;
                for (uint i = 0; i < n; i++) {
                    using (var tx = env.BeginTransaction(TransactionBeginFlags.ReadOnly)) {
                        var data = tx.Get(db, key);
                        var obj = Utils.Deserialize<Bin>(data);
                        var search = i % Const.BinItemCount + 10000;

                        for (int j = 0; j < Const.BinItemCount; j++) {
                            var found = obj.Items[j];
                            if (found.ItemID == search) {
                                counter += found.Count;
                                break;
                            }
                        }
                    }
                }
            }
        }
        
        public static void BenchAdd(int n) {
            using (var env = Utils.NewEnv()) {
                var db = env.CreateDB();
                
                
                //prepare 
                var key = new byte[] {1};

                CreateBin(env, db, key);

                // Update random item in the bin
                for (uint i = 0; i < n; i++) {
                    using (var tx = env.BeginTransaction(Const.WriteTx)) {
                        var data = tx.Get(db, key);
                        var obj = Utils.Deserialize<Bin>(data);
                        var search = i % Const.BinItemCount + 10000;
                        Add(obj, search, 1);
                        tx.Put(db, key, Utils.Serialize(obj));
                        tx.Commit();
                    }
                    
                }
            }
        }


        [DataContract]
        public sealed class Bin {
            [DataMember(Order = 1)]
            public ItemType ItemType { get; set; }

            [DataMember(Order = 2)]
            public IList<BinItem> Items { get; set; }


            public Bin() {
                Items = new List<BinItem>();
            }
        }

        [DataContract]
        public struct BinItem {
            [DataMember(Order = 1)] public ItemType Type;
            [DataMember(Order = 2)] public ulong ItemID;
            [DataMember(Order = 3)] public uint Count;

            public BinItem(ItemType type, ulong itemId, uint count) {
                Type = type;
                ItemID = itemId;
                Count = count;
            }

            public BinItem Add(uint count) {
                return new BinItem(Type, ItemID, Count + count);
            }
            
            public BinItem Remove(uint count) {
                return new BinItem(Type, ItemID, Count - count);
            }
        }

        public enum ItemType : byte {
            Undefined,
            Bin,
            Box,
            Product,
            Kit,
            Container
        }
    }
}