using FSO.BrowserAries;
using Xunit;

namespace FSO.BrowserAries.Tests;

public class AriesCodecTests
{
    [Fact]
    public void Frame_HasLittleEndianHeader()
    {
        var payload = new byte[] { 1, 2, 3 };
        var frame = AriesCodec.Frame(2000, payload);
        Assert.Equal(15, frame.Length);
        Assert.Equal(2000u, BitConverter.ToUInt32(frame, 0));
        Assert.Equal(3u, BitConverter.ToUInt32(frame, 8));
        Assert.Equal(new byte[] { 1, 2, 3 }, frame.AsSpan(12).ToArray());
    }

    [Fact]
    public void Framer_ReassemblesSplitFrames()
    {
        var a = AriesCodec.Frame(22, Array.Empty<byte>());
        var b = AriesCodec.VoltronFrame(0x001e, new byte[] { 0, 0, 0x7f, 0xff, 0x10, 0x00 });
        var framer = new AriesFramer();
        var part1 = a.AsSpan(0, 8).ToArray();
        var part2 = new byte[a.Length - 8 + b.Length];
        Buffer.BlockCopy(a, 8, part2, 0, a.Length - 8);
        Buffer.BlockCopy(b, 0, part2, a.Length - 8, b.Length);

        Assert.Empty(framer.Push(part1));
        var frames = framer.Push(part2).ToList();
        Assert.Equal(2, frames.Count);
        Assert.Equal(22u, frames[0].Type);
        Assert.Equal(0u, frames[1].Type);
        Assert.True(AriesDecode.TryVoltronSubtype(frames[1].Payload, out var sub));
        Assert.Equal(0x001e, sub);
    }

    [Fact]
    public void LotSession_Unknown39_Length356()
    {
        var payload = AriesCodec.EncodeLotSessionResponse("1", "demo-ticket");
        Assert.Equal(356, payload.Length);
        Assert.Equal(39, payload[318]);
        Assert.Equal((byte)'d', payload[324]);
    }

    [Fact]
    public void ArchiveSession_Unknown40_UsesVlcPassword()
    {
        var payload = AriesCodec.EncodeArchiveSessionResponse("BrowserDemo", "pw");
        Assert.Equal(40, payload[318]);
        Assert.Equal(324 + 1 + 2, payload.Length); // VLC len byte + "pw"
        Assert.Equal(2, payload[324]);
    }

    [Fact]
    public void DecodeFindLot_RoundTripsFakeCityBody()
    {
        // status=0, lot=1, ticket/address/user via VLC
        var body = AriesCodec.Concat(
            new byte[] { 0, 0 }, // status
            new byte[] { 0, 0, 0, 1 }, // lotId
            AriesCodec.EncodeVlcString("demo-ticket"),
            AriesCodec.EncodeVlcString("127.0.0.1:34101"),
            AriesCodec.EncodeVlcString("1"));
        var electron = AriesCodec.VoltronStyleInner(6, body);
        Assert.True(AriesDecode.TryDecodeFindLotResponse(electron, out var status, out var lot, out var ticket, out var addr, out var user));
        Assert.Equal(0, status);
        Assert.Equal(1u, lot);
        Assert.Equal("demo-ticket", ticket);
        Assert.Equal("127.0.0.1:34101", addr);
        Assert.Equal("1", user);
    }

    [Fact]
    public void DecodeAvatarSelect_SuccessAndFailure()
    {
        var ok = AriesCodec.VoltronStyleInner(31, new byte[] { 0, 0 });
        Assert.True(AriesDecode.TryDecodeAvatarSelectResponse(ok, out var code));
        Assert.Equal(0, code);

        var inUse = AriesCodec.VoltronStyleInner(31, new byte[] { 0, 4 }); // InUse
        Assert.True(AriesDecode.TryDecodeAvatarSelectResponse(inUse, out code));
        Assert.Equal(4, code);

        var wrongSubtype = AriesCodec.VoltronStyleInner(30, new byte[] { 0, 0 });
        Assert.False(AriesDecode.TryDecodeAvatarSelectResponse(wrongSubtype, out _));
    }
}
