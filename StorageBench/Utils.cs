using System.IO;
using LightningDB;
using ProtoBuf;

namespace SimCluster {
    public static class Utils {
        public static void ResetFolder(string path) {
            if (Directory.Exists(path)) {
                Directory.Delete(path, true);
            }

            Directory.CreateDirectory(path);
        }

        
        public static byte[] Serialize<T>(T obj) {
            using (var mem = new MemoryStream()) {
                Serializer.Serialize(mem, obj);
                return mem.ToArray();
            }
        }
        
        
        public static T Deserialize<T>(byte[] dat) {
            using (var mem = new MemoryStream(dat)) {
                return Serializer.Deserialize<T>(mem);
            }
        }

        public static LightningEnvironment NewEnv(string path = "pathtofolder") {
            if (Directory.Exists(path)) {
                Directory.Delete(path, true);
            }

            Directory.CreateDirectory(path);


            var env = new LightningEnvironment(path) {
                MaxDatabases = 1
            };


            env.Open();

            return env;
        }

        public static LightningDatabase CreateDB(this LightningEnvironment env) {
            var config = new DatabaseConfiguration {
                Flags = DatabaseOpenFlags.Create 
            };


            LightningDatabase db;

            using (var tx = env.BeginTransaction()) {
                db = tx.OpenDatabase("custom", config);
                tx.Commit();
            }

            return db;
        }
    }
}