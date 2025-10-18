using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class UserMediaEntry
    {
        public int UserID { get; set; }
        public int MediaEntryId { get; set; }
        public UserMediaInteractions Type { get; set; }

    }
}
