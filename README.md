# High-Performance UDP Socket IO

This repository contains an example of how to do really efficient UDP socket IO
in .NET5, that can handle both individual very high bandwidth sockets, plus 
handle a very large number of concurrent sockets through the use of IO Completion Ports.

Some of the concepts used in this example includes:

- `Memory<byte>` for buffers.
- `ValueTask`, and `IValueTaskSource` for custom completion of ValueTasks.
- Use of the POH (Pinned Object Heap) introduced in .NET5.
- Publishing a trimmed self-contained binary (`dotnet publish Enclave.UdpPerf.Test -c Release -r <rid>`)
- Object Pools using Microsoft.Extensions.ObjectPool. 

## Running the Example

If you run the 'Enclave.UdpPerf.Test' binary with no arguments, it starts listening for data on 0.0.0.0:9999.

If you provide the `-s <ip>` argument, it goes into client mode, and starts sending data to the specified address, on port 9999.

You should get throughput indicators on both sides, with MB per sec, Mb per sec and packets per sec.

These results are over a local gigabit ethernet link, with a packet size of 65535 bytes:

### Client
```
> ./Enclave.UdpPerf.Test.exe -s <ip>
Sending to <ip>:9999
112.87MBps (903.00Mbps) - 1811.00pps
113.56MBps (908.48Mbps) - 1822.00pps
113.75MBps (909.98Mbps) - 1825.00pps
112.19MBps (897.51Mbps) - 1800.00pps
113.87MBps (910.98Mbps) - 1827.00pps
113.50MBps (907.99Mbps) - 1821.00pps
113.75MBps (909.98Mbps) - 1825.00pps
113.93MBps (911.48Mbps) - 1828.00pps
113.75MBps (909.98Mbps) - 1825.00pps
113.81MBps (910.48Mbps) - 1826.00pps
113.87MBps (910.98Mbps) - 1827.00pps
113.62MBps (908.98Mbps) - 1823.00pps
113.81MBps (910.48Mbps) - 1826.00pps
```

### Server
```
> ./Enclave.UdpPerf.Test.exe
Listening on 0.0.0.0:9999
0.00MBps (0.00Mbps) - 0.00pps
0.00MBps (0.00Mbps) - 0.00pps
0.00MBps (0.00Mbps) - 0.00pps
0.00MBps (0.00Mbps) - 0.00pps
70.87MBps (566.93Mbps) - 1137.00pps
114.00MBps (911.97Mbps) - 1829.00pps
112.38MBps (899.01Mbps) - 1803.00pps
111.88MBps (895.02Mbps) - 1795.00pps
113.69MBps (909.48Mbps) - 1824.00pps
112.31MBps (898.51Mbps) - 1802.00pps
112.44MBps (899.51Mbps) - 1804.00pps
113.87MBps (910.98Mbps) - 1827.00pps
112.25MBps (898.01Mbps) - 1801.00pps
113.81MBps (910.48Mbps) - 1826.00pps
113.93MBps (911.48Mbps) - 1828.00pps
112.25MBps (898.01Mbps) - 1801.00pps
113.69MBps (909.48Mbps) - 1824.00pps
```