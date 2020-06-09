/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Common.Utils
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

        public void SetValue(T value)
        {
            this.HasRun = true;
            this.Value = value;
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
