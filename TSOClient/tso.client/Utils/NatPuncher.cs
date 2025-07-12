using Open.Nat;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Client.Utils
{
    public class NatPuncher : IDisposable
    {
        private const int PortLeaseRenew = 60 * 4; // 4 minutes
        private const int PortLeaseLength = 300; // 5 minutes

        private string _entryName;
        private bool _disposed;
        private Mapping _portMapping;
        private NatDevice _device;
        private CancellationTokenSource _disposedCancellation;

        public NatPuncher(string entryName)
        {
            _entryName = entryName;
        }

        public NatPuncher(string entryName, NatPuncher other)
        {
            _entryName = entryName;
            _device = other._device;
        }

        private static bool CanOpenTCP(ushort port)
        {
            IPGlobalProperties props = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] listeners = props.GetActiveTcpListeners();

            foreach (var listener in listeners)
            {
                if (listener.Port == port)
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<ushort> NatPunch(ushort basePort, ushort increment, int attempts)
        {
            NatDiscoverer discoverer = new NatDiscoverer();
            CancellationTokenSource cts = new CancellationTokenSource(1000);

            try
            {
                _device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);
            }
            catch (NatDeviceNotFoundException)
            {
                return 0; // No UPnP available
            }

            for (int i = 0; i < attempts; i++)
            {
                ushort port = (ushort)(basePort + increment * i);

                if (CanOpenTCP(port))
                {
                    try
                    {
                        _portMapping = new Mapping(Protocol.Tcp, port, port, PortLeaseLength, _entryName);

                        await _device.CreatePortMapAsync(_portMapping);

                        BeginPolling();

                        return port;
                    }
                    catch (MappingException)
                    {
                        // Failed to get this port, check the next one.
                        continue;
                    }
                    catch (Exception)
                    {
                        // Unknown error - is UPnP broken?
                        return 0;
                    }
                }
            }

            return ushort.MaxValue;
        }

        private void BeginPolling()
        {
            _disposedCancellation = new CancellationTokenSource();

            _ = Task.Delay(PortLeaseRenew * 1000, _disposedCancellation.Token).ContinueWith((task) => Task.Run(RefreshLease));
        }

        private async Task RefreshLease()
        {
            if (_disposed || _device == null)
            {
                return;
            }

            _portMapping = new Mapping(Protocol.Tcp, _portMapping.PrivatePort, _portMapping.PublicPort, PortLeaseLength, _portMapping.Description);

            try
            {
                Console.WriteLine($"Forwarded {_portMapping.PublicPort}");
                await _device.CreatePortMapAsync(_portMapping);
            }
            catch (Exception)
            {
                // Just ignore it
            }

            _ = Task.Delay(PortLeaseRenew * 1000, _disposedCancellation.Token).ContinueWith((task) => Task.Run(RefreshLease));
        }

        public void Dispose()
        {
            if (!_disposed && _portMapping != null && _disposedCancellation != null)
            {
                _disposed = true;
                _disposedCancellation.Cancel();

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await _device.DeletePortMapAsync(_portMapping);
                    }
                    catch (Exception)
                    {

                    }
                });

                task.Wait(500);
            }
        }
    }
}
