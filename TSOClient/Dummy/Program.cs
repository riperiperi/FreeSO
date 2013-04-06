using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dummy
{
    class Program
    {
        static void Main(string[] args)
        {
            var service = new TSOServiceClient.TSOServiceClient();
            var session = service.Authenticate(new TSOServiceClient.Model.AuthRequest {
                Username = "dazlee4",
                Password = "password"
            });

            var x = true;
        }
    }
}
