using System;
using FluentAssertions;
using NSubstitute;
using Ssh.Net.Instrumentation.Details;
using Xunit;

namespace Ssh.Net.Instrumentation.UnitTests
{
    public class ShellInstrumentationTests
    {
        private readonly IShellOperationCapturing operationsCapturing;
        private readonly ShellInstrumentation shellInstrumentation;

        public ShellInstrumentationTests()
        {
            operationsCapturing = Substitute.For<IShellOperationCapturing>();
            operationsCapturing.IsReady.Returns(true, false);
            operationsCapturing.GetCurrentPromptInfo().Returns(new ShellPromptInfo(1,2,"~"));
            operationsCapturing.WaitForReady().Returns(true);
            operationsCapturing.WaitForReady(1000).Returns(true);
            operationsCapturing.WaitForReady(TimeSpan.FromMilliseconds(500)).Returns(true);

            shellInstrumentation = new ShellInstrumentation(operationsCapturing);
        }

        [Fact]
        public void ReadyPropertyTest()
        {
            shellInstrumentation.IsReady.Should().BeTrue();
            shellInstrumentation.IsReady.Should().BeFalse();
            var _ = operationsCapturing.Received(2).IsReady;
        }

        [Fact]
        public void WaitForReadyTest()
        {
            shellInstrumentation.WaitForReady();
            shellInstrumentation.WaitForReady(1000).Should().BeTrue();
            shellInstrumentation.WaitForReady(TimeSpan.FromMilliseconds(500)).Should().BeTrue();

   
            operationsCapturing.Received(1).WaitForReady();
            operationsCapturing.Received(1).WaitForReady(1000);
            operationsCapturing.Received(1).WaitForReady(TimeSpan.FromMilliseconds(500));
        }

        [Fact]
        public void PromptEnterTest()
        {
            shellInstrumentation.PromptEnter("<command>");

            operationsCapturing.Received(1).PromptEnter("<command>");
        }


        [Fact]
        public void GetPromptInfoTest()
        {
            var promptInfo = shellInstrumentation.GetCurrentPromptInfo();
            promptInfo.LastCommandNumber.Should().Be(1);
            promptInfo.LastExitCode.Should().Be(2);
            promptInfo.CurrentDirectory.Should().Be("~");
            operationsCapturing.Received(1).GetCurrentPromptInfo();
        }

        [Fact]
        public void DisposeTest()
        {
            shellInstrumentation.Dispose();
            shellInstrumentation.Dispose();

            shellInstrumentation.IsDisposed.Should().BeTrue();
            operationsCapturing.Received(1).Dispose();
        }
    }
}
