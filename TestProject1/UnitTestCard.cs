using MTCG.Models;
using MTCG.DatabaseAccess;
using Moq;


namespace TestsMTCG
{
    internal class UnitTestCard
    {
        [Test]
        public void CreateNewCard()
        {
            //Act
            Card card = new Card("845f0dc7-37d0-426e-994e-43fc3ac83c08", "WaterGoblin", 10);
            
            //Assert
            Assert.That(card, Is.Not.Null);
            Assert.That(card.Id, Is.EqualTo("845f0dc7-37d0-426e-994e-43fc3ac83c08"));
            Assert.That(card.Name, Is.EqualTo("WaterGoblin"));
            Assert.That(card.Damage, Is.EqualTo(10));
        }

        [Test]
        public void Deck_DefaultConstructor_InitializesEmptyObject()
        {
            // Act
            Deck deck = new Deck();

            // Assert
            Assert.IsNull(deck.user_id);
            Assert.IsNull(deck.card_id);
        }

        /*[Test] //This test is only valid after running the curl script when the database contains 9 decks
        public void TestGetNextPackageId()
        {
            // Arrange
            int expected = 10;

            // Act
            int actual = DatabaseCard.GetPackageId();

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
        }*/

        [Test]
        public void DefineDeck_FailureEmptyString()
        {
            User user = new User("kienboec", "daniel", "YX5WOPXAhUiZDkKS4tRZAg==", 0, 100, "me playin...", ":-)", "Kienboeck", 0, 0, 0);
            var args = new Mock<HttpSvrEventArgs>("PUT /deck HTTP/1.1\r\nHost: localhost:10001\r\nUser-Agent: curl/7.83.1\r\nAccept: */*\r\nContent-Type: application/json\r\nAuthorization: Basic kienboec-mtcgToken\r\nContent-Length: 0\r\n\r\n", null);

            DatabaseCard.DefineDeck(args.Object, user);

            args.Verify(e => e.Reply(404, ("No cards received. ")));
        }



        [Test]
        public void UserGetsPackage_FailureNotEnoughMoney()
        {
            User user = new User("kienboec", "daniel", "YX5WOPXAhUiZDkKS4tRZAg==", 3, 100, "me playin...", ":-)", "Kienboeck", 0, 0, 0);
            var args = new Mock<HttpSvrEventArgs>("POST transactions/packages HTTP/1.1\r\nHost: localhost:10001\r\nUser-Agent: curl/7.83.1\r\nAccept: */*\r\nContent-Type: application/json\r\nAuthorization: Basic kienboec-mtcgToken\r\nContent-Length: 0\r\n\r\n", null);

            DatabaseCard.UserGetsPackage(user, 10, args.Object);

            args.Verify(e => e.Reply(401, ("Not enough money")));
        }


        [Test]
        public void UserGetsPackage_SuccessEnoughMoney()
        {
            User user = new User("kienboec", "daniel", "YX5WOPXAhUiZDkKS4tRZAg==", 5, 100, "me playin...", ":-)", "Kienboeck", 0, 0, 0);
            var args = new Mock<HttpSvrEventArgs>("POST transactions/packages HTTP/1.1\r\nHost: localhost:10001\r\nUser-Agent: curl/7.83.1\r\nAccept: */*\r\nContent-Type: application/json\r\nAuthorization: Basic kienboec-mtcgToken\r\nContent-Length: 0\r\n\r\n", null);

            DatabaseCard.UserGetsPackage(user, 10, args.Object);

            args.Verify(e => e.Reply(200, ("Coins: " + 0)));
        }

       [Test]
        public void DefineDeck_WrongCardSize()
        {
            User user = new User("kienboec", "daniel", "YX5WOPXAhUiZDkKS4tRZAg==", 3, 100, "me playin...", ":-)", "Kienboeck", 0, 0, 0);
            var args = new Mock<HttpSvrEventArgs>("PUT /deck HTTP/1.1\r\nHost: localhost:10001\r\nUser-Agent: curl/7.83.1\r\nAccept: */*\r\nContent-Type: application/json\r\nAuthorization: Basic altenhof-mtcgToken\r\nContent-Length: 120\r\n\r\n" +
                "[\"aa9999a0-734c-49c6-8f4a-651864b14e62\", " +
                "\"d6e9c720-9b5a-40c7-a6b2-bc34752e3463\", " +
                "\"d60e23cf-2238-4d49-844f-c7589ee5342e\"]", null);

            args.SetupGet(m => m.Payload_stripped).Returns("[\"aa9999a0-734c-49c6-8f4a-651864b14e62\", " +
                "\"d6e9c720-9b5a-40c7-a6b2-bc34752e3463\", " +
                "\"d60e23cf-2238-4d49-844f-c7589ee5342e\"]");

            DatabaseCard.DefineDeck(args.Object, user);

            args.Verify(e => e.Reply(404, ("The deck must consist of exactly 4 cards. ")));
        }

        




    }
}

