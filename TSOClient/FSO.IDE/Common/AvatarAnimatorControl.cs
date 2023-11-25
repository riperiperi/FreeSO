using FSO.Client.UI.Framework;
using FSO.Client;
using FSO.Vitaboy;
using FSO.SimAntics.Engine.Scopes;

namespace FSO.IDE.Common
{
    public partial class AvatarAnimatorControl : FSOUIControl
    {
        public UIAvatarAnimator Renderer;
        public void EnsureReady()
        {
            if (FSOUI == null)
            {
                var mainCont = new UIExternalContainer(128, 128);
                mainCont.UseZ = true;
                Renderer = new UIAvatarAnimator();
                mainCont.Add(Renderer);
                GameFacade.Screens.AddExternal(mainCont);

                SetUI(mainCont);
            }
        }

        public void ShowAnim(string anim)
        {
            EnsureReady();
            Renderer.SetAnimation(anim);
        }

        public void BindOutfit(VMPersonSuits type, Outfit oft)
        {
            EnsureReady();
            Renderer.BindOutfit(type, oft);
        }

        public void AddAccessory(string name)
        {
            EnsureReady();
            Renderer.AddAccessory(name);
        }

        public void RemoveAccessory(string name)
        {
            EnsureReady();
            Renderer.RemoveAccessory(name);
        }

        public void ClearAccessories()
        {
            EnsureReady();
            Renderer.ClearAccessories();
        }
    }
}
