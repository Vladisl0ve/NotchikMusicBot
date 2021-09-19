using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMB.DataStructs.Deserializers
{
    public class MemoryDTO
    {
        public ulong Reservable { get; set; }
        public ulong Used { get; set; }
        public ulong Free { get; set; }
        public ulong Allocated { get; set; }
    }
}
