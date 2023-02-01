using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG
{
    public class HttpHeader
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // constructors                                                                                             //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Creates a new instance of this class.</summary>
        /// <param name="name">Header name.</param>
        /// <param name="value">Header value.</param>
        public HttpHeader(string name, string value)
        {
            Name = name;
            Value = value;
        }


        /// <summary>Creates a new instance of this class.</summary>
        /// <param name="header">Header text.</param>
        public HttpHeader(string header)
        {
            Name = Value = "";
            try
            {
                int n = header.IndexOf(':');
                Name = header.Substring(0, n).Trim();
                Value = header.Substring(n + 1).Trim();
            }
            catch (Exception) { }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public properties                                                                                        //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Gets the header name.</summary>
        public string Name
        {
            get; private set;
        }


        /// <summary>Gets the header value.</summary>
        public string Value
        {
            get; private set;
        }
    }
}
