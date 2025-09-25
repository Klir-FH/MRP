using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRP.Models
{
    public class Credentials
    {
        
        public int Id { get; set; }
        public int UserId { get; set; }
        public string HashedPassword { get; set; }
    }
}
