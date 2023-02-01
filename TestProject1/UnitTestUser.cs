using MTCG.DatabaseAccess;
using MTCG.Models;
using System.Data;
using Moq;

namespace TestsMTCG
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }

        [Test]
        public void CreateNewUser()
        {
            User user = new User("Paula", "Anton", "123&%", 20, 100, "bla", ":-)", "me", 0, 0, 0);

            Assert.That(user, Is.Not.Null);
            Assert.That(user.Coins, Is.EqualTo(20));
            Assert.That(user.Elo, Is.EqualTo(100));
            Assert.That(user.Name, Is.EqualTo("me"));
        }


        [Test]
        public void CreateUser_WithMissingPassword()
        {
            var args = new Mock<HttpSvrEventArgs>("POST /users HTTP/1.1\r\nHost: localhost:10001\r\nUser-Agent: curl/7.83.1\r\nAccept: *\r\nContent-Type: application/json\r\nContent-Length: 35\r\n\r\n{\"Username\":\"Peter\", \"Password\":\"\"}", null);
            args.SetupGet(m => m.Method).Returns("POST");
            args.SetupGet(m => m.Path).Returns("/users");
            args.SetupGet(m => m.Payload_stripped).Returns("{\"Username\":\"Peter\", \"Password\":\"\"}");
           
            DatabaseUser.CreateUser(args.Object);

            args.Verify(e => e.Reply(400, "Bad Request."));
        }

        [Test]
        public void CreateUser_WithMissingUsername()
        {
            var args = new Mock<HttpSvrEventArgs>("POST /users HTTP/1.1\r\nHost: localhost:10001\r\nUser-Agent: curl/7.83.1\r\nAccept: */*\r\nContent-Type: application/json\r\nContent-Length: 35\r\n\r\n{\"Username\":\"\", \"Password\":\"12345\"}", null);
            //args.SetupGet(m => m.Method).Returns("POST");
            //args.SetupGet(m => m.Path).Returns("/users");
            args.SetupGet(m => m.Payload_stripped).Returns("{\"Username\":\"\", \"Password\":\"1234\"}");

            DatabaseUser.CreateUser(args.Object);

            args.Verify(e => e.Reply(400, "Bad Request."));
        }

        [Test]
        public void LoginUser_WithWrongPassword()
        {
            
            var args = new Mock<HttpSvrEventArgs>("POST /sessions HTTP/1.1\r\nHost: localhost:10001\r\nUser-Agent: curl/7.83.1\r\nAccept: */*\r\nContent-Type: application/json\r\nContent-Length: 39\r\n\r\n{\"Username\":\"kienboec\", \"Password\":\"d\"}", null);
            
            args.SetupGet(m => m.Payload_stripped).Returns("{\"Username\":\"kienboec\", \"Password\":\"d\"}");

            DatabaseUser.LoginUser(args.Object);

            args.Verify(e => e.Reply(401, "Wrong password."));
        }

        [Test]
        public void CheckIfLoggedIn_WithWrongUsername() 
        {
            var args = new Mock<HttpSvrEventArgs>("GET /users/fe!!n%lkEFJiuo^ß8hgtrte54433432/&%§§§/)=?9%($)==)$$=)%(W?54 HTTP/1.1\r\nHost: localhost:10001\r\nUser-Agent: curl/7.83.1\r\nAccept: */*\r\nAuthorization: Basic fe!!n%lkEFJiuo^ß8hgtrte54433432/&%§§§/)=?9%($)==)$$=)%(W?54-mtcgToken\r\n\r\n", null);
           
            string path_username  = "fe!!n%lkEFJiuo^ß8hgtrte54433432/&%§§§/)=?9%($)==)$$=)%(W?54";

            DatabaseUser.CheckIfLoggedIn(path_username, args.Object);

            args.Verify(e => e.Reply(404, ("User not found. ")));
        }

        [Test]
        public void CheckIfAdmin_userIsNotAdmin() 
        {
            var args = new Mock<HttpSvrEventArgs>("POST /packages HTTP/1.1\r\nHost: localhost:10001\r\nUser-Agent: curl/7.83.1\r\nAccept: */*\r\nContent-Type: application/json\r\nAuthorization: Basic admin-mtcgToken\r\nContent-Length: 0\r\n\r\n", null);
            args.SetupGet(m => m.Method).Returns("POST");
            args.SetupGet(m => m.Path).Returns("/packages");
            args.SetupGet(m => m.Token).Returns("admin-mtcgToken");

            bool checkifadmin = DatabaseUser.CheckIfAdmin(args.Object);

            Assert.IsTrue(checkifadmin);    
        }

        [Test]
        public void AuthenticateUser_GetTheRightName()
        {
            
            var args = new Mock<HttpSvrEventArgs>("GET /cards HTTP/1.1\r\nHost: localhost:10001\r\nUser-Agent: curl/7.83.1\r\nAccept: */*\r\nAuthorization: Basic kienboec-mtcgToken\r\n\r\n", null);
            args.SetupGet(m => m.Token).Returns("kienboec-mtcgToken");

            User user = DatabaseUser.AuthenticateUser(args.Object);

            Assert.That(user.Username, Is.EqualTo("kienboec"));

        }

        [Test]
        public void GetStats_withSuccessFromUser()
        {
            //User user = new User("kienboec", "daniel", "YX5WOPXAhUiZDkKS4tRZAg==", 0, 100, "me playin...", ":-)", "Kienboeck", 0, 0, 0);
            var args = new Mock<HttpSvrEventArgs>("GET /stats HTTP/1.1\r\nHost: localhost:10001\r\nUser-Agent: curl/7.83.1\r\nAccept: */*\r\nAuthorization: Basic kienboec-mtcgToken\r\n\r\n", null);
            args.SetupGet(m => m.Token).Returns("kienboec-mtcgToken");

            User user = DatabaseUser.AuthenticateUser(args.Object);

            DatabaseUser.GetStats(args.Object, user);

            args.Verify(e => e.Reply(200, ("Username: " + user.Username + ", Elo: " + user.Elo + ", Wins: " + user.Wins + ", Losses: " + user.Losses + ", Draws: " + user.Draws)));
           
        }

        [Test]
        public void GetStats_FailureEmptyToken()
        {
            User user = new User("kienboec", "daniel", "YX5WOPXAhUiZDkKS4tRZAg==", 0, 100, "me playin...", ":-)", "Kienboeck", 0, 0, 0);
            var args = new Mock<HttpSvrEventArgs>("GET /stats HTTP/1.1\r\nHost: localhost:10001\r\nUser-Agent: curl/7.83.1\r\nAccept: */*\r\n\r\n", null);

            DatabaseUser.GetStats(args.Object, user);

            args.Verify(e => e.Reply(401, ("Not authenticated. ")));
        }



    }
}