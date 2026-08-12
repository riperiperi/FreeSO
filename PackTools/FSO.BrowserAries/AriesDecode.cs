using System.Buffers.Binary;

namespace FSO.BrowserAries;

public readonly struct AriesFrame
{
    public uint Type { get; init; }
    public byte[] Payload { get; init; }
}

/// <summary>Reassembles Aries frames from a TCP/WS byte stream.</summary>
public sealed class AriesFramer
{
    private byte[] _buf = Array.Empty<byte>();

    public IEnumerable<AriesFrame> Push(ReadOnlySpan<byte> chunk)
    {
        var merged = new byte[_buf.Length + chunk.Length];
        Buffer.BlockCopy(_buf, 0, merged, 0, _buf.Length);
        chunk.CopyTo(merged.AsSpan(_buf.Length));
        _buf = merged;

        var frames = new List<AriesFrame>();
        while (_buf.Length >= 12)
        {
            var size = BinaryPrimitives.ReadUInt32LittleEndian(_buf.AsSpan(8));
            var need = 12 + (int)size;
            if (_buf.Length < need) break;

            var type = BinaryPrimitives.ReadUInt32LittleEndian(_buf.AsSpan(0));
            var payload = new byte[size];
            Buffer.BlockCopy(_buf, 12, payload, 0, (int)size);
            frames.Add(new AriesFrame { Type = type, Payload = payload });

            var rest = new byte[_buf.Length - need];
            Buffer.BlockCopy(_buf, need, rest, 0, rest.Length);
            _buf = rest;
        }
        return frames;
    }
}

public static class AriesDecode
{
    public static bool TryVoltronSubtype(ReadOnlySpan<byte> payload, out ushort subtype)
    {
        subtype = 0;
        if (payload.Length < 6) return false;
        subtype = BinaryPrimitives.ReadUInt16BigEndian(payload);
        return true;
    }

    public static bool TryReadPascalVlc(ReadOnlySpan<byte> data, ref int o, out string value)
    {
        value = "";
        if (o >= data.Length) return false;
        int len = 0, shift = 0;
        byte b;
        do
        {
            if (o >= data.Length) return false;
            b = data[o++];
            len |= (b & 0x7f) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);

        if (o + len > data.Length) return false;
        value = System.Text.Encoding.UTF8.GetString(data.Slice(o, len));
        o += len;
        return true;
    }

    public static bool TryDecodeFindLotResponse(ReadOnlySpan<byte> electronPayload,
        out ushort status, out uint lotId, out string ticket, out string address, out string user)
    {
        status = 0; lotId = 0; ticket = address = user = "";
        if (!TryVoltronSubtype(electronPayload, out var subtype) || subtype != 6) return false;
        var body = electronPayload.Slice(6);
        if (body.Length < 6) return false;
        status = BinaryPrimitives.ReadUInt16BigEndian(body);
        lotId = BinaryPrimitives.ReadUInt32BigEndian(body.Slice(2));
        var o = 6;
        return TryReadPascalVlc(body, ref o, out ticket)
            && TryReadPascalVlc(body, ref o, out address)
            && TryReadPascalVlc(body, ref o, out user);
    }

    /// <summary>
    /// Electron ArchiveAvatarSelectResponse (subtype 31): BE u16 code
    /// (<c>ArchiveAvatarSelectCode.Success = 0</c>).
    /// </summary>
    public static bool TryDecodeAvatarSelectResponse(ReadOnlySpan<byte> electronPayload, out ushort code)
    {
        code = 0;
        if (!TryVoltronSubtype(electronPayload, out var subtype) || subtype != 31) return false;
        var body = electronPayload.Slice(6);
        if (body.Length < 2) return false;
        code = BinaryPrimitives.ReadUInt16BigEndian(body);
        return true;
    }

    public static string? TryDecodeArchiveHandshakeName(ReadOnlySpan<byte> payload)
    {
        var o = 0;
        return TryReadPascalVlc(payload, ref o, out var name) ? name : null;
    }
}
