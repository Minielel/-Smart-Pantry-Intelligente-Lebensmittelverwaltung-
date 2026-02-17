using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartpantry.Models
{
    public class FoodItem
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Name { get; set; }

        public decimal Amount { get; set; }

        public string Unit { get; set; }

        public DateTime ExpirationDate { get; set; }

        public int? CategoryId { get; set; }

        public DateTime CreatedAt { get; set; }

        public User User { get; set; }

        public Category Category { get; set; }
    }
}
