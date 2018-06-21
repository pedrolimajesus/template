using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppComponents;
using System.ComponentModel.DataAnnotations;

namespace Lok.Unik.ModelCommon.Client
{
    public class Tenant
    {
        public Tenant() 
        { 
        }

        [DocumentIdentifier]
        [Key]
        public string Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Site { get; set; }

        public string CustomerNumber { get; set; }

        public string License { get; set; }


        [Timestamp]
        public DateTime Timestamp { get; set; }
    }
}
