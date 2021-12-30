using System;
using System.Threading;

namespace Ssh.Net.Instrumentation.ServerSideTestApp
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("Test Application Start");
            for (int i = 0; i < 10; ++i)
            {
                Console.WriteLine($"Loop #{ i }");
                Thread.Sleep(500);
            }
            Console.WriteLine("Test Application Finish");
            return 42;
        }
    }
}