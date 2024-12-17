using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCP_Client
{
    internal class TimeHelper
    {
        private Stopwatch _timer;

        public TimeHelper()
        {
            _timer = new Stopwatch();
        }

        public void Start() => _timer.Restart();

        public long Stop()
        {
            _timer.Stop();
            return _timer.ElapsedMilliseconds;
        }
    }
}
