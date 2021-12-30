using System;
using System.Collections;
using System.Text;
using FluentAssertions;
using FluentAssertions.Collections;
using NSubstitute;
using Ssh.Net.Instrumentation.Details;
using Xunit;

namespace Ssh.Net.Instrumentation.UnitTests
{
    public class ShellOperationCapturingTests: IDisposable
    {

        private readonly TestShellStream shellStream;
        private readonly IShellOperationCapturing operationsCapturing;


        public ShellOperationCapturingTests()
        {
            shellStream = new TestShellStream();
            
            shellStream.ProvideReaderInput(new [] {TestShellStream.CorrectPrompt});
            
            operationsCapturing = new ShellOperationCapturing(shellStream, new ShellInstrumentationConfig()
            {
                ShellPromptReadyWaitTime = TimeSpan.FromMilliseconds(10)
            });

            shellStream.WrittenLines.Count.Should().Be(1);
            shellStream.WrittenLines.Should().Contain(new[]
                { "PROMPT_COMMAND='RET=$?;\\\r\nexport PS1=\"\\r\\n|<<SHELL PROMPT>>|\\#|$RET|\\w|> \";'" });

            shellStream.WrittenLines.Clear();
        }

        [Fact]
        public void ConstructionTest()
        {
            operationsCapturing.IsReady.Should().BeTrue();
            operationsCapturing.IsDisposed.Should().BeFalse();
            shellStream.DisposeCalled.Should().Be(0);
        }


        [Fact]
        public void NotReadyTest()
        {
            operationsCapturing.PromptEnter(string.Empty);

            operationsCapturing.IsReady.Should().BeFalse();
            operationsCapturing.WaitForReady(500,0).Should().BeFalse();
        }

        [Fact]
        public void NotReadyAndReadyTest()
        {
            operationsCapturing.PromptEnter(string.Empty);
            operationsCapturing.IsReady.Should().BeFalse();

            shellStream.ProvideReaderInput(new[] { TestShellStream.CorrectPrompt });

            operationsCapturing.WaitForReady(500, 0).Should().BeTrue();
            operationsCapturing.IsReady.Should().BeTrue();
        }

        [Fact]
        public void PromptEnterTest()
        {
            operationsCapturing.PromptEnter("LINE1");
            operationsCapturing.PromptEnter("LINE2");
            operationsCapturing.PromptEnter("LINE3");
            shellStream.WrittenLines.Should().ContainInOrder(new[] {"LINE1" , "LINE2", "LINE3"});
        }

        [Fact]
        public void DisposeTest()
        {
            operationsCapturing.Dispose();
            operationsCapturing.IsDisposed.Should().BeTrue();
            shellStream.DisposeCalled.Should().Be(1);
        }

        public void Dispose()
        {
            operationsCapturing?.Dispose();
            shellStream?.Dispose();
        }
    }
}
