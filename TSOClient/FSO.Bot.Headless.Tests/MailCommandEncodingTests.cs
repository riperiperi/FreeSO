using FSO.Bot.Headless;
using FSO.Common.Serialization;
using FSO.Files.Formats.tsodata;
using FSO.Server.Protocol.Electron;
using FSO.Server.Protocol.Electron.Packets;
using Mina.Core.Buffer;
using Xunit;

namespace FSO.Bot.Headless.Tests;

/// <summary>
/// Golden-byte / round-trip encoding tests for mail-family PDUs
/// (freesoexperiment-bd2). Pins the wire shape: any accidental change to the
/// MailRequest / MessageItem layout (field reorder, endian bug, added field)
/// breaks these tests.
///
/// MailRequest is an Electron packet, so we serialize via Mina IoBuffer with
/// BigEndian order (per AbstractElectronPacket.Allocate). For POLL_INBOX and
/// DELETE variants we assert exact bytes (the wire is just an enum byte +
/// int64). For SEND we round-trip MessageItem (which is TSO tsodata format
/// with Pascal strings) because the binary shape is not obvious by inspection.
/// </summary>
public class MailCommandEncodingTests
{
    [Fact]
    public void MailRequest_PollInbox_EncodesExpectedBytes()
    {
        // Shape (PutEnum is UInt16 even for a byte-backed enum — IoBufferUtils.cs:129):
        //   [uint16 Type = POLL_INBOX (0), BigEndian]
        //   [int64 TimestampID, BigEndian]
        // Total: 10 bytes.
        var req = new MailRequest
        {
            Type = MailRequestType.POLL_INBOX,
            TimestampID = 0x0102030405060708L,
        };
        var bytes = SerializeElectron(req);
        Assert.Equal(10, bytes.Length);
        // Type (BE UInt16) = 0 = POLL_INBOX.
        Assert.Equal(new byte[] { 0x00, 0x00 }, bytes[0..2]);
        // BigEndian int64: MSB first.
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 }, bytes[2..10]);
    }

    [Fact]
    public void MailRequest_PollInbox_ZeroSince_EncodesAllZeros()
    {
        // Regression: the default (since=0) is the session-start full-inbox poll;
        // make sure the 8-byte zero tail is encoded, not dropped.
        var req = new MailRequest
        {
            Type = MailRequestType.POLL_INBOX,
            TimestampID = 0L,
        };
        var bytes = SerializeElectron(req);
        Assert.Equal(10, bytes.Length);
        for (int i = 0; i < 10; i++) Assert.Equal(0x00, bytes[i]);
    }

    [Fact]
    public void MailRequest_Delete_EncodesExpectedBytes()
    {
        // Shape identical to POLL_INBOX but Type=DELETE(2) and TimestampID carries
        // the fso_inbox.message_id, not a timestamp. Type byte goes in the low byte
        // of the BE UInt16 enum slot (high byte 0x00).
        var req = new MailRequest
        {
            Type = MailRequestType.DELETE,
            TimestampID = 42L,
        };
        var bytes = SerializeElectron(req);
        Assert.Equal(10, bytes.Length);
        // Type (BE UInt16) = 2 = DELETE.
        Assert.Equal(new byte[] { 0x00, 0x02 }, bytes[0..2]);
        // int64 42 BigEndian → 7 zero bytes then 0x2A.
        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2A }, bytes[2..10]);
    }

    [Fact]
    public void MailRequest_Send_RoundTrips()
    {
        // SEND variant serializes MessageItem into a length-prefixed blob. Round-trip
        // rather than asserting raw bytes (Pascal string encoding, magic header,
        // etc. is verified by MessageItem.Save/Read).
        var original = new MailRequest
        {
            Type = MailRequestType.SEND,
            Item = new MessageItem
            {
                SenderID = 2,
                TargetID = 4,
                Subject = "hi Kelly",
                Body = "just saying hello",
                SenderName = "baron",
                Type = 0,
                Subtype = 0,
                Time = 0x0011223344556677L,
                ReadState = 0,
                ReplyID = null,
            },
        };
        var bytes = SerializeElectron(original);
        // Type field (BE UInt16) = 1 = SEND.
        Assert.Equal(new byte[] { 0x00, 0x01 }, bytes[0..2]);

        // Round-trip: deserialize from the bytes and compare.
        var roundTripped = DeserializeMailRequest(bytes);
        Assert.Equal(MailRequestType.SEND, roundTripped.Type);
        Assert.NotNull(roundTripped.Item);
        Assert.Equal(original.Item.SenderID, roundTripped.Item.SenderID);
        Assert.Equal(original.Item.TargetID, roundTripped.Item.TargetID);
        Assert.Equal(original.Item.Subject, roundTripped.Item.Subject);
        Assert.Equal(original.Item.Body, roundTripped.Item.Body);
        Assert.Equal(original.Item.SenderName, roundTripped.Item.SenderName);
        Assert.Equal(original.Item.Type, roundTripped.Item.Type);
        Assert.Equal(original.Item.Subtype, roundTripped.Item.Subtype);
        Assert.Equal(original.Item.Time, roundTripped.Item.Time);
        Assert.Equal(original.Item.ReadState, roundTripped.Item.ReadState);
        Assert.Equal(original.Item.ReplyID, roundTripped.Item.ReplyID);
    }

    [Fact]
    public void MailRequest_Send_NullItem_EmitsZeroLengthBlob()
    {
        // When Item is null, serializer writes length=0 and no payload bytes.
        // Shape: [uint16 1 BE][int32 0 BE] = 6 bytes.
        var req = new MailRequest
        {
            Type = MailRequestType.SEND,
            Item = null,
        };
        var bytes = SerializeElectron(req);
        Assert.Equal(6, bytes.Length);
        Assert.Equal(new byte[] { 0x00, 0x01 }, bytes[0..2]);
        // int32 0 BigEndian
        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00 }, bytes[2..6]);
    }

    [Fact]
    public void MailHandlers_BodyCap_Is1500()
    {
        // Lock the client-side pre-wire cap: agents need to see a consistent
        // value, and the server's SanitizeBB+Substring(0,1500) should match.
        Assert.Equal(1500, MailHandlers.MaxBodyChars);
    }

    [Fact]
    public void MailHandlers_SubjectCap_Is128()
    {
        Assert.Equal(128, MailHandlers.MaxSubjectChars);
    }

    // ---- helpers ----

    private static byte[] SerializeElectron(AbstractElectronPacket pkt)
    {
        // Allocate generously; Mina auto-grows. Use the same order the
        // AbstractElectronPacket.Allocate helper does — BigEndian.
        var buf = IoBuffer.Allocate(8192);
        buf.Order = ByteOrder.BigEndian;
        pkt.Serialize(buf, new SerializationContext(null, null));
        buf.Flip();
        var bytes = new byte[buf.Remaining];
        for (int i = 0; i < bytes.Length; i++) bytes[i] = buf.Get();
        return bytes;
    }

    private static MailRequest DeserializeMailRequest(byte[] bytes)
    {
        var buf = IoBuffer.Wrap(bytes);
        buf.Order = ByteOrder.BigEndian;
        var req = new MailRequest();
        req.Deserialize(buf, new SerializationContext(null, null));
        return req;
    }
}
