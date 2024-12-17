using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCP_Client
{
    public class DataForClient
    {
        public long Time { get; set; }
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
    }
}
