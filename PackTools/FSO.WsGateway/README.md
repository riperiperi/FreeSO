# FSO.WsGateway

WS↔TCP byte bridge for FreeSO Archive city (33101) and lot (34101). Serves an
Aries protocol debugger at `wwwroot/`.

## Run against fakes (full join handshake)

```sh
# terminal 1
python3 PackTools/FSO.WsGateway/tools/fake-city-server.py 33101

# terminal 2
python3 PackTools/FSO.WsGateway/tools/fake-lot-server.py 34101

# terminal 3
dotnet run --project PackTools/FSO.WsGateway -- --listen http://127.0.0.1:8087
```

Open http://127.0.0.1:8087 → **Connect /city**. The page walks:

city handshake → type 21 → HostOnline → ClientOnline → avatar select → FindLot  
→ opens `/lot` → type 22 → ticket type 21 → HostOnline → ClientOnline → empty VM tick.

## Run against live Archive

Start FreeSO hosting (Quick Start). Point the gateway at the same ports (defaults).
Live city auth still needs a valid RSA PKCS#1 token (page ships BigInt encrypt).

## Tests

```sh
dotnet test PackTools/FSO.WsGateway.Tests
```
