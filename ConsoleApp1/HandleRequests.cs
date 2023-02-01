using MTCG.Models;
using MTCG.Battles;
using MTCG.DatabaseAccess;


namespace MTCG
{
    public class HandleRequests
    {
   
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public static methods                                                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

   
        /// <summary>Processes an incoming HTTP request.</summary>
        /// <param name="evt">Event arguments.</param>

        public static void _Svr_Incoming(object evt)
        {
            HttpSvrEventArgs e = (HttpSvrEventArgs)evt;

            string path_username;


            if (!string.IsNullOrEmpty(e.Path))
            {

                //to get the path users/{username}
                if (e.Path.Length > 6)
                {
                    path_username  = e.Path.Substring(7);
                }

                else
                {
                    path_username = "";
                }

                // to be able to apply the switch case to e.g. users/kienboec
                // only constant values are allowed, so the path is shortened to /users/.
                string modified_e_path = e.Path;
                if (modified_e_path.Length > 6)
                {
                    if (modified_e_path.Substring(0, 7) == "/users/")
                    {
                        modified_e_path = "/users/";
                    }
                }
                int format = 0;
                if (modified_e_path.Length > 17)
                {
                    if (modified_e_path.Substring(0, 18) == "/deck?format=plain")
                    {
                        modified_e_path = "/deck";
                        format = 1;
                    }
                }
                bool tobecontinued;
                switch (modified_e_path)
                {
                    case "/users":
                        switch (e.Method)
                        {
                            case "POST":
                                DatabaseUser.CreateUser(e);
                                break;
                        }
                        break;
                    case "/users/":

                        tobecontinued = DatabaseUser.CheckIfLoggedIn(path_username, e);
                        switch (e.Method)
                        {
                            case "GET":

                                if (tobecontinued)
                                {
                                    DatabaseUser.GetUserdata(path_username, e);
                                }
                                break;

                            case "PUT":

                                if (tobecontinued)
                                {
                                    DatabaseUser.EditUserdata(path_username, e);
                                }
                                break;
                        }
                        break;

                    case "/sessions":
                        switch (e.Method)
                        {
                            case "POST":
                                DatabaseUser.LoginUser(e);

                                break;
                        }
                        
                        break;

                    case "/packages":
  
                        switch (e.Method)
                        {
                            case "POST":
                                int package_id;

                               //check if user is admin ist, then get package ID
                                tobecontinued = DatabaseUser.CheckIfAdmin(e);
                                package_id = DatabaseCard.GetPackageId();

                                if (tobecontinued) // only if user is admin
                                {
                                    // continues only if no already existing card in package
                                    tobecontinued = DatabaseCard.CheckIfCardAlreadyExists(e);

                                    if (tobecontinued==false)
                                    {
                                        e.Reply(401, ("Package contains already existing card. "));
                                    }

                                    if (tobecontinued)
                                    {
                                        // add package to table "card"
                                        DatabaseCard.AddPackagetoCard(package_id, e);
                                    }
                                }

                                break;
                        }
                        break;

                    case "/transactions/packages":
                        switch (e.Method)
                        {
                            case "POST":

                                User user = DatabaseUser.AuthenticateUser(e);
                                int package_id = 0;

                                if (user.Id != "id") // User is authenticated and should not return the default value "id".
                                {
                                    //Get package_Id, reduce coins of user and attribute package to user
                                    DatabaseCard.UserGetsPackage(user, package_id, e);
                                }
                                else
                                {
                                    e.Reply(401, ("No authentication token received. "));
                                }
                                break;
                        }

                        break;

                    case "/cards":

                        switch (e.Method)
                        {
                            case "GET":
                                {
                                    if (e.Token != null)
                                    {
                                        User user = DatabaseUser.AuthenticateUser(e);

                                        if (user.Id != "id") // User is authenticated and should not return the default value "id".
                                        {
                                            DatabaseCard.GetCardsFromUser(e, user);
                                        }
                                    }
                                    else
                                    {
                                        e.Reply(401, ("User not authenticated. "));
                                    }
                                }
                                break;
                        }
                        break;

                    case "/deck":
                        
                        switch (e.Method)
                        {
                            case "GET":
                                if (e.Token != null)
                                {
                                    User user = DatabaseUser.AuthenticateUser(e);

                                    if (user.Id != "id") // User is authenticated and should not return the default value "id".
                                    {
                                       //checks if the deck of a user is configured, if that is true it shows the cards of the deck
                                        DatabaseCard.ShowDeck(e, user, format);
                                    }

                                    else
                                    {
                                        e.Reply(401, ("No authentication token received. "));
                                    }
                                }
                                break;

                            case "PUT":

                                if (e.Token != null)
                                {
                                    User user = DatabaseUser.AuthenticateUser(e);

                                    if (user.Id != "id")// User is authenticated and should not return the default value "id".
                                    {

                                        DatabaseCard.DefineDeck(e, user);
                                    }
                                } 
                                else
                                {
                                    e.Reply(401, ("No authentication token received. "));
                                }
                                break;
                        }
                       
                        break;

                    case "/stats":
                        switch (e.Method)
                        {
                            case "GET":
                                if (e.Token != null)
                                {
                                    User user = DatabaseUser.AuthenticateUser(e);

                                    if (user.Id != "id") //User is authenticated and should not return the default value "id".
                                    {
                                        //Get the statistic of a user
                                        DatabaseUser.GetStats(e, user);
                                    }
                                }
                                else
                                {
                                    e.Reply(401, ("No authentication token received. "));
                                }
                                break;
                        }
                        break;

                    case "/score":

                        switch (e.Method)
                        {
                            case "GET":
                                if (e.Token != null)
                                {
                                    User user = DatabaseUser.AuthenticateUser(e);

                                    if (user.Id != "id") 
                                    {
                                        //Get score of all users 
                                        DatabaseUser.GetScore(e);
                                    }
                                }
                                else
                                {
                                    e.Reply(401, ("No authentication token received. "));
                                }
                                break;
                        }
                        break;

                    case "/battles":
                        switch (e.Method)
                        {
                            case "POST":
                                if (e.Token != null)
                                {
                                    User user = DatabaseUser.AuthenticateUser(e);

                                    if (user.Id != "id") 
                                    {
                                      //Initialize and start the battle
                                        Battle.Init(e, user);
                                    }
                                }
                                else
                                {
                                    e.Reply(401, ("No authentication token received. "));
                                }
                                break;
                        }
                        break;

                    default:
                            e.Reply(404, ("Command not implemented. "));
                        break;
                }
            }
        }
    }
}
