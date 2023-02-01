using Moq;
using MTCG.DatabaseAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TestsMTCG
{
    internal class BasicTests
    {
        [Test]
        public void TestEventArgs()
        {
            HttpSvrEventArgs e = new HttpSvrEventArgs("GET /deck HTTP/1.1\r\nHost: localhost:10001\r\nUser-Agent: curl/7.83.1\r\nAccept: */*\r\nAuthorization: Basic kienboec-mtcgToken\r\nContent-Length: 0", null);

            Assert.That(e.Method, Is.EqualTo("GET"));
            Assert.That(e.Path, Is.EqualTo("/deck"));
            Assert.That(e.Token, Is.EqualTo("kienboec-mtcgToken"));
        }


        [Test]
        public void TestInitDbReturnsOpenConnection()
        {
            var con = DatabaseUser.InitDb();
            Assert.That(con.State, Is.EqualTo(ConnectionState.Open));
        }

        [Test]
        public void TestConnectionStringValue()
        {
            var expected = "Host=localhost;Username=postgres;Password=postgres;Database=MonsterCardGame";
            var actual = DatabaseCard.connectionstring;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void TestPathUsernameValue()
        {
            //var networkStreamMock = new Mock<INetworkStream>();
            HttpSvrEventArgs e = new HttpSvrEventArgs("GET /users/kienboec HTTP/1.1\\r\\nHost: localhost:10001\\r\\n", null);

            HandleRequests._Svr_Incoming(e);

            var expected = "/users/kienboec";
            var actual = e.Path;
            //e.Reply(200, "");
            Assert.That(actual, Is.EqualTo(expected));
        }



        [Test]
        public void _Svr_Incoming_FailureEmptyString()
        {
            var args = new Mock<HttpSvrEventArgs>("GET /score HTTP/1.1\r\nHost: localhost:10001\r\nUser-Agent: curl/7.83.1\r\nAccept: *\r\n\r\n", null);
            args.SetupGet(m => m.Method).Returns("GET");
            args.SetupGet(m => m.Path).Returns("/score");

            HandleRequests._Svr_Incoming(args.Object);

            args.Verify(e => e.Reply(401, "No authentication token received. "));
        }

    }
}
