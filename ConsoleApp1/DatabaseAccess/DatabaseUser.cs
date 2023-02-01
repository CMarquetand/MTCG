using MTCG.Models;
using Newtonsoft.Json;
using Npgsql;

namespace MTCG.DatabaseAccess
{
    public class DatabaseUser
    {
        private static readonly string connectionstring = "Host=localhost;Username=postgres;Password=postgres;Database=MonsterCardGame";

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public static methods                                                                                    //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Initializes the database connection.</summary>
        public static NpgsqlConnection InitDb()
        {
            NpgsqlConnection con = new NpgsqlConnection(connectionstring);
            con.Open();
            return con;
        }

        /// <summary>Checks if a username exists, if not creates a new user in the Database table "users".</summary>
        /// <param name="evt">Event arguments.</param>
        public static void CreateUser(HttpSvrEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Payload_stripped))
            {
                User user = JsonConvert.DeserializeObject<User>(e.Payload_stripped)!;
                string? username = user.Username;
 
                using (NpgsqlConnection con = InitDb())
                {
                    // check if username already exists
                    NpgsqlCommand check_User_Name = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE username = @Username", con);
                    check_User_Name.Parameters.AddWithValue("@Username", username);
                    int UserExist = Convert.ToInt32(check_User_Name.ExecuteScalar());
                    check_User_Name.Dispose();

                    if (UserExist > 0)
                    {
                        //Username exist

                        e.Reply(409, user.Username);
                    }
                    else
                    {
                        //Username doesn't exist.
                        // create new user
                        if (!string.IsNullOrEmpty(user.Username) && !string.IsNullOrEmpty(user.Password))
                        {
                            string sql = "INSERT INTO users (id, username, password, coins, elo, bio, image, name, wins, losses, draws) VALUES (@id,@username,@password,@coins,@elo,@bio,@image,@name,@wins,@losses,@draws)";
                            using (NpgsqlCommand command = new NpgsqlCommand(sql, con))
                            {
                                //creates a token/Id for user
                                string token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

                                user.Id = token;
                                user.Coins = 20;
                                user.Elo = 100;
                                user.Bio = "I'm playing";
                                user.Image = ":-)";
                                user.Wins = 0;
                                user.Losses = 0;
                                user.Draws = 0;

                                command.Parameters.AddWithValue("@username", user.Username);
                                command.Parameters.AddWithValue("@password", user.Password);
                                command.Parameters.AddWithValue("@id", user.Id);
                                command.Parameters.AddWithValue("@coins", user.Coins);
                                command.Parameters.AddWithValue("@elo", user.Elo);
                                command.Parameters.AddWithValue("@bio", user.Bio);
                                command.Parameters.AddWithValue("@image", user.Image);
                                command.Parameters.AddWithValue("@name", user.Username);
                                command.Parameters.AddWithValue("@wins", user.Wins);
                                command.Parameters.AddWithValue("@losses", user.Losses);
                                command.Parameters.AddWithValue("@draws", user.Draws);

                                command.ExecuteNonQuery();


                                e.Reply(201, ("Username: " + user.Username + ", Password: " + user.Password));
                                command.Dispose();
                            }
                        }
                        else
                        {
                            e.Reply(400, "Bad Request.");
                        }
                    }
                }
            }
        }

        /// <summary> checks whether the username and password match the data in the database.</summary>
        /// <param name="evt">Event arguments.</param>
        public static void LoginUser(HttpSvrEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Payload_stripped))
            {
                User? user = JsonConvert.DeserializeObject<User>(e.Payload_stripped);

                using (NpgsqlConnection con = InitDb())
                {
                    if (!string.IsNullOrEmpty(user?.Username))
                    {
                        string sql = "SELECT id, username, password FROM users WHERE username=@username";
                        using (NpgsqlCommand command = new NpgsqlCommand(sql, con))
                        {
                            command.Parameters.AddWithValue("@username", user.Username);

                            NpgsqlDataReader reader = command.ExecuteReader();
                            //Console.WriteLine(path_username);
                            string? id = null;
                            string? username = null;
                            string? password = null;

                            while (reader.Read())
                            {
                                id = reader.GetString(0);
                                username = reader.GetString(1);
                                password = reader.GetString(2);
                            }

                            if (string.IsNullOrEmpty(id))
                            {
                                e.Reply(404, null);
                            }

                            else if (password == user.Password)
                            {
                                Console.WriteLine("Logged in");

                                e.Reply(200, ("Username: " + username + ", Id:" + id));
                            }

                            else
                            {
                                e.Reply(401, ("Wrong password."));
                            }

                        }
                    }
                }
            }

        }

        /// <summary> returns true if user is logged in.</summary>
        /// <param name="path_username">Substring of e.Path.</param>
        /// <param name="e">Event arguments.</param>
        public static bool CheckIfLoggedIn(string path_username, HttpSvrEventArgs e)
        {
            bool tobecontinued = false;
            using (NpgsqlConnection con = InitDb())
            { 
                string sql = "SELECT id FROM users WHERE username=@username";
                using (NpgsqlCommand check_if_logged_in = new NpgsqlCommand(sql, con))
                {
                    check_if_logged_in.Parameters.AddWithValue("@username", path_username);
                    NpgsqlDataReader reader = check_if_logged_in.ExecuteReader();
                    string? id = null;

                    while (reader.Read())
                    {
                        id = reader.GetString(0);
                    }

                    if (string.IsNullOrEmpty(id))
                    {
                        e.Reply(404, ("User not found. "));
                    }

                    else if (e.Token == path_username+"-mtcgToken")
                    //else if(e.Token == id)
                    {
                        //e.Reply(200, ("Username: " + username));
                        // continue, e.Reply not possible, otherwise client will be already closed.
                        Console.WriteLine("User authenticated");
                        tobecontinued=true;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(e.Token))
                        {
                            Console.WriteLine(e.Token, " "+path_username+"-mtcgToken");
                            e.Reply(401, ("Wrong authentication. "));
                            tobecontinued = false;
                        }
                    }

                    check_if_logged_in.Dispose();
                }
                con.Close();
            }
            return tobecontinued;           
        }

        /// <summary>shows username,bio,image of user.</summary>
        /// <param name="path_username">Substring of e.Path.</param>
        /// <param name="e">Event arguments.</param>
        public static void GetUserdata(string path_username, HttpSvrEventArgs e)
        {
            using (NpgsqlConnection con = InitDb())
            {

                string sql = "SELECT id, username, bio, image FROM users WHERE username=@username";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, con))
                {
                    command.Parameters.AddWithValue("@username", path_username);
                    NpgsqlDataReader reader = command.ExecuteReader();

                    string? username = null;
                    string? bio = null;
                    string? image = null;
                    string? id = null;

                    while (reader.Read())
                    {
                        id = reader.GetString(0);
                        username = reader.GetString(1);
                        bio = reader.GetString(2);
                        image = reader.GetString(3);
                    }

                    if (string.IsNullOrEmpty(id))
                    {
                        e.Reply(404, (""));
                    }
                    else if (e.Token == username+"-mtcgToken")
                    //else if(e.Token == id)
                    {
                        e.Reply(200, ("Username: " + username + ", Bio: " + bio + ", Image:" + image));
                    }
                    else
                    {
                        e.Reply(401, (""));
                    }
                }
            }
        }

        /// <summary>change/update username,bio,image of user.</summary>
        /// <param name="path_username">Substring of e.Path.</param>
        /// <param name="e">Event arguments.</param>
        public static void EditUserdata(string path_username, HttpSvrEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Payload_stripped))
            {

                User user = JsonConvert.DeserializeObject<User>(e.Payload_stripped)!;

                using (NpgsqlConnection con = InitDb())
                {
                    if (!string.IsNullOrEmpty(user.Name) && !string.IsNullOrEmpty(user.Bio) && !string.IsNullOrEmpty(user.Image))
                    {
                        string sql = "UPDATE users SET name = @name, bio = @bio, image = @image WHERE username = @username";
                        using (NpgsqlCommand command = new NpgsqlCommand(sql, con))
                        {
                            command.Parameters.AddWithValue("@username", path_username);
                            command.Parameters.AddWithValue("@name", user.Name);
                            command.Parameters.AddWithValue("@bio", user.Bio);
                            command.Parameters.AddWithValue("@image", user.Image);
                            command.ExecuteNonQuery();

                            e.Reply(200, ("Name: " + user.Name + ", Bio: " + user.Bio + ", Image: " + user.Image));
                        }
                    }
                }
            }
        }

        /// <summary>update elo, wins, losses of user.</summary>
        /// <param name="user">authenticated User object</param>
        public static void EditUserStats(User user)
        {
            using (NpgsqlConnection con = InitDb())
            {
                if (!string.IsNullOrEmpty(user.Username))
                {
                    string sql = "UPDATE users SET elo = @elo, wins = @wins, losses = @losses, draws = @draws WHERE username = @username";
                    using (NpgsqlCommand command = new NpgsqlCommand(sql, con))
                    {
                        command.Parameters.AddWithValue("@username", user.Username);
                        command.Parameters.AddWithValue("@elo", user.Elo);
                        command.Parameters.AddWithValue("@wins", user.Wins);
                        command.Parameters.AddWithValue("@losses", user.Losses);
                        command.Parameters.AddWithValue("@draws", user.Draws);
                        command.ExecuteNonQuery();

                    }
                }
                con.Close();
            }
        }
        /// <summary> returns true if user is admin.</summary>
        /// <param name="e">Event arguments.</param>
        public static bool CheckIfAdmin(HttpSvrEventArgs e)
        {
            bool tobecontinued = false;
            using (NpgsqlConnection con = InitDb())
            {
                string sql = "SELECT id FROM users WHERE username= 'admin'";
                using (NpgsqlCommand check_if_admin = new NpgsqlCommand(sql, con))
                {
                    NpgsqlDataReader reader = check_if_admin.ExecuteReader();
                    string username = "admin";
                    string? id = null;

                    while (reader.Read())
                    {
                        id = reader.GetString(0);
                    }

                    if (string.IsNullOrEmpty(id))
                    {
                        e.Reply(404, (""));
                    }

                    else if (e.Token == username+"-mtcgToken")
                    //else if(e.Token == id)
                    {
                        //e.Reply(200, ("Username: " + username));
                        // continue, e.Reply not possible, otherwise client will be already closed.
                        Console.WriteLine("Admin authenticated");
                        tobecontinued=true;

                    }
                    else
                    {
                        //Console.WriteLine(e.Token," "+username+"-mtcgToken");
                        e.Reply(401, (""));
                    }
                    check_if_admin.Dispose();
                }        //}
                con.Close();
            }
            return tobecontinued;
        }


        /// <summary> authenticates a user and returns an instance of the Users class</summary>
        /// <param name="e">Event arguments.</param>
        public static User AuthenticateUser(HttpSvrEventArgs e)
        {
            //Users? users = JsonConvert.DeserializeObject<Users>(e.Payload_stripped);
            User userFromDB = new User("username", "password", "id", 0, 100, "me playing", ":-)", "name", 0, 0, 0);
            
            string? username = e.Token;
            if (!string.IsNullOrEmpty(username))
            {
                // to be replaced with lines further down below, if correct token transferred via curl

                username = username.Replace("-mtcgToken", "");

                string? id = "";
                string? username_from_db = "";
                int coins = 0;
                int elo = 100;
                string? bio = "";
                string? image = "";
                string? name = "";
                int wins = 0;
                int losses = 0;
                int draws = 0;

                using (NpgsqlConnection con = InitDb())
                {
                    // to be replaced with lines further down below, if correct token transferred via curl
                    string sql = "SELECT id, username, coins, elo, bio, image, name, wins, losses, draws FROM users WHERE username=@username";
                    // use the following line if the correct token is transferred via curl
                    //string sql = "SELECT id, username, coins FROM users WHERE id=@id";
                    using (NpgsqlCommand check_if_logged_in = new NpgsqlCommand(sql, con))
                    {
                        check_if_logged_in.Parameters.AddWithValue("@username", username);
                        // use the following line instead of the previous one if the correct token is transferred via curl
                        //check_if_logged_in.Parameters.AddWithValue("@id", users.Token);
                        NpgsqlDataReader reader = check_if_logged_in.ExecuteReader();

                        while (reader.Read())
                        {
                            id = reader.GetString(0);
                            username_from_db = reader.GetString(1);
                            coins = reader.GetInt32(2);
                            elo = reader.GetInt32(3);
                            bio = reader.GetString(4);
                            image = reader.GetString(5);
                            name = reader.GetString(6);
                            wins = reader.GetInt32(7);
                            losses = reader.GetInt32(8);
                            draws = reader.GetInt32(9);
                        }

                        if (string.IsNullOrEmpty(id))
                        {
                            e.Reply(404, (""));
                        }

                        else if (e.Token == username+"-mtcgToken")
                        //else if(e.Token == id)
                        {
                            // Continue below.

                            //e.Reply(200, ("Username: " + username));
                            // continue, e.Reply not possible, otherwise client will be already closed.
                            Console.WriteLine("User authenticated");
                        }

                        else
                        {
                            e.Reply(401, ("")); // This should never happen, but just in case...
                        }

                        check_if_logged_in.Dispose();
                    }
                    
                    con.Close();
                }
                userFromDB.Username = username_from_db;
                userFromDB.Id = id;
                userFromDB.Coins = coins;
                userFromDB.Elo = elo;
                userFromDB.Bio = bio;
                userFromDB.Image = image;
                userFromDB.Name = name;
                userFromDB.Wins = wins;
                userFromDB.Losses = losses;
                userFromDB.Draws = draws;
            }
            return userFromDB;
        }

        /// <summary> shows game statistic (elo, wins, losses, draws) of user</summary>
        /// <param name="e">Event arguments.</param>
        /// <param name="user">authenticated User object</param>
        public static void GetStats(HttpSvrEventArgs e, User user)
        {
            using (NpgsqlConnection con = InitDb())
            {

                string sql = "SELECT id, username, elo, wins, losses, draws FROM users WHERE username=@username";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, con))
                {
                    command.Parameters.AddWithValue("@username", user.Username);
                    NpgsqlDataReader reader = command.ExecuteReader();

                    string? id = null;
                    string? username = null;
                    int elo = 0;
                    int wins = 0;
                    int losses = 0;
                    int draws = 0;  

                    while (reader.Read())
                    {
                        id = reader.GetString(0);
                        username = reader.GetString(1);
                        elo = reader.GetInt32(2);
                        wins = reader.GetInt32(3);  
                        losses = reader.GetInt32(4);    
                        draws = reader.GetInt32(5); 
                    }

                    if (string.IsNullOrEmpty(id))
                    {
                        e.Reply(404, ("User not found. "));
                    }
                    else if (e.Token == username+"-mtcgToken")
                    //else if(e.Token == id)
                    {
                        e.Reply(200, ("Username: " + username + ", Elo: " + elo + ", Wins: " + wins + ", Losses: " + losses + ", Draws: " + draws));
                    }
                    else
                    {
                        e.Reply(401, ("Not authenticated. "));
                    }
                }
            }
        }

        /// <summary> shows highscore of all users of table users</summary>
        /// <param name="e">Event arguments.</param>
        public static void GetScore(HttpSvrEventArgs e)
        {
            using (NpgsqlConnection con = InitDb())
            {
                string? scoreToPrint = ""; 
                string sql = "SELECT id, username, elo FROM users ORDER BY elo DESC";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, con))
                {
                    NpgsqlDataReader reader = command.ExecuteReader();

                    string? id = null;
                    string? username = null;
                    int elo = 0;
                    

                    while (reader.Read())
                    {
                        id = reader.GetString(0);
                        username = reader.GetString(1);
                        elo = reader.GetInt32(2);
                        scoreToPrint += "Elo: " + elo + ", Username: " + username  +"\n";

                    }

                    if (string.IsNullOrEmpty(id))
                    {
                        e.Reply(404, ("User not found. "));
                    }
                    else
                    {
                        e.Reply(200, scoreToPrint);
                    }
                }
            }
        }
    }

}
