using System.Threading.Tasks;
using NMB.Services;

namespace NMB
{
    internal class Program
    {
        public static Task Main(string[] args) => new InitialService().InitializeAsync();
    }
}