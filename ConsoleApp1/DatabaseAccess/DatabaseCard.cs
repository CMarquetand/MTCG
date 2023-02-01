using MTCG.Models;
using Newtonsoft.Json;
using Npgsql;


namespace MTCG.DatabaseAccess
{
    
    public class DatabaseCard
    {
        
        public static readonly string connectionstring = "Host=localhost;Username=postgres;Password=postgres;Database=MonsterCardGame";

        public static NpgsqlConnection InitDb()
        {
            NpgsqlConnection con = new NpgsqlConnection(connectionstring);
            con.Open();
            return con;
        }

        /// <summary> returns int for package_id that is 1 higher than the last one (or higher than zero of not found)</summary>
        public static int GetPackageId()
        {
            using (NpgsqlConnection con = InitDb())
            {
                int package_id = 0;
               
                string sql = "Select package_id From card ORDER BY package_id DESC LIMIT 1";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, con))
                {
                    NpgsqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        package_id = reader.GetInt32(0);
                    }

                    package_id++; // new value for package_id is higher than the last one (or higher than zero of not found)

                    command.Dispose();
                }
                con.Close();
                return package_id;
            }
        }

        /// <summary>checks if one of the new cards is already existing in table card, returns true if card is new</summary>
        /// <param name="e">Event arguments.</param>
        public static bool CheckIfCardAlreadyExists(HttpSvrEventArgs e)
        {
            bool tobecontinued = false;
            if (!string.IsNullOrEmpty(e.Payload_stripped))
            {
                var package = JsonConvert.DeserializeObject<List<Card>>(e.Payload_stripped);

                if (package != null)
                {
                    foreach (Card card in package)
                    {
                        using (NpgsqlConnection con = InitDb())
                        {
                            //lock (lockObject)
                            //{
                            string? id_to_check = "";
                            string sql = "Select id From card where id=@id";
                            using (NpgsqlCommand command = new NpgsqlCommand(sql, con))
                            {
                                command.Parameters.AddWithValue("@id", card.Id);
                                NpgsqlDataReader reader = command.ExecuteReader();

                                while (reader.Read())
                                {
                                    id_to_check = reader.GetString(0);
                                }

                                if (string.IsNullOrEmpty(id_to_check))
                                {
                                    Console.WriteLine("Card is new: " + card.Id);
                                    tobecontinued = true;
                                }
                                else
                                {
                                    Console.WriteLine("Card is already existing: " + card.Id);
                                    tobecontinued=false;
                                    //e.Reply(401, ("Package contains already existing card. "));
                                }

                                command.Dispose();
                            }
                            con.Close();
                        }
                    }
                }
            }
            
            return tobecontinued;
        }

        /// <summary>Adds a new Package of cards to table card.</summary>
        /// <param name="package_id">next/current Package ID.</param>
        /// <param name="e">Event arguments.</param>
        public static void AddPackagetoCard(int package_id, HttpSvrEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Payload_stripped))
            {
                var package = JsonConvert.DeserializeObject<List<Card>>(e.Payload_stripped);
                using (NpgsqlConnection con = InitDb())
                {
                    //lock (lockObject)
                    //{
                    if (package != null)
                    {
                        foreach (Card card in package)
                        {
                            string sql = "INSERT INTO card (id, name, damage, package_id) VALUES (@id,@name,@damage,@package_id)";
                            using (NpgsqlCommand command = new NpgsqlCommand(sql, con))
                            {
                                //Console.WriteLine("Id: " + card.id + ", Name: " + card.name + ", Damage: " + card.damage);
                                command.Parameters.AddWithValue("@id", card.Id);
                                command.Parameters.AddWithValue("@name", card.Name);
                                command.Parameters.AddWithValue("@damage", card.Damage);
                                command.Parameters.AddWithValue("@package_id", package_id);

                                command.ExecuteNonQuery();
                            }
                        }
                    }
                    // command.Parameters.Add(p);
                    e.Reply(201, ("Added package. "));

                    //}
                    con.Close();
                }
            }
        }

        /// <summary>user aquires new package, coins getting reduced.</summary>
        /// <param name="user">authenticated User object</param>
        /// <param name="package_id">next/current Package ID.</param>
        /// <param name="e">Event arguments.</param>
        public static void UserGetsPackage(User user, int package_id, HttpSvrEventArgs e)
        {
            if (user.Coins > 4)
            {
                // get package_id that is attributed to user later
                using (NpgsqlConnection con = InitDb())
                {
                 
                    string sql = "SELECT package_id FROM card WHERE username IS NULL ORDER BY package_id ASC LIMIT 1";
                    using (NpgsqlCommand command = new NpgsqlCommand(sql, con))
                    {
                        NpgsqlDataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            package_id = reader.GetInt32(0);
                        }

                        command.Dispose();
                    }
                 
                    con.Close();
                }

                if (package_id > 0)
                {
                    user.Coins -= 5;
                    // reduce coins in database
                    using (NpgsqlConnection con = InitDb())
                    {
                        string sql = "UPDATE users SET coins = @coins WHERE username = @username";
                        using (NpgsqlCommand command = new NpgsqlCommand(sql, con))
                        {
                            command.Parameters.AddWithValue("@username", user.Username);
                            command.Parameters.AddWithValue("@coins", user.Coins);
                            command.ExecuteNonQuery();
                            //e.Reply(200, ("Coins: " + coins));
                            command.Dispose();
                        }
                    }
                    // actually attribute package to user
                    using (NpgsqlConnection con = InitDb())
                    {
                        string sql = "UPDATE card SET username = @username WHERE package_id = @package_id";
                        using (NpgsqlCommand command = new NpgsqlCommand(sql, con))
                        {
                            command.Parameters.AddWithValue("@username", user.Username);
                            command.Parameters.AddWithValue("@package_id", package_id);
                            command.ExecuteNonQuery();

                            e.Reply(200, ("Coins: " + user.Coins));
                            command.Dispose();
                        }
                    }
                }

                else
                {
                    e.Reply(404, ("No package left. "));
                }
            }

            else
            {
                e.Reply(401, ("Not enough money"));
            }

        }

        /// <summary>shows card information of user</summary>
        /// <param name="user">authenticated User object, see Models/User.cs</param>
        /// <param name="e">Event arguments.</param>
        public static void GetCardsFromUser(HttpSvrEventArgs e, User user)
        {
            //string username = e.Token;
            string? username_from_db = user.Username;
            using (NpgsqlConnection con = InitDb())
            {
                string sql = "SELECT id, name, damage FROM card WHERE username=@username";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, con))
                {
                    command.Parameters.AddWithValue("@username", username_from_db);
                    NpgsqlDataReader reader = command.ExecuteReader();

                    string? id = null;
                    string? name = null;
                    int damage = 0;
                    string userCards = "";

                    while (reader.Read())
                    {
                        id = reader.GetString(0);
                        name = reader.GetString(1);
                        damage= reader.GetInt32(2);
                        userCards += "Id: " + id + ", Name: " + name + ", Damage:" + damage + "\n";
                    }

                    if (string.IsNullOrEmpty(id))
                    {
                        e.Reply(404, (""));
                    }

                    else
                    //else if(e.Token == id)
                    {
                        e.Reply(200, userCards);
                    }
                }
            }
        }
        /// <summary>first create List deckIds from database deck which contains the cardIds of a deck</summary>
        /// <summary>then use the infirmation from deckIds to create a second list which contains tha actual cards of the card</summary>
        /// <summary>only the second list is returned</summary>
        /// <param name="e">Event arguments.</param>

        public static List<Card> GetDeck(User user)
        {
            bool tobecontinued = true;
            
            List<string> deckIds = new List<string>();
            List<Card> deck = new List<Card>();
            // if no payload then just show deck
            using (NpgsqlConnection con = InitDb())
            {
                string sql = "SELECT card_id, user_id FROM deck WHERE user_id=@id";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, con))
                {
                    command.Parameters.AddWithValue("@id", user.Id);
                    NpgsqlDataReader reader = command.ExecuteReader();
 
                    while (reader.Read())
                    {
                        deckIds.Add(reader.GetString(0));
                        //cardsToPrint +=  "Card_Id: " + card_id + "\n";
                    }
                    if (deckIds?.Any() != true)
                    {
                        // no deck defined
                        tobecontinued = false;
                    }
                    command.Dispose();
                }
              
                con.Close();
            }
            if (tobecontinued)
            {
                if (deckIds!=null)
                {
                    foreach (string cardId in deckIds)
                    {
                        using (NpgsqlConnection con = InitDb())
                        {
                         
                            string sql = "SELECT id, name, damage FROM card WHERE id=@id";
                            using (NpgsqlCommand command = new NpgsqlCommand(sql, con))
                            {
                                command.Parameters.AddWithValue("@id", cardId);
                                NpgsqlDataReader reader = command.ExecuteReader();

                                while (reader.Read())
                                {
                                    Card card = new Card(reader.GetString(0), reader.GetString(1), reader.GetFloat(2));
                                    deck.Add(card);
                                }
                                command.Dispose();
                            }
                       
                            con.Close();
                        }
                    }  // foreach
                }
            } // inner if (tobecontinued)
            return deck;
        }

        /// <summary>shows all cards of a user, two formats of showing the cards are available </summary>
        /// <param name="user">authenticated User object, see Models/User.cs</param>
        /// <param name="e">Event arguments.</param>
        /// <param name="format">integer format.</param>
        public static void ShowDeck(HttpSvrEventArgs e, User user, int format)
        {
            bool tobecontinued = true;
            string cardsToPrint = "";
            if (format == 0)
            {
                cardsToPrint = "User_Id: " + user.Id + "User_Name: " + user.Username + "\n";
            }
            else if (format == 1)
            {
                cardsToPrint = "";
            }

            List<string> deck = new List<string>();
            // if no payload then just show deck
            using (NpgsqlConnection con = InitDb())
            {
          
                string sql = "SELECT card_id, user_id FROM deck WHERE user_id=@id";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, con))
                {
                    command.Parameters.AddWithValue("@id", user.Id);
                    NpgsqlDataReader reader = command.ExecuteReader();
                    //Console.WriteLine(path_username);
                    //string? card_id = null;
                    while (reader.Read())
                    {
                        deck.Add(reader.GetString(0));
                        //cardsToPrint +=  "Card_Id: " + card_id + "\n";
                    }
                    if (deck?.Any() != true)
                    {
                        cardsToPrint += "No deck defined";
                        tobecontinued = false;
                        e.Reply(200, cardsToPrint);
                    }
                    command.Dispose();
                }
                // }
                con.Close();
            }
            if (tobecontinued)
            {
                if (deck != null)
                {
                    foreach (string cardId in deck)
                    {
                        using (NpgsqlConnection con = InitDb())
                        {
                            //  lock (lockObject)
                            //  {
                            string sql = "SELECT id, name, damage FROM card WHERE id=@id";
                            using (NpgsqlCommand command = new NpgsqlCommand(sql, con))
                            {
                                command.Parameters.AddWithValue("@id", cardId);
                                NpgsqlDataReader reader = command.ExecuteReader();

                                string? id = null;
                                string? name = null;
                                int damage = 0;
                                while (reader.Read())
                                {
                                    id = reader.GetString(0);
                                    name = reader.GetString(1);
                                    damage= reader.GetInt32(2);
                                    if (format == 0)
                                    {
                                        cardsToPrint += "Id: " + id + ", Name: " + name + ", Damage:" + damage + "\n";
                                    }
                                    else if (format == 1)
                                    {
                                        cardsToPrint += name + " " + damage + "\n";
                                    }
                                }
                                command.Dispose();
                            }
                            //} // lock
                            con.Close();
                        }
                    }  // foreach
                }
                e.Reply(200, cardsToPrint);
            } // inner if (tobecontinued)

        }

        public static void DefineDeck(HttpSvrEventArgs e, User user)
        {
            bool tobecontinued = true;
            string cardsToPrint = "User_Id: " + user.Username + "\n";
            // if no payload then print error
            if (string.IsNullOrEmpty(e.Payload_stripped))
            {
                e.Reply(404, ("No cards received. "));
            }

            else // define deck
            {
                // Check whether the user has already a deck defined 
                int UserExist;

                using (NpgsqlConnection con = InitDb())
                {
                    NpgsqlCommand checkUserId = new NpgsqlCommand("SELECT COUNT(*) FROM deck WHERE user_id = @user_id", con);
                    checkUserId.Parameters.AddWithValue("@user_id", user.Username);

                    UserExist = Convert.ToInt32(checkUserId.ExecuteScalar());
                    checkUserId.Dispose();
                    con.Close();
                }

                if (!(UserExist == 0 || UserExist == 4))
                {
                    e.Reply(404, ("Database error, not zero or 4 deck cards found. "));
                }
                else
                {
                    // Read in deck as list of strings
                    List<string>? deck = JsonConvert.DeserializeObject<List<string>>(e.Payload_stripped);
                    //if (deck.Count != 4)
                    if (deck == null || deck.Count != 4)
                    {
                        e.Reply(404, ("The deck must consist of exactly 4 cards. "));
                    }
                    else
                    {
                        bool isError404 = false;
                        bool isError401 = false;

                        foreach (string cardId in deck)
                        {
                            // Check if every card belongs to the user
                            using (NpgsqlConnection con = InitDb())
                            {

                                //lock (lockObject)
                                //{
                                string? id_to_check = null;
                                string? username_to_check = null;
                                string? sql = "Select id,username From card where id=@id";
                                using (NpgsqlCommand command = new NpgsqlCommand(sql, con))
                                {
                                    command.Parameters.AddWithValue("@id", cardId);
                                    NpgsqlDataReader reader = command.ExecuteReader();

                                    while (reader.Read())
                                    {
                                        id_to_check = reader.GetString(0);
                                        username_to_check = reader.GetString(1);
                                    }

                                    if (string.IsNullOrEmpty(id_to_check))
                                    {
                                        //Console.WriteLine("Card not in database: " + cardId);
                                        tobecontinued = false;
                                        isError404 = true;
                                        cardsToPrint += "Card not in database " + id_to_check;
                                    }

                                    else
                                    {
                                        if (username_to_check != user.Username)
                                        {
                                            tobecontinued = false;
                                            isError404 = true;
                                            cardsToPrint += "Card does not belong to user: " + id_to_check;
                                        }
                                    }
                                    command.Dispose();
                                }
                                //} lock
                                con.Close();
                            }
                            // two cases: new deck or update deck
                            if (tobecontinued)
                            {
                                // no deck defined, define new deck
                                if (UserExist == 0)
                                {
                                    using (NpgsqlConnection con = InitDb())
                                    {
                                        object lockObject = new object();
                                        lock (lockObject)
                                        {
                                            string sql = "INSERT INTO deck (card_id, user_id) VALUES (@card_id,@user_id)";

                                            using (NpgsqlCommand command = new NpgsqlCommand(sql, con))
                                            {
                                                command.Parameters.AddWithValue("@card_id", cardId);
                                                command.Parameters.AddWithValue("@user_id", user.Id);

                                                command.ExecuteNonQuery();
                                                command.Dispose();
                                                //cardsToPrint +=  "Card_Id: " + cardId + "\n";
                                            }

                                        } // lock

                                        lock (lockObject)
                                        {
                                            string sql = "SELECT id, name, damage FROM card WHERE id=@id";
                                            using (NpgsqlCommand command = new NpgsqlCommand(sql, con))
                                            {
                                                command.Parameters.AddWithValue("@id", cardId);
                                                NpgsqlDataReader reader = command.ExecuteReader();

                                                string? id = null;
                                                string? name = null;
                                                int damage = 0;
                                                while (reader.Read())
                                                {
                                                    id = reader.GetString(0);
                                                    name = reader.GetString(1);
                                                    damage= reader.GetInt32(2);
                                                    cardsToPrint += "Id: " + id + ", Name: " + name + ", Damage:" + damage + "\n";
                                                }
                                                command.Dispose();
                                            }
                                        } // lock
                                        con.Close();
                                    } // using con
                                }
                                else if (UserExist == 4) // deck already defined, update
                                {
                                    using (NpgsqlConnection con = InitDb())
                                    {
                                        //lock (lockObject)
                                        //{
                                        string sql = "UPDATE deck SET card_id = @card_id WHERE user_id = @user_id";

                                        using (NpgsqlCommand command = new NpgsqlCommand(sql, con))
                                        {
                                            command.Parameters.AddWithValue("@card_id", cardId);
                                            command.Parameters.AddWithValue("@user_id", user.Id);

                                            command.ExecuteNonQuery();
                                            command.Dispose();
                                            cardsToPrint +=  "Card_Id: " + cardId + "\n";
                                        }
                                        // } // lock
                                        con.Close();
                                    } // using con
                                }
                            } //inner if(tobecontinued)
                        } // foreach card

                        if (isError404)
                        {
                            e.Reply(404, cardsToPrint);
                        }
                        else if (isError401)
                        {
                            e.Reply(401, cardsToPrint);
                        }
                        else
                        {
                            e.Reply(200, cardsToPrint);
                        }

                    }
                }
            }
        }

    }

}
