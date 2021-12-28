using System;
using FluentAssertions;
using Ssh.Net.Instrumentation.Details;
using Xunit;

namespace Ssh.Net.Instrumentation.UnitTests
{
    public class ShellOperationCapturingPromptReadinessTests
    {
        [Fact]
        public void PromptReadyTest()
        {
            using var shellStream = new TestShellStream();

            shellStream.ProvideReaderInput(new[] { TestShellStream.CorrectPrompt });

            using var operationsCapturing = new ShellOperationCapturing(shellStream, new ShellInstrumentationConfig());

            operationsCapturing.IsReady.Should().BeTrue();
            operationsCapturing.IsDisposed.Should().BeFalse();
            shellStream.DisposeCalled.Should().Be(0);
        }

        [Fact]
        public void NoPromptReceivedTest()
        {
            using var shellStream = new TestShellStream();

            Action act = () =>
            {
                using var operationsCapturing =
                    new ShellOperationCapturing(shellStream, new ShellInstrumentationConfig());
            };

            act.Should().Throw<TimeoutException>()
                .WithMessage("Shell not entering ready state");

        }

        [Fact]
        public void PromptNotOpenTest()
        {
            using var shellStream = new TestShellStream();
            shellStream.ProvideReaderInput(new[] { $"{TestShellStream.CorrectPrompt} something behind open prompt" });

            Action act = () =>
            {
                using var operationsCapturing =
                    new ShellOperationCapturing(shellStream, new ShellInstrumentationConfig());
            };

            act.Should().Throw<TimeoutException>()
                .WithMessage("Shell not entering ready state");

        }

    }
}