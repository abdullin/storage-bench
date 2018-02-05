using System;
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

        
        public static ArraySegment<byte> Serialize<T>(T obj, byte[] buffer = null) {

            if (null == buffer) {
                buffer = new byte[1024];
            }
            // allocate a buffer in
            using (var mem = new MemoryStream(buffer)) {
                Serializer.Serialize(mem, obj);
                return new ArraySegment<byte>(buffer, 0, (int)mem.Position);
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