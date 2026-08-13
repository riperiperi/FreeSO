namespace FSO.WsGateway
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var listen = "http://127.0.0.1:8087";
            var city = ("127.0.0.1", 33101);
            var lot = ("127.0.0.1", 34101);
            var sandbox = ("127.0.0.1", 37564);

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--listen": listen = args[++i]; break;
                    case "--city": city = ParseTarget(args[++i]); break;
                    case "--lot": lot = ParseTarget(args[++i]); break;
                    case "--sandbox": sandbox = ParseTarget(args[++i]); break;
                    default:
                        Console.Error.WriteLine("usage: FSO.WsGateway [--listen http://127.0.0.1:8087] [--city host:33101] [--lot host:34101] [--sandbox host:37564]");
                        return 2;
                }
            }

            var gateway = new Gateway(new Dictionary<string, (string, int)>
            {
                ["/city"] = city,
                ["/lot"] = lot,
                ["/sandbox"] = sandbox,
            });
            await gateway.Start(listen);
            Console.WriteLine($"gateway listening on {gateway.Address}");
            Console.WriteLine($"  {gateway.Address.Replace("http", "ws")}/city -> {city.Item1}:{city.Item2}");
            Console.WriteLine($"  {gateway.Address.Replace("http", "ws")}/lot  -> {lot.Item1}:{lot.Item2}");
            Console.WriteLine($"  {gateway.Address.Replace("http", "ws")}/sandbox -> {sandbox.Item1}:{sandbox.Item2} (LotHostLite lockstep)");

            await Task.Delay(Timeout.Infinite);
            return 0;
        }

        private static (string, int) ParseTarget(string s)
        {
            var idx = s.LastIndexOf(':');
            return (s.Substring(0, idx), int.Parse(s.Substring(idx + 1)));
        }
    }
}
