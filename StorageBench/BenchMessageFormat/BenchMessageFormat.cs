using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using ProtoBuf;

namespace SimCluster.BenchMessageFormat {
    
    
    public sealed class ProtoBufDeserialize  {

        public string Name => "PB deserialize";

        public void Run(int n) {
            
            
            Serializer.PrepareSerializer<RecordCreated>();
            
            
            var list = new List<NestedObject>();

            for (int i = 0; i < 15; i++) {
                list.Add(new NestedObject(i * 1024, "Some random value" + i, i, i * 1075));
            }
            var obj = new RecordCreated(7321, "Some random string", list);


            byte[] array;
            using (var mem = new MemoryStream()) {
                Serializer.Serialize(mem, obj);
                array = mem.ToArray();
            }

            using (var mem = new MemoryStream(array)) {
                for (int i = 0; i < n; i++) {
                    mem.Seek(0, SeekOrigin.Begin);
                    Serializer.Deserialize<RecordCreated>(mem);
                }
            }
        }
        
        
    }
    
    
    [DataContract]
    public class RecordCreated {
        [DataMember(Order = 1)]
        public long Id { get; set; }
        
        [DataMember(Order = 2)]
        public string Str { get; set; }
        [DataMember(Order = 3)]
        public IList<NestedObject> List { get; set; }


        private RecordCreated() { }

        public RecordCreated(long id, string str, IList<NestedObject> list) {
            Id = id;
            Str = str;
            List = list;
        }
    }

    [DataContract]
    public class NestedObject {
        [DataMember(Order = 1)]
        public long Id { get; set; }
        [DataMember(Order = 2)]
        public string Str { get; set; }
        [DataMember(Order = 3)]
        public decimal Money { get; set; }
        [DataMember(Order = 4)]
        public int Quantity { get; set; }

        public NestedObject() { }

        public NestedObject(long id, string str, decimal money, int quantity) {
            Id = id;
            Str = str;
            Money = money;
            Quantity = quantity;
        }
    }
}