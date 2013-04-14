using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSOClient.Code.Utils
{
    public class Promise <T>
    {
        private Func<object, T> Getter;
        private T Value;
        private bool HasRun = false;
        

        public Promise(Func<object, T> getter)
        {
            this.Getter = getter;
        }


        public T Get()
        {
            if (HasRun == false)
            {
                Value = Getter(null);
                HasRun = true;
            }

            return Value;
        }
    }
}
