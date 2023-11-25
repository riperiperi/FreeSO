using System;

namespace FSO.Common.Utils
{
    public class Promise <T>
    {
        private Func<object, T> Getter;
        private T Value;
        private bool HasRun = false;
        

        public Promise(Func<object, T> getter)
        {
            this.Getter = getter;
        }

        public void SetValue(T value)
        {
            this.HasRun = true;
            this.Value = value;
        }


        public T Get()
        {
            if (HasRun == false)
            {
                Value = Getter(null);
                HasRun = true;
            }

            return Value;
        }
    }
}
