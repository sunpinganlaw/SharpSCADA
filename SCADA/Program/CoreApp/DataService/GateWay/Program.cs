using BatchCoreService;
using System;

namespace GateWay
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("GateWay Is Running");
            DAService dataService = new DAService();
            Console.ReadLine();
            //while (true)
            //{
            //    string str = Console.ReadLine();

            //    if (string.IsNullOrWhiteSpace(str))
            //    {
            //        continue;
            //    }

            //    str = str.Replace("吗", "");
            //    str = str.Replace("?", "!");
            //    str = str.Replace("？", "！");

            //    Console.WriteLine(str);
            //}
        }

      
    }
}
