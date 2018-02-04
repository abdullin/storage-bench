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

                var buffer = new byte[1024];
                CreateBin(env, db, key);

                // Remove random item from the bin.
                // if at zero - discard
                // if missing - add 10
                for (uint i = 0; i < n; i++) {
                    using (var tx = env.BeginTransaction(Const.WriteTx)) {
                        var data = tx.Get(db, key);
                        var obj = Utils.Deserialize<Bin>(data);
                        var search = i % Const.BinItemCount + Const.ProductIDOffset;

                        if (!TryPickOne(obj, search)) {
                           Add(obj, search, Const.RestockCount, i); 
                        }
                        tx.Put(db, key, Utils.Serialize(obj, buffer));
                        tx.Commit();
                    }
                    
                }
            }
        }

        private static void CreateBin(LightningEnvironment env, LightningDatabase db, byte[] key) {
            using (var tx = env.BeginTransaction()) {
                var binItems = new List<BinItem>() { };
                
                for (ulong i = 0; i < Const.BinItemCount; i++) {
                    binItems.Add(new BinItem(ItemType.Product, i + Const.ProductIDOffset, 0, (uint) (i + 1)));
                }
                var bin = new Bin(ItemType.Bin, binItems, Const.BinCode, 1,Const.SaleID, Const.Flag);
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

        static void Add(Bin bin, ulong itemID, uint count, ulong shipmentID) {
            for (int i = 0; i < bin.Items.Count; i++) {
                var binItem = bin.Items[i];
                if (binItem.ItemID == itemID) {
                        bin.Items[i] = binItem.Add(1);
                    return;

                }
            }
            bin.Items.Add(new BinItem(ItemType.Product, itemID, shipmentID, count));
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
                        var search = i % Const.BinItemCount + Const.ProductIDOffset;

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
                var buffer = new byte[1024];

                CreateBin(env, db, key);

                // Update random item in the bin
                for (uint i = 0; i < n; i++) {
                    using (var tx = env.BeginTransaction(Const.WriteTx)) {
                        var data = tx.Get(db, key);
                        var obj = Utils.Deserialize<Bin>(data);
                        var search = i % Const.BinItemCount + Const.ProductIDOffset;
                        Add(obj, search, 1, 0);
                        tx.Put(db, key, Utils.Serialize(obj, buffer));
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
            [DataMember(Order = 3)]
            public string Code { get; set; }
            [DataMember(Order = 4)]
            public uint SubType { get; set; }
            [DataMember(Order = 5)]
            public ulong SaleID { get; set; }
            [DataMember(Order = 6)]
            public BinFlags Flags { get; set; }

            public Bin(ItemType itemType, IList<BinItem> items, string code, uint subType, ulong saleId, BinFlags flags) {
                ItemType = itemType;
                Items = items;
                Code = code;
                SubType = subType;
                SaleID = saleId;
                Flags = flags;
            }


            Bin() {
                Items = new List<BinItem>();
            }
        }

        [DataContract]
        public struct BinItem {
            [DataMember(Order = 1)] public ItemType Type;
            [DataMember(Order = 2)] public ulong ItemID;
            [DataMember(Order = 3)] public ulong ShipmentID;
            [DataMember(Order = 4)] public uint Count;

            public BinItem(ItemType type, ulong itemId, ulong shipmentId, uint count) {
                Type = type;
                ItemID = itemId;
                ShipmentID = shipmentId;
                Count = count;
            }

            public BinItem Add(uint count) {
                return new BinItem(Type, ItemID, ShipmentID, Count + count);
            }
            
            public BinItem Remove(uint count) {
                return new BinItem(Type, ItemID, ShipmentID, Count - count);
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