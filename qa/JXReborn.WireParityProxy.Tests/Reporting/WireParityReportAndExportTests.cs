using System.Buffers.Binary;
using JXReborn.Adapter.Legacy.Packets;
using JXReborn.TestBase.WireCaptures;
using JXReborn.WireParityProxy.Configuration;
using JXReborn.WireParityProxy.Diffing;
using JXReborn.WireParityProxy.Proxy;
using JXReborn.WireParityProxy.Reporting;
using JXReborn.WireParityProxy.Wire;
using Xunit;

namespace JXReborn.WireParityProxy.Tests.Reporting;

public sealed class WireParityReportAndExportTests
{
    [Fact]
    public void WriteAllAndExportL3aCapture_ShouldProduceReportsAndSha256VerifiedCaptureFile()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"wpp-test-{Guid.NewGuid():N}");
        try
        {
            var options = new WireParityProxyOptions { ReportOutputDir = tempDir };
            var session = new WireParitySession("HeroReport01", options);
            DateTimeOffset now = DateTimeOffset.UtcNow;

            byte[] world53 = new byte[5];
            world53[0] = ProtocolOpcodes.s2c_syncworld;
            BinaryPrimitives.WriteInt32LittleEndian(world53.AsSpan(1, 4), 53);
            session.PrimaryState.ApplyS2c(world53, now);
            session.ShadowState.ApplyS2c(world53, now);

            var c2s = new JxWireFrame(ProtocolOpcodes.c2s_npcwalk, new byte[13] { ProtocolOpcodes.c2s_npcwalk, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, now, 1);
            var window = new WireActionWindow(1, "Step001_Walk", c2s, c2s.DecryptedPayload, false, null);

            byte[] moneyPkt = new byte[5];
            moneyPkt[0] = ProtocolOpcodes.s2c_syncmoney;
            BinaryPrimitives.WriteInt32LittleEndian(moneyPkt.AsSpan(1, 4), 1234);
            window.AddPrimaryS2c(new JxWireFrame(ProtocolOpcodes.s2c_syncmoney, moneyPkt, now.AddMilliseconds(55), 1));
            window.AddShadowS2c(new JxWireFrame(ProtocolOpcodes.s2c_syncmoney, moneyPkt, now.AddMilliseconds(60), 1));

            WireStepReport step = session.DiffCoordinator.CompleteActionWindow(window, session.PrimaryState, session.ShadowState);
            Assert.True(step.Passed);

            string reportDir = WireParityReportWriter.WriteAll(session, tempDir);
            Assert.True(File.Exists(Path.Combine(reportDir, "findings.jsonl")));
            Assert.True(File.Exists(Path.Combine(reportDir, "report.md")));
            Assert.True(File.Exists(Path.Combine(reportDir, "report.html")));

            string capturePath = L3aCaptureExporter.ExportActionWindow(session, stepIndex: 1, captureId: "WPP_Test_S001", outputDirectory: tempDir);
            Assert.True(File.Exists(capturePath));

            WireCaptureDocument loaded = WireCaptureStore.LoadAndVerify("WPP_Test_S001", tempDir);
            Assert.Equal("WPP_Test_S001", loaded.Metadata.CaptureId);
            Assert.Single(loaded.Steps);
            Assert.Single(loaded.Steps[0].ExpectedS2cContract);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
