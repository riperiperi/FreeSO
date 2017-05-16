using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Framework
{
    [DataServiceModel]
    public abstract class AbstractModel : INotifyPropertyChanged, IModel
    {
        public bool ClientSourced;
        public bool RequestDefaultData {
            get; set;
        } = false;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
