using System;
using System.Diagnostics;
using System.IO;
using LightningDB;
using ProtoBuf;

namespace SimCluster.SketchLmdb {
    public sealed class SketchLmdbMain  {
        
        
        public void Run(int iterations) {


            
            
            using (var env = Utils.NewEnv("pathtofolder")) {
                var db = env.CreateDB();

                
                var tenantInfo = new TenantInfo() {
                    TenantId = 1,
                    TenantName = "FooBar",
                };

                var keyBytes = new byte[] {1};
                using (var tx = env.BeginTransaction()) {
                    var valBytes = Utils.Serialize(tenantInfo);
                    tx.Put(db, keyBytes, valBytes);
                    tx.Commit();
                }

                using (var tx = env.BeginTransaction(TransactionBeginFlags.ReadOnly)) {

                    for (int i = 0; i < iterations; i++) {
                        tx.Reset();
                        tx.Renew();
                        var result = tx.Get(db, keyBytes);
                        var val = Utils.Deserialize<TenantInfo>(result);
                    
                        Debug.Assert(val.TenantId == tenantInfo.TenantId);
                        Debug.Assert(val.TenantName == tenantInfo.TenantName);    
                    }
                    
                }
            }
        }
    }

    
}