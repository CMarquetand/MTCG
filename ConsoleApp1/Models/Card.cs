using System;
using System.Collections.Generic;
using System.Linq;

namespace MTCG.Models
{
    public class Card
    {
        public Card(string i, string name, float damage)
        {
            Id = i; 
            Name = name;    
            Damage = damage;    
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public float Damage { get; set; }

    }
}
