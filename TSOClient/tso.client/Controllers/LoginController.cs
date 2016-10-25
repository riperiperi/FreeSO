using FSO.Client.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers
{
    public class LoginController : IDisposable
    {
        private LoginScreen View;

        public LoginController(LoginScreen view)
        {
            View = view;
        }

        public void Dispose()
        {
            View.Dispose();
        }
    }
}
