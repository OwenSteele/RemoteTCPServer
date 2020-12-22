using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer
{
    public static class ExceptionHandling
    {
        public static void Print(Exception ex, string additionalArgs =null)
        {
            ConsoleColor currentForeColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR!{ex.TargetSite} threw exception '{ex.GetType().ToString()}'\n" +
                $"Message: '{ex.Message}'" +
                $"{(additionalArgs == null ? "" :"\nDefined notes: "+additionalArgs)}");
            Console.ForegroundColor = currentForeColor;
        }
    }
}
