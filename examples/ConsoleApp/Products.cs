using System;
using AWS.Lambda.PowerTools.Metrics;

namespace ConsoleApp
{
    public partial class Products
    {
        public string Name { get; set; }
        public int Amount { get; set; }
        
        internal void Output()
        {
            Console.WriteLine("This is products");   
            
        }
    }

    
}