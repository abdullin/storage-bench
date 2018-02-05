using System;
using LightningDB;

namespace SimCluster {

    public static class Const {
        public const int BinItemCount = 5;
        public const uint RestockCount = 20;
        public const ulong ProductIDOffset = 100000;
        public const string BinCode = "RandomBinHere";
        public const ulong SaleID = 1234023;
        public const BinFlags Flag = BinFlags.HasSale;
        public const TransactionBeginFlags WriteTx = TransactionBeginFlags.NoSync;
    }
    internal class Program {
        public static void Main(string[] args) {

            TestLmdb();
            
            var benchmarks = new Action<int>[] {
                InventoryBinProtoBuf.BenchAdd,
                InventoryBinFlatBuffers.BenchAdd,
                
                InventoryBinProtoBuf.BenchAddRemove,
                InventoryBinFlatBuffers.BenchAddRemove,
                
                InventoryBinProtoBuf.BenchRead,
                InventoryBinFlatBuffers.BenchRead,
            };

            foreach (var b in benchmarks) {
                Bench.Auto(b);
            }
        }


        static void TestLmdb() {


            using (var env = Utils.NewEnv()) {
                var db = env.CreateDB();
                var key = new byte[]{1};
                var data = BitConverter.GetBytes(2018);
                using (var tx = env.BeginTransaction()) {
                    
                    tx.Put(db, key, data);
                    tx.Commit();
                }

                using (var tx = env.BeginTransaction(TransactionBeginFlags.ReadOnly)) {
                    var result = tx.Get(db, key);
                    var actual = BitConverter.ToInt32(result, 0);

                    if (actual != 2018) {
                        throw new InvalidOperationException("Rinat, you've messed up PInvoke");
                    }
                }
            }
        }
    }
}