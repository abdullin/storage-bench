using System.Runtime.Serialization;

namespace SimCluster.SketchLmdb {
    [DataContract]
    public class TenantInfo {
        [DataMember(Order = 1)]
        public long TenantId { get; set; }
        [DataMember(Order = 2)]
        public string TenantName { get; set; }
    }
}