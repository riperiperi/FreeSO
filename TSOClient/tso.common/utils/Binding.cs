using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FSO.Common.Utils
{
    public class Binding <T> where T : INotifyPropertyChanged
    {
        private List<IBinding> Bindings = new List<IBinding>();
        private List<INotifyPropertyChanged> Watching = new List<INotifyPropertyChanged>();
        private T _Value;

        public Binding()
        {
        }

        public T Value
        {
            get
            {
                return _Value;
            }
            set
            {
                ClearWatching();
                _Value = value;
                if(_Value != null){
                    Watch(_Value);
                }
                Digest();
            }
        }

        private void ClearWatching()
        {
            foreach (var item in Watching)
            {
                item.PropertyChanged -= OnPropertyChanged;
            }
        }

        private void Watch(INotifyPropertyChanged source)
        {
            source.PropertyChanged += OnPropertyChanged;
            Watching.Add(source);

            var properties = source.GetType().GetProperties();
            foreach(var property in properties){
                if (typeof(INotifyPropertyChanged).IsAssignableFrom(property.PropertyType)){
                    var value = (INotifyPropertyChanged)property.GetValue(source, null);
                    if(value != null){
                        Watch(value);
                    }
                }
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var property = sender.GetType().GetProperty(e.PropertyName);
            if (typeof(INotifyPropertyChanged).IsAssignableFrom(property.PropertyType))
            {
                //We may have a new nested object to watch
                ClearWatching();
                if (_Value != null){
                    Watch(_Value);
                }
            }
            Digest();
        }

        //Using a dumb digest system for now, not very efficient but works
        private void Digest()
        {
            foreach(var binding in Bindings)
            {
                binding.Digest(_Value);
            }
        }

        public Binding<T> WithBinding(object target, string targetProperty, string sourcePath)
        {
            //If top level changes, we need to update children
            //If member of top level changes, we need to update children
            var binding = new DotPathBinding(target, targetProperty, DotPath.CompileDotPath(typeof(T), sourcePath));
            Bindings.Add(binding);
            return this;
        }
    }

    class DotPathBinding : Binding
    {
        private PropertyInfo[] Path;
        private object LastValue;

        public DotPathBinding(object target, string targetProperty, PropertyInfo[] path) : base(target, targetProperty)
        {
            this.Path = path;
        }

        public override void Digest(object source){
            var value = GetValue(source);
            if(value != LastValue){
                LastValue = value;
                SetValue(value);
            }
        }

        private object GetValue(object source)
        {
            return DotPath.GetDotPathValue(source, Path);
        }
    }

    abstract class Binding : IBinding
    {
        private object Target;
        private string TargetProperty;
        private PropertyInfo[] TargetPropertyPath;

        public Binding(object target, string targetProperty)
        {
            Target = target;
            TargetProperty = targetProperty;
            TargetPropertyPath = DotPath.CompileDotPath(target.GetType(), targetProperty);
        }

        protected void SetValue(object value)
        {
            DotPath.SetDotPathValue(Target, TargetPropertyPath, value);
        }

        public abstract void Digest(object source);
    }

    interface IBinding
    {
        void Digest(object source);

        //object GetValue(object source);
        //void SetValue(object value);
    }
}
