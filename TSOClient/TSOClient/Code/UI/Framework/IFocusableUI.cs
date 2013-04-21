using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Model;

namespace TSOClient.Code.UI.Framework
{
    public interface IFocusableUI
    {
        void OnFocusChanged(FocusEvent newFocus);
    }

    public enum FocusEvent
    {
        FocusIn,
        FocusOut
    }
}
