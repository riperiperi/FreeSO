using System;
using System.IO;
using System.Text.Json.Nodes;
using FSO.Bot.Headless;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using Xunit;

namespace FSO.Bot.Headless.Tests;

/// <summary>
/// Golden-byte packet encoding tests for movement-family VM commands
/// (freesoexperiment-b9c). Pins the wire shape: any accidental change to the
/// PDU layout (field reorder, new/removed field, endianness bug) breaks
/// these tests.
///
/// The full PDU on the lot Aries socket is the byte array produced by
/// <c>VMNetCommand(body).SerializeInto(writer)</c>, wrapped in
/// <c>FSOVMCommand</c>. We assert on the inner VMNetCommand bytes because the
/// FSOVMCommand wrapper only length-prefixes them.
/// </summary>
public class MovementCommandEncodingTests
{
    [Fact]
    public void WalkTo_EncodesExpectedBytes()
    {
        // Shape:
        //   [byte VMCommandType=Goto(10)]
        //   [uint ActorUID (LE)]
        //   [ushort Interaction (LE)]
        //   [short  Param0 (LE)]
        //   [short  x (LE)]
        //   [short  y (LE)]
        //   [sbyte  level]
        // Total: 14 bytes.

        var body = new VMNetGotoCmd
        {
            ActorUID = 0xAABBCCDD,
            Interaction = 4,      // "Run Here"
            Param0 = 0,
            x = 512,              // 0x0200
            y = 640,              // 0x0280
            level = 1,
        };

        var bytes = SerializeVMCommand(body);

        Assert.Equal(14, bytes.Length);
        // VMCommandType.Goto = 10
        Assert.Equal(10, bytes[0]);
        // ActorUID — little-endian uint
        Assert.Equal(new byte[] { 0xDD, 0xCC, 0xBB, 0xAA }, bytes[1..5]);
        // Interaction = 4
        Assert.Equal(new byte[] { 0x04, 0x00 }, bytes[5..7]);
        // Param0 = 0
        Assert.Equal(new byte[] { 0x00, 0x00 }, bytes[7..9]);
        // x = 512
        Assert.Equal(new byte[] { 0x00, 0x02 }, bytes[9..11]);
        // y = 640
        Assert.Equal(new byte[] { 0x80, 0x02 }, bytes[11..13]);
        // level = 1
        Assert.Equal(1, (sbyte)bytes[13]);
    }

    [Fact]
    public void CancelInteraction_EncodesExpectedBytes()
    {
        // Shape:
        //   [byte VMCommandType=InteractionCancel(7)]
        //   [uint ActorUID (LE)]
        //   [ushort ActionUID (LE)]
        // Total: 7 bytes.

        var body = new VMNetInteractionCancelCmd
        {
            ActorUID = 0x01020304,
            ActionUID = 0x4242,
        };
        var bytes = SerializeVMCommand(body);
        Assert.Equal(7, bytes.Length);
        Assert.Equal(7, bytes[0]); // InteractionCancel
        Assert.Equal(new byte[] { 0x04, 0x03, 0x02, 0x01 }, bytes[1..5]);
        Assert.Equal(new byte[] { 0x42, 0x42 }, bytes[5..7]);
    }

    [Fact]
    public void Interaction_EncodesExpectedBytes()
    {
        // Shape:
        //   [byte VMCommandType=Interaction(1)]
        //   [uint ActorUID (LE)]
        //   [ushort Interaction (LE)]
        //   [short  CalleeID (LE)]
        //   [short  Param0 (LE)]
        //   [bool   Global (1 byte)]
        //   [short  CallerID (LE)]
        // Total: 14 bytes.

        var body = new VMNetInteractionCmd
        {
            ActorUID = 0x11223344,
            Interaction = 1,
            CalleeID = 17,
            Param0 = 0,
            Global = false,
            CallerID = 123,
        };
        var bytes = SerializeVMCommand(body);
        Assert.Equal(14, bytes.Length);
        Assert.Equal(1, bytes[0]);
        Assert.Equal(new byte[] { 0x44, 0x33, 0x22, 0x11 }, bytes[1..5]);
        Assert.Equal(new byte[] { 0x01, 0x00 }, bytes[5..7]);     // Interaction
        Assert.Equal(new byte[] { 0x11, 0x00 }, bytes[7..9]);     // CalleeID=17
        Assert.Equal(new byte[] { 0x00, 0x00 }, bytes[9..11]);    // Param0
        Assert.Equal(0, bytes[11]);                                // Global=false
        Assert.Equal(new byte[] { 0x7B, 0x00 }, bytes[12..14]);   // CallerID=123
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
