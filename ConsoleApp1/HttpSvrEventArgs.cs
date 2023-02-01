using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MTCG
{

    /// <summary>This class provides event arguments for an HTTP server.</summary>
    public class HttpSvrEventArgs : EventArgs
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // private members                                                                                          //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>TCP client.</summary>
        private TcpClient _Client;



        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // constructor                                                                                             //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////


        /// <summary>Creates a new instance of this class.</summary>
        /// <param name="tcp">HTTP message received from TCP listener.</param>
        public HttpSvrEventArgs(string tcp, TcpClient client)
        {
            _Client = client;
            PlainMessage = tcp;

            //PlainMessage = tcp.Replace("\"", "\\\"").Replace("{", "\"{").Replace("}", "}\"");
            string[] lines = tcp.Replace("\r\n", "\n").Replace("\r", "\n").Split("\n");

            bool inheaders = true;
            List<HttpHeader> headers = new List<HttpHeader>();

            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0)
                {
                    string[] inq = lines[0].Split(" ");
                    Method = inq[0];
                    Path = inq[1];
                }
                else if (inheaders)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                    {
                        inheaders = false;
                    }
                    else
                    {
                        string[] inq = lines[i].Split(" ");

                        if (inq[0] == "Authorization:")
                        {
                            Token = inq[2];
                        }
                        headers.Add(new HttpHeader(lines[i]));
                    }
                }
                else
                {
                    Payload += (lines[i] + "\r\n");
                    Payload_stripped += (lines[i]);
                    //Payload = Payload.Replace("\"", "\\\"").Replace("{", "\"{").Replace("}", "}\"");
                }

                Headers = headers.ToArray();
            }
        }



        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public properties                                                                                        //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Gets the plain message as received from TCP.</summary>
        public virtual string? PlainMessage
        {
            get; private set;
        }


        /// <summary>Get the HTTP method.</summary>
        public virtual string? Method
        {
            get; protected set;
        }

        /// <summary>Get the HTTP method.</summary>
        public virtual string? Token
        {
            get; protected set;
        }



        /// <summary>Gets the URL path.</summary>
        public virtual string? Path
        {
            get; protected set;
        }


        /// <summary>Gets the HTTP headers.</summary>
        public HttpHeader[]? Headers
        {
            get; private set;
        }


        /// <summary>Gets the HTTP payload.</summary>
        public string? Payload
        {
            get; set;
        }
        /// <summary>Gets the HTTP payload.</summary>
        public virtual string? Payload_stripped
        {
            get; private set;
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public methods                                                                                           //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Returns a reply to the HTTP request.</summary>
        /// <param name="status">Status code.</param>
        /// <param name="payload">Payload.</param>
        public virtual void Reply(int status, string? payload)
        {
            string data;

            switch (status)
            {                                                                   // create response status string from code
                case 200:
                    data = "HTTP/1.1 200 Data successfully retrieved\n";
                    break;
                case 201:
                    data = "HTTP/1.1 201 Succesfully created";
                    break;
                case 400:
                    data = "HTTP/1.1 400 Bad Request\n";
                    break;
                case 401:
                    data = "HTTP/1.1 401 Access token is missing or invalid\n";
                    break;
                case 403:
                    data = "HTTP/1.1 403 Provided user is not \"admin\"\n";
                    break;
                case 404:
                    data = "HTTP/1.1 404 Not Found\n";
                    break;
                case 409:
                    data = "HTTP/1.1 409 User with same username already registered";
                    break;
                default:
                    data = "HTTP/1.1 418 I'm a Teapot\n";
                    break;
            }

            if (string.IsNullOrEmpty(payload))
            {                                                                   // set Content-Length to 0 for empty content
                data += "Content-Length: 0\n";
            }
            data += "Content-Type: text/plain\n\n";

            if (payload != null) { data += payload; }

            if (status == 401) { data+= "Access token is missing or invalid"; }

            byte[] dbuf = Encoding.ASCII.GetBytes(data);
            _Client.GetStream().Write(dbuf, 0, dbuf.Length);                    // send a response

            _Client.GetStream().Close();                                        // shut down the connection
            _Client.Dispose();
        }
    }


}
