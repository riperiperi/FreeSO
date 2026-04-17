using System;
using System.IO;
using FSO.Bot.Headless;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using Xunit;

namespace FSO.Bot.Headless.Tests;

/// <summary>
/// Golden-byte packet encoding tests for social-family VM commands
/// (freesoexperiment-9ae). Pins the wire shape of <c>VMNetChatCmd</c> (speak)
/// and re-asserts <c>VMNetInteractionCmd</c> for the directed-social case
/// (be-friendly, tell-joke, flirt, be-mean, give-gift — all share the same
/// PDU, varying only by the caller-resolved <c>Interaction</c> TTAB index).
///
/// Any accidental change to the PDU layout (field reorder, new/removed field,
/// endianness bug, string-encoding change) breaks these tests immediately.
/// </summary>
public class SocialCommandEncodingTests
{
    [Fact]
    public void Chat_EncodesExpectedBytes()
    {
        // Shape:
        //   [byte VMCommandType=Chat(4)]
        //   [uint ActorUID (LE)]
        //   [string Message (BinaryWriter 7-bit length-prefix + UTF-8 bytes)]
        //   [byte ChannelID]
        //
        // Message = "hi" (2 bytes):
        //   length prefix: 0x02 (single byte LEB128 for length 2)
        //   utf8:          'h' (0x68) 'i' (0x69)

        var body = new VMNetChatCmd
        {
            ActorUID = 0xDEADBEEF,
            Message = "hi",
            ChannelID = 0,
        };
        var bytes = SerializeVMCommand(body);

        //  1 (type) + 4 (ActorUID) + 1 (len) + 2 (utf8) + 1 (channel) = 9
        Assert.Equal(9, bytes.Length);
        Assert.Equal(4, bytes[0]); // VMCommandType.Chat = 4
        Assert.Equal(new byte[] { 0xEF, 0xBE, 0xAD, 0xDE }, bytes[1..5]);
        Assert.Equal(0x02, bytes[5]);        // string length prefix
        Assert.Equal((byte)'h', bytes[6]);
        Assert.Equal((byte)'i', bytes[7]);
        Assert.Equal(0x00, bytes[8]);        // ChannelID = 0
    }

    [Fact]
    public void Chat_NonZeroChannel_EncodesExpectedBytes()
    {
        // Verifies the ChannelID byte is carried through and the admin channel (7) encodes
        // correctly for operators that have permission. The bot's speak handler rejects
        // leading '/' from non-admin agents, but the PDU itself has no such gate —
        // VMNetChatCmd.Verify enforces permission on the server. Golden-byte pins both fields.
        var body = new VMNetChatCmd
        {
            ActorUID = 0x01020304,
            Message = "a",
            ChannelID = 7,
        };
        var bytes = SerializeVMCommand(body);
        Assert.Equal(8, bytes.Length);
        Assert.Equal(4, bytes[0]);
        Assert.Equal(new byte[] { 0x04, 0x03, 0x02, 0x01 }, bytes[1..5]);
        Assert.Equal(0x01, bytes[5]);
        Assert.Equal((byte)'a', bytes[6]);
        Assert.Equal(0x07, bytes[7]);
    }

    [Fact]
    public void DirectedSocial_EncodesAsVMNetInteractionCmd()
    {
        // All five directed socials (be-friendly, tell-joke, flirt, be-mean, give-gift) share
        // this PDU. The distinction is the Interaction value the bot resolves from the target's
        // pie menu — the WIRE shape is identical.
        //
        // Shape (same as queue-interaction, catalog row):
        //   [byte VMCommandType=Interaction(1)]
        //   [uint ActorUID (LE)]
        //   [ushort Interaction (LE)]
        //   [short  CalleeID (LE)]
        //   [short  Param0 (LE)]
        //   [bool   Global]
        //   [short  CallerID (LE)]
        // Total 14 bytes.

        var body = new VMNetInteractionCmd
        {
            ActorUID = 0xAABBCCDD,
            Interaction = 12,   // e.g. TTAB index of "Be Friendly"
            CalleeID = 17,
            Param0 = 0,
            Global = false,
            CallerID = 5,
        };
        var bytes = SerializeVMCommand(body);
        Assert.Equal(14, bytes.Length);
        Assert.Equal(1, bytes[0]);                                    // VMCommandType.Interaction
        Assert.Equal(new byte[] { 0xDD, 0xCC, 0xBB, 0xAA }, bytes[1..5]);
        Assert.Equal(new byte[] { 0x0C, 0x00 }, bytes[5..7]);         // Interaction = 12
        Assert.Equal(new byte[] { 0x11, 0x00 }, bytes[7..9]);         // CalleeID = 17
        Assert.Equal(new byte[] { 0x00, 0x00 }, bytes[9..11]);        // Param0
        Assert.Equal(0, bytes[11]);                                    // Global = false
        Assert.Equal(new byte[] { 0x05, 0x00 }, bytes[12..14]);       // CallerID = 5
    }

    private static byte[] SerializeVMCommand(VMNetCommandBodyAbstract body)
    {
        using var ms = new MemoryStream();
        using (var bw = new BinaryWriter(ms))
        {
            new VMNetCommand(body).SerializeInto(bw);
        }
        return ms.ToArray();
    }
}
