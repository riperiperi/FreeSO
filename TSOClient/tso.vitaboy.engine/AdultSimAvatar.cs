using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSO.Vitaboy
{
    public class AdultSimAvatar : SimAvatar
    {
        public AdultSimAvatar() : base(TSO.Content.Content.Get().AvatarSkeletons.Get("adult.skel")) {
        }
    }
}
