namespace FSO.Common.Utils
{

    public delegate void Callback();
    public delegate void Callback<T>(T data);
    public delegate void Callback<T, T2>(T data, T2 data2);
    public delegate void Callback<T, T2, T3>(T data, T2 data2, T3 data3);
}
