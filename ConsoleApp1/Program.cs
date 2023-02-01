using System.Net;
using System.Data;
using System.Text;
using System.Threading;
using Npgsql;
using Newtonsoft.Json;


//using System.Convert;

namespace MTCG
{

    public static class Program
    {


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // main entry point                                                                                         //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Entry point.</summary>
        /// <param name="args">Command line arguments.</param>
        ///
        static void Main(string[] args)
        {
            HttpSvr svr = new();
            svr.Incoming += _Svr_Incoming;
            Console.WriteLine("Listening...");
            svr.Run();
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // event handlers                                                                                           //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Processes an incoming HTTP request.</summary>
        /// <param name="sender">Object that raised the event.</param>
        /// <param name="e">Event arguments.</param>

        public static void _Svr_Incoming(object sender, HttpSvrEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(HandleRequests._Svr_Incoming, e);
        }



    } // Program
} // namespace
