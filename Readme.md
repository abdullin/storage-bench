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

# Scalability Targets

The goal of this research is to find a balance between performance and
development convenience while targeting the following scenarios:

* fast replay of 100M - 10B events to rebuild views or run reports
* LMDB environments up to 200M entities (B-Tree depth 30-35) and total
  size up to 200GB (anything large could be partitioned)
* Write throughput (read/update/save) of 10k tx/sec

#  Results

Machine:
```
MacBook Pro Retina, 13-inch, Early 2015
Processor 2,7 GHz Intel Core i5
Memory 8 GB 1867 MHz DDR3
macOS Sierra 10.12.6
```

## .NET Core

```
InventoryBinProtoBuf/BenchAdd: 5953 op/sec / 0.1679ms
InventoryBinFlatBuffers/BenchAdd: 6438 op/sec / 0.1553ms
InventoryBinProtoBuf/BenchAddRemove: 6359 op/sec / 0.1572ms
InventoryBinFlatBuffers/BenchAddRemove: 6472 op/sec / 0.1545ms
InventoryBinProtoBuf/BenchRead: 290276 op/sec / 0.0034ms
InventoryBinFlatBuffers/BenchRead: 712588 op/sec / 0.0014ms
```


# Conclusions

So it looks like:

* FlatBuffers indeed offer significant performance boost on partial
  reads
* Writing performance is comparable
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
