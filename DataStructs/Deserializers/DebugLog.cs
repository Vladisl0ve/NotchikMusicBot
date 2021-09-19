using System;

namespace NMB.DataStructs.Deserializers
{
    public class DebugLog
    {
        public int PlayingPlayers { get; set; }
        public string Op { get; set; }
        public MemoryDTO Memory { get; set; }
        public int Players { get; set; }
        public CpuDTO Cpu { get; set; }
        public ulong Uptime { get; set; }

        public override string ToString()
        {
            var t = new TimeSpan(0, 0, 0, 0, (int)Uptime).ToString(@"hh\:mm\:ss");
            return $"PP: {PlayingPlayers}, " + //playing players
                $"Memory: used {(Memory.Used > 1000000 ? Math.Round((double)(Memory.Used / 1000000), 3) : Memory.Used)} MB |" +
                $"free {(Memory.Free > 1000000 ? (double)(Memory.Free / 1000000) : Memory.Free)} MB, " +
                $"Players: {Players}, " +
                $"CPU: {Cpu.Cores} - {Math.Round(Cpu.SystemLoad, 3)} - {Math.Round(Cpu.LavalinkLoad, 3)}; " +
                $"Uptime: {new TimeSpan(0, 0, 0, 0, (int)Uptime).ToString(@"hh\:mm\:ss")} ";
        }
    }
}