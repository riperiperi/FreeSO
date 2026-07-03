namespace FSO.Server.Protocol.Utils
{
    public static class PortTransformer
    {
        public static string TransformAddress(string address, string connType = "101")
        {
            int portSplit = address.LastIndexOf(":");

            if (portSplit == -1)
            {
                return address;
            }

            string port = address.Substring(portSplit + 1);

            return port.Length == 2 ? address + connType : address;
        }

        public static string DefaultCityPort(string address)
        {
            int portSplit = address.LastIndexOf(":");

            if (portSplit != -1)
            {
                return address;
            }

            return $"{address}:33101";
        }
    }
}
