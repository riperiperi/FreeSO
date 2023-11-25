using FSO.Client.UI.Framework;
using Ninject.Parameters;
using Ninject;

namespace FSO.Client.Utils
{
    public static class ControllerUtils
    {
        public static T BindController<T>(UIElement target)
        {
            var controllerInstance =
                FSOFacade.Kernel.Get<T>(new ConstructorArgument("view", target));
            target.Controller = controllerInstance;
            return controllerInstance;
        }
    }
}
