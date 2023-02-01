using MTCG.Models;
using System;
using System.Collections.Concurrent;
using MTCG.DatabaseAccess;
using System.Numerics;
using System.Threading;

namespace MTCG.Battles
{
    public class Battle
    {
        //The "ConcurrentQueue" is a thread-safe data structure, used to store elements in a queue.
        //The "UserQueue" variable is a queue of objects of type "User".
        private static ConcurrentQueue<User> UserQueue = new ConcurrentQueue<User>();

        //used to synchronize access to a shared resource in a multithreaded environment
        //to ensure that only one thread at a time can access the resource
        private static object LockObject = new object();


        /// <summary>Initializes the Batlle, queues the users and beginns the battle </summary>
        /// <param name="user">authenticated User object, see Models/User.cs</param>
        /// <param name="e">Event arguments.</param>
       
        public static void Init(HttpSvrEventArgs e, User user)
        {
            User user1 = new User("username", "password", "id", 0, 100, "me playing", ":-)", "name", 0, 0, 0);
            User user2 = new User("username", "password", "id", 0, 100, "me playing", ":-)", "name", 0, 0, 0);


            lock (LockObject)
            {

                UserQueue.Enqueue(user);

                if (UserQueue.Count >= 2)
                {
                    UserQueue.TryDequeue(out user1);
                    UserQueue.TryDequeue(out user2);
                    if (user1 == user2)
                    {
                        e.Reply(400, "Battle against oneself not allowed. ");
                        return;
                    }
                }
            }

            if ((user1.Id != "id") && (user2.Id != "id"))
            {
                string battleLog = BeginBattle(user1, user2);
                e.Reply(200, battleLog);
                return;
            }

        }
        /// <summary>two users playing against each other </summary>
        /// <param name="user1">authenticated User object, see Models/User.cs</param>
        /// <param name="user2">authenticated User object, see Models/User.cs</param>

        public static string BeginBattle(User user1, User user2)
        {

            List<Card> deck1 = new List<Card>();
            List<Card> deck2 = new List<Card>();
            deck1 = DatabaseCard.GetDeck(user1);
            deck2 = DatabaseCard.GetDeck(user2);
            int round;
            string battleLog = "";
            int randomNumber;

            battleLog += user1.Name + " vs. " + user2.Name + ":\n";

            for (round = 1; round <= 100; round++)
            {
                if (deck1.Count() == 0)
                {
                    user1.Losses++;
                    user1.Elo-=5;
                    user2.Wins++;
                    user2.Elo+=3;
                    battleLog += "\n" + user2.Name + " wins the battle.\n";
                    DatabaseUser.EditUserStats(user1);
                    DatabaseUser.EditUserStats(user2);
                    break;
                }
                if (deck2.Count() == 0)
                {
                    user1.Wins++;
                    user1.Elo+=3;
                    user2.Losses++;
                    user2.Elo-=5;
                    battleLog += "\n" + user1.Name + " wins the battle.\n";
                    DatabaseUser.EditUserStats(user1);
                    DatabaseUser.EditUserStats(user2);
                    break;
                }

                // choose random card from decks
                Random rnd = new Random();
                int i1 = rnd.Next(deck1.Count());
                int i2 = rnd.Next(deck2.Count());

                Card card1 = deck1[i1];
                Card card2 = deck2[i2];

                // Mandatory unique feature: Godly intervention
                randomNumber = rnd.Next(0, 50);
                if (randomNumber == 0)
                {
                    battleLog += "Godly intervention: " + user1.Name + "'s card gets increased damage.\n";
                    card1.Damage = card1.Damage + card2.Damage;
                }
                else if (randomNumber == 1)
                {
                    battleLog += "   Godly intervention: " + user2.Name + "'s card gets increased damage.\n";
                    card2.Damage = card1.Damage + card2.Damage;
                }

                if (card1.Damage > card2.Damage)
                {
                    // user1 wins round
                    // transfer card
                    deck1.Add(card2);
                    deck2.RemoveAt(i2);
                    battleLog += user1.Name + " wins with "+card1.Name+" ("+card1.Damage+ ") against "+card2.Name+" ("+card2.Damage+ ").\n";
                }
                else if (card1.Damage < card2.Damage)
                {
                    // user2 wins round
                    // transfer card temporarily
                    deck1.RemoveAt(i1);
                    deck2.Add(card1);
                    battleLog += user2.Name + " wins with "+card2.Name+" ("+card2.Damage+ ") against "+card1.Name+" ("+card1.Damage+ ").\n";
                }
                else
                {
                    // draw
                    battleLog += "Draw of "+card1.Name+" ("+card1.Damage+ ") against "+card2.Name+" ("+card2.Damage+ ").\n";
                    continue;
                }

            }

            if (round >= 100)
            {
                battleLog += "\nBattle ends in a draw.\n";
                user1.Draws++;
                user2.Draws++;
                DatabaseUser.EditUserStats(user1);
                DatabaseUser.EditUserStats(user2);
            }

            return battleLog;
        }
    }
}
