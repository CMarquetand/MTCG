namespace MTCG.Models
{
    public class User
    {

        public string Username { get; set; }
        public string Password { get; set; }
        public string Id { get; set; }
        public int Coins { get; set; }
        public int Elo { get; set; }
        public string? Bio { get; set; }
        public string? Image { get; set; }
        public string? Name { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }

        public User(string u, string p, string id, int c, int e, string b, string i, string n, int w, int l, int d)
        {
            Username = u;
            Password = p;
            Id = id;
            Coins = c;
            Elo = e;
            Bio = b;
            Image = i;
            Name = n;
            Wins = w;
            Losses = l;
            Draws = d;
        }


    }
}//namespace


