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
            shellInstrumentation = new ShellInstrumentation(operationsCapturing);
        }

        [Fact]
        public void ReadyPropertyTest()
        {

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
