using System.Linq;
using System.Threading;

namespace Enclave.UdpPerf.Test
{
    public class ThroughputCounter
    {
        private ThreadLocal<long> _deltaCount = new ThreadLocal<long>(trackAllValues: true);

        public void Add(long value)
        {
            _deltaCount.Value += value;
        }

        public long SampleAndReset()
        {
            var original = _deltaCount;

            _deltaCount = new ThreadLocal<long>(trackAllValues: true);

            return original.Values.Sum();
        }
    }
}
