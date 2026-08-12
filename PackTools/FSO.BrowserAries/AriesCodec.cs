using System.Buffers.Binary;
using System.Text;

namespace FSO.BrowserAries;

/// <summary>12-byte LE Aries header + payload (AriesProtocolEncoder).</summary>
public static class AriesCodec
{
    public const uint Voltron = 0;
    public const uint Electron = 1000;
    public const uint RequestClientSessionResponse = 21;
    public const uint RequestClientSession = 22;
    public const uint RequestClientSessionArchive = 2000;

    public static byte[] Frame(uint type, ReadOnlySpan<byte> payload)
    {
        var frame = new byte[12 + payload.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(0), type);
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(8), (uint)payload.Length);
        payload.CopyTo(frame.AsSpan(12));
        return frame;
    }

    public static byte[] VoltronStyleInner(ushort subtype, ReadOnlySpan<byte> body)
    {
        var inner = new byte[6 + body.Length];
        BinaryPrimitives.WriteUInt16BigEndian(inner.AsSpan(0), subtype);
        BinaryPrimitives.WriteUInt32BigEndian(inner.AsSpan(2), (uint)(6 + body.Length));
        body.CopyTo(inner.AsSpan(6));
        return inner;
    }

    public static byte[] VoltronFrame(ushort subtype, ReadOnlySpan<byte> body) =>
        Frame(Voltron, VoltronStyleInner(subtype, body));

    public static byte[] ElectronFrame(ushort subtype, ReadOnlySpan<byte> body) =>
        Frame(Electron, VoltronStyleInner(subtype, body));

    public static byte[] EncodeVlcString(string s)
    {
        var data = Encoding.UTF8.GetBytes(s ?? "");
        var n = data.Length;
        var lenBytes = new List<byte>();
        var first = true;
        while (n > 0 || first)
        {
            lenBytes.Add((byte)((n > 127 ? 0x80 : 0) | (n & 0x7f)));
            n >>= 7;
            first = false;
        }
        var outBytes = new byte[lenBytes.Count + data.Length];
        lenBytes.CopyTo(outBytes);
        data.CopyTo(outBytes, lenBytes.Count);
        return outBytes;
    }

    public static byte[] EncodeFixedAscii(string s, int fieldSize)
    {
        var outBytes = new byte[fieldSize];
        var src = Encoding.ASCII.GetBytes(s ?? "");
        Buffer.BlockCopy(src, 0, outBytes, 0, Math.Min(src.Length, fieldSize));
        return outBytes;
    }

    public static byte[] Concat(params byte[][] parts)
    {
        var n = 0;
        foreach (var p in parts) n += p.Length;
        var outBytes = new byte[n];
        var o = 0;
        foreach (var p in parts)
        {
            Buffer.BlockCopy(p, 0, outBytes, o, p.Length);
            o += p.Length;
        }
        return outBytes;
    }

    /// <summary>Archive city type 21 (Unknown=40, PascalVLC password).</summary>
    public static byte[] EncodeArchiveSessionResponse(string user, string password)
    {
        var mid = new byte[8];
        mid[2] = 40;
        BinaryPrimitives.WriteUInt16LittleEndian(mid.AsSpan(6), 4);
        return Concat(
            EncodeFixedAscii(user, 112),
            EncodeFixedAscii("", 80),
            EncodeFixedAscii("", 40),
            EncodeFixedAscii("", 84),
            mid,
            EncodeVlcString(password));
    }

    /// <summary>Lot type 21 (Unknown=39, 32-byte ASCII ticket).</summary>
    public static byte[] EncodeLotSessionResponse(string user, string ticket)
    {
        var mid = new byte[8];
        mid[2] = 39;
        BinaryPrimitives.WriteUInt16LittleEndian(mid.AsSpan(6), 4);
        return Concat(
            EncodeFixedAscii(user, 112),
            EncodeFixedAscii("", 80),
            EncodeFixedAscii("", 40),
            EncodeFixedAscii("", 84),
            mid,
            EncodeFixedAscii(ticket, 32));
    }

    public static byte[] EncodeClientOnlineBurst()
    {
        // ClientOnline 0x000a (22 zero) + empty SetIgnoreList 0x0034 + SetInvincible 0x0036
        return Concat(
            VoltronFrame(0x000a, new byte[22]),
            VoltronFrame(0x0034, new byte[] { 0, 0 }),
            VoltronFrame(0x0036, new byte[4]));
    }

    public static byte[] EncodeLotClientOnline() => VoltronFrame(0x000a, new byte[22]);

    public static byte[] EncodeAvatarSelectRequest(uint avatarId)
    {
        var body = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(body, avatarId);
        return ElectronFrame(30, body);
    }

    public static byte[] EncodeFindLotRequest(uint lotId, bool openIfClosed = false)
    {
        var body = new byte[5];
        BinaryPrimitives.WriteUInt32BigEndian(body, lotId);
        body[4] = openIfClosed ? (byte)1 : (byte)0;
        return ElectronFrame(5, body);
    }
}
