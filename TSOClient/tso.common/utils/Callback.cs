using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Common.Utils
{

    public delegate void Callback();
    public delegate void Callback<T>(T data);
    public delegate void Callback<T, T2>(T data, T2 data2);
}
