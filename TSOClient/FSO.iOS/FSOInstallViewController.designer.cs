// WARNING
//
// This file has been generated automatically by Xamarin Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;
using UIKit;

namespace FSOiOS
{
    [Register ("FSOInstallViewController")]
    partial class FSOInstallViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton IpConfirm { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextField IPEntry { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIProgressView StatusProgress { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel StatusText { get; set; }

        [Action ("IpConfirm_TouchUpInside:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void IpConfirm_TouchUpInside (UIKit.UIButton sender);

        void ReleaseDesignerOutlets ()
        {
            if (IpConfirm != null) {
                IpConfirm.Dispose ();
                IpConfirm = null;
            }

            if (IPEntry != null) {
                IPEntry.Dispose ();
                IPEntry = null;
            }

            if (StatusProgress != null) {
                StatusProgress.Dispose ();
                StatusProgress = null;
            }

            if (StatusText != null) {
                StatusText.Dispose ();
                StatusText = null;
            }
        }
    }
}