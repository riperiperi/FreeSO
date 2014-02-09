using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.content;

namespace tso.vitaboy
{
    public class AdultSimAvatar : SimAvatar
    {
        public AdultSimAvatar() : base(Content.Get().AvatarSkeletons.Get("adult.skel")) {
        }
    }
}
