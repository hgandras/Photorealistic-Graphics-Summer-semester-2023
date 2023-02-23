using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Util;

/// <summary>
/// Class for logging/debugging
/// </summary>
internal class Logging
{
    /// <summary>
    /// Set up values for logging, will be decided later if necessary, or just create a static class
    /// Maybe warning levels can be defined here.
    /// </summary>
    public Logging()
    {

    }
    public static void InvalidArgument(string arg_name="Default")
    {
        if (arg_name == "Default")
        {
            Console.WriteLine("Key or value missing from argument!");
        }
        else
        {
            Console.WriteLine("Command line argument " + arg_name.ToUpper() + " value is invalid, using default!");
        }
    }
    
}
