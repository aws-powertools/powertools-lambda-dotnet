using System;
using AWS.Lambda.PowerTools.Metrics;

namespace ConsoleApp
{
    partial class Program
    {
        static void Main(string[] args)
        {
            Products p = new Products()
            {
                Name = "p1",
                Amount = 10
            };
            
            p.Output();
            
        }
    }
}