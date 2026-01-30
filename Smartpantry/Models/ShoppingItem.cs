using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartpantry.Models
{
    public class ShoppingItem
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Name { get; set; }

        public decimal Amount { get; set; }

        public string Unit { get; set; }

        public bool Checked { get; set; }

        public User User { get; set; }
    }
}
