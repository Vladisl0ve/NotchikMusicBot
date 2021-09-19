using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMB.DataStructs.Deserializers
{
    public class CpuDTO
    {
        public int Cores { get; set; }
        public double SystemLoad { get; set; }
        public double LavalinkLoad { get; set; }
    }
}
