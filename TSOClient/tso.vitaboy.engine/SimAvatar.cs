using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.content;

namespace tso.vitaboy
{
    public abstract class SimAvatar : Avatar
    {
        public SimAvatar(Skeleton skel) : base(skel){
        }

        private AvatarAppearanceInstance _HeadInstance;
        private Outfit _Head;
        public Outfit Head
        {
            get { return _Head; }
            set {
                _Head = value;
                InvalidateHead();
            }
        }

        private void InvalidateHead(){
            if (_HeadInstance != null){
                base.RemoveAppearance(_HeadInstance, true);
            }
            if (_Head != null)
            {
                var appearanceID = _Head.GetAppearance(_Appearance);
                var appearance = Content.Get().AvatarAppearances.Get(appearanceID);
                if (appearance != null)
                {
                    _HeadInstance = base.AddAppearance(appearance);
                }
            }
        }

        private AvatarAppearanceInstance _BodyInstance;
        private Outfit _Body;

        public Outfit Body
        {
            get
            {
                return _Body;
            }
            set
            {
                _Body = value;
                InvalidateBody();
            }
        }
        
        private void InvalidateBody()
        {
            if (_BodyInstance != null)
            {
                base.RemoveAppearance(_BodyInstance, true);
            }
            if (_Body != null)
            {
                var appearanceID = _Body.GetAppearance(_Appearance);
                var appearance = Content.Get().AvatarAppearances.Get(appearanceID);
                if (appearance != null)
                {
                    _BodyInstance = base.AddAppearance(appearance);
                }
            }
        }
        
        
        private AppearanceType _Appearance = AppearanceType.Light;
        public AppearanceType Appearance
        {
            get
            {
                return _Appearance;
            }
            set
            {
                _Appearance = value;
                InvalidateHead();
            }
        }
    }
}
