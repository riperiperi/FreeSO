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

        public event Callback<T> ValueChanged;

        public Binding()
        {
        }

        ~Binding()
        {
            ClearWatching();
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

                if(ValueChanged != null)
                {
                    ValueChanged(_Value);
                }
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
            lock (Watching)
            {
                var property = sender.GetType().GetProperty(e.PropertyName);
                if (typeof(INotifyPropertyChanged).IsAssignableFrom(property.PropertyType))
                {
                    //We may have a new nested object to watch
                    ClearWatching();
                    if (_Value != null)
                    {
                        Watch(_Value);
                    }
                }
            }
            Digest();
        }

        //Using a dumb digest system for now, not very efficient but works
        private void Digest()
        {
            lock (this)
            {
                foreach (var binding in Bindings)
                {
                    binding.Digest(_Value);
                }
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

        public Binding<T> WithMultiBinding(Callback<BindingChange[]> callback, params string[] paths)
        {
            var compiledPaths = new PropertyInfo[paths.Length][];
            for(int i=0; i < paths.Length; i++){
                compiledPaths[i] = DotPath.CompileDotPath(typeof(T), paths[i]);
            }

            var binding = new MultiDotPathBinding(callback, paths, compiledPaths);
            Bindings.Add(binding);
            return this;   
        }
    }

    public class BindingChange
    {
        public string Path;
        public object PreviousValue;
        public object Value;
    }

    class MultiDotPathBinding : IBinding
    {
        private Callback<BindingChange[]> Callback;
        private PropertyInfo[][] Paths;
        private string[] PathStrings;
        private object[] Values;

        public MultiDotPathBinding(Callback<BindingChange[]> callback, string[] pathStrings, PropertyInfo[][] paths)
        {
            Callback = callback;
            Paths = paths;
            PathStrings = pathStrings;
            Values = new object[Paths.Length];
        }

        public void Digest(object source)
        {
            List<BindingChange> changes = null;
            for(int i=0; i < Paths.Length; i++){
                var path = Paths[i];
                var value = DotPath.GetDotPathValue(source, path);

                if(value != Values[i]){
                    //Changed
                    if (changes == null) { changes = new List<BindingChange>(); }
                    changes.Add(new BindingChange()
                    {
                        Path = PathStrings[i],
                        PreviousValue = Values[i],
                        Value = value
                    });
                    Values[i] = value;
                }
            }

            if(changes != null)
            {
                Callback(changes.ToArray());
            }
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
