using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Url_Detection_Agent.Models.API.ServerDetector
{
    public class ServerDetectorResponse
    {
        public string Url_link { get; set; }
        public bool Is_malicious { get; set; }
        public object? Reason { get; set; } 
    }
}
