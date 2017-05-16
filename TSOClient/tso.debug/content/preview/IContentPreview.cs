using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Debug.Content.Preview
{
    interface IContentPreview
    {
        bool CanPreview(object value);
        void Preview(object value);
    }
}
