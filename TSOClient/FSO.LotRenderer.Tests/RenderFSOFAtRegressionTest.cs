// Regression tests for freesoexperiment-499 (mode-leak) and freesoexperiment-2a1 (bigThumb texture-leak).
//
// Both bugs are in TSOClient/FSO.LotRenderer/Program.cs RenderFSOFAt.
//
// 499: GraphicsModeControl switched to Full2D at line ~457 but not in a try/finally.
//      Any exception between ChangeMode(Full2D) and ChangeMode(Full3D) permanently leaved
//      the process in Full2D, poisoning all subsequent RenderFSOF / RenderFSOFAt calls.
//
// 2a1: GetLotThumbAt (non-roofless path) returns a GPU RenderTarget2D stored in `bigThumb`.
//      Only the Decimate output (tex) was disposed; bigThumb leaked one RT per cache-miss render.
//
// Test strategy (bugfix depth):
//   - 499: call RenderFSOFAt with a valid (empty) marshal but null GraphicsDevice.
//         The null GD triggers NullReferenceException at gd.SetRenderTarget(null),
//         which is AFTER ChangeMode(Full2D) and BEFORE ChangeMode(Full3D).
//         Pre-fix: mode stays Full2D after the exception. Post-fix: finally block restores Full3D.
//
//   - 2a1: structural source verification via File.ReadAllText — confirms bigThumb.Dispose()
//         is present in Program.cs. A live GPU test would require real game assets; that evidence
//         is provided by the binary-run RSS hammer in the MANDATORY BINARY-RUN VERIFICATION GATE.
//         This test verifies the fix is committed and not accidentally reverted.
//
// Run with: dotnet test TSOClient/FSO.LotRenderer.Tests --filter "RegressionTest_499_2a1"

using System;
using System.IO;
using FSO.Common;
using FSO.LotView;
using FSO.LotView.Model;
using Xunit;

namespace FSO.LotRenderer.Tests
{
    /// <summary>
    /// Regression tests for freesoexperiment-499 (mode-leak) and freesoexperiment-2a1 (bigThumb texture-leak).
    /// Both bugs are in Program.RenderFSOFAt.
    /// </summary>
    public class RenderFSOFAtRegressionTest
    {
        /// <summary>
        /// 499 regression: RenderFSOFAt must restore GraphicsModeControl to Full3D even when
        /// an exception is thrown inside the Full2D block.
        ///
        /// Pre-fix code had no try/finally — an exception between ChangeMode(Full2D) and
        /// ChangeMode(Full3D) left the process permanently in Full2D.
        ///
        /// This test passes a null GraphicsDevice, which causes NullReferenceException at
        /// gd.SetRenderTarget(null) — the first statement inside the try block, AFTER the
        /// ChangeMode(Full2D) call.  Post-fix: the finally block fires and mode is Full3D.
        /// Pre-fix: the exception propagates without restoring mode; mode stays Full2D.
        /// </summary>
        [Fact]
        public void RenderFSOFAt_ExceptionInFull2DBlock_RestoresFull3DMode_Regression499()
        {
            // Enable3D must be true; otherwise ChangeMode is a no-op and the test proves nothing.
            var savedEnable3D = FSOEnvironment.Enable3D;
            FSOEnvironment.Enable3D = true;

            // Prime mode to Full3D so we have a clear before/after.
            GraphicsModeControl.ChangeMode(GlobalGraphicsMode.Full3D);
            Assert.Equal(GlobalGraphicsMode.Full3D, GraphicsModeControl.Mode);

            try
            {
                // An empty byte array: VMMarshal.Deserialize reads the "FSOv" magic
                // and returns early (no exception) when it doesn't match — so the marshal
                // is left at default/empty.  The exception we care about fires later at
                // gd.SetRenderTarget(null) when gd is null.
                var emptyFsov = new byte[8]; // all zeros — "FSOv" magic won't match; Deserialize returns early.

                // Should throw NullReferenceException from gd.SetRenderTarget(null).
                // We don't care about the exception type — only that mode is restored afterward.
                var ex = Record.Exception(() =>
                    Program.RenderFSOFAt(
                        fsov:        emptyFsov,
                        gd:          null,       // triggers NRE after ChangeMode(Full2D)
                        level:       1,
                        rotation:    FSO.LotView.WorldRotation.TopLeft,
                        zoom:        FSO.LotView.WorldZoom.Far,
                        thumbAction: null,
                        roofless:    false));

                // The exception must have been thrown (null gd → NRE).
                Assert.NotNull(ex);

                // KEY ASSERTION: mode must be Full3D after the exception.
                // Pre-fix: Full2D (mode never restored). Post-fix: Full3D (finally block ran).
                Assert.Equal(GlobalGraphicsMode.Full3D, GraphicsModeControl.Mode);
            }
            finally
            {
                // Restore Enable3D to whatever it was before this test.
                FSOEnvironment.Enable3D = savedEnable3D;
                // Restore mode to Full3D regardless, so other tests in the run aren't poisoned.
                if (savedEnable3D)
                    GraphicsModeControl.ChangeMode(GlobalGraphicsMode.Full3D);
            }
        }

        /// <summary>
        /// 2a1 regression: bigThumb.Dispose() must be present in Program.cs RenderFSOFAt's
        /// non-roofless path.
        ///
        /// Pre-fix code only disposed `tex` (the Decimate output); `bigThumb` (the GPU
        /// RenderTarget2D returned by GetLotThumbAt) was never disposed, leaking one GPU
        /// render target per cache-miss render.
        ///
        /// A live GPU texture-disposal test requires real game assets and a GraphicsDevice —
        /// evidence for that is the binary-run RSS hammer in the mandatory binary verification gate.
        /// This test guards against accidental reversion: it fails immediately if bigThumb.Dispose()
        /// is removed from Program.cs.
        /// </summary>
        [Fact]
        public void RenderFSOFAt_NonRooflessPath_BigThumbDispose_PresentInSource_Regression2a1()
        {
            // Locate Program.cs relative to this test assembly's location.
            // The test project has a ProjectReference to FSO.LotRenderer, so they share the same
            // TSOClient/ tree.  Walk up from the test assembly to find the source.
            var testDir = Path.GetDirectoryName(typeof(RenderFSOFAtRegressionTest).Assembly.Location)!;
            string programCsPath = null;
            var dir = new System.IO.DirectoryInfo(testDir);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "FSO.LotRenderer", "Program.cs");
                if (File.Exists(candidate))
                {
                    programCsPath = candidate;
                    break;
                }
                dir = dir.Parent;
            }

            Assert.True(programCsPath != null && File.Exists(programCsPath),
                $"Could not locate FSO.LotRenderer/Program.cs searching from {testDir}. " +
                "Run dotnet build from the TSOClient root to ensure the source tree is intact.");

            var source = File.ReadAllText(programCsPath);

            // The fix for 2a1 adds bigThumb.Dispose() in the non-roofless else-branch.
            // We check for the comment tag AND the dispose call so an accidental revert
            // (comment stays, Dispose removed) still fails the assertion.
            Assert.Contains("bigThumb.Dispose()", source,
                StringComparison.Ordinal);

            // Also assert the fix comment references freesoexperiment-2a1 so the
            // traceability is preserved and can't be silently dropped.
            Assert.Contains("freesoexperiment-2a1", source,
                StringComparison.Ordinal);
        }
    }
}
