# StorageBench Overview

This is a simple synthetic benchmark project for working with
[LMDB](https://symas.com/lmdb/) and different serialization formats:
[ProtocolBuffers](https://developers.google.com/protocol-buffers/) and
[FlatBuffers](https://github.com/google/flatbuffers).

At the point of writing I've seen that ProtocolBuffers are quite good
for managing data in the long-term storage (like events in the commit
log), but have some inherent performance issues. When reading a
ProtoBuf message we use CPU and allocate memory for the entire message
even if only a few fields are needed.

By switching event storage format from ProtoBuf to FlatBuffers on the
[real-time analytics project](https://abdullin.com/bitgn/real-time-analytics/)
I've been able to drop report generation time from 49 to 15
minutes. That time is spent on reading and aggregating 400M events
from the real-time bidding system.

However, while FlatBuffers are very convenient for data that has to be
read frequently, writing these messages or mutating them in a
destructive way isn't that nice. And if you have decided to store some
views in LMDB you have to do some updates at some point.


This is the purpose of this project - to provide some stable place to
compare different storage strategies and benchmark any changes in a
repeatable manner.

#  Results

Current results on my MacBook Pro (Early 2015, 2,7 GHz Intel Core i5)
for .NET Core:

```
InventoryBinProtoBuf/BenchAdd: 6299 op/sec / 0.1587ms
InventoryBinFlatBuffers/BenchAdd: 6873 op/sec / 0.1454ms
InventoryBinProtoBuf/BenchAddRemove: 6344 op/sec / 0.1576ms
InventoryBinFlatBuffers/BenchAddRemove: 6975 op/sec / 0.1433ms
InventoryBinProtoBuf/BenchRead: 306674 op/sec / 0.0032ms
InventoryBinFlatBuffers/BenchRead: 742263 op/sec / 0.0013ms
```

So it looks like:

* FlatBuffers offer significant performance boost on partial reads
* Writing performance is equivalent
* FlatBuffers take more effort to write an object down or mutate

Note, that LMDB library could potentially benefit from Span/Memory

# Licenses

This project includes source code of:

* [CoreyKaylor/Lightning.NET](https://github.com/CoreyKaylor/Lightning.NET) -
  MIT License
* .NET portion of
  [Google FlatBuffers](https://github.com/google/flatbuffers) - Apache
  2.0 License

I needed a way to quickly tweak these projects and including them as
source code was the logical way to do so.
