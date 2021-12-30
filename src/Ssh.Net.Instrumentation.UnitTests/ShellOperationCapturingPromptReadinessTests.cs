using System;
using System.Threading;
using System.Threading.Tasks;
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

            var startTask = new TaskFactory().StartNew(() =>
            {
                shellStream.ProvideReaderInput(new[] { "Shell Startup Text Line1", "Shell Startup Text Line2" });

                while (shellStream.WrittenLines.Count == 0)
                {
                    Thread.Sleep(100);
                }

                shellStream.ProvideReaderInput(new[] { TestShellStream.CorrectPrompt });
            }, TaskCreationOptions.LongRunning);


            using var operationsCapturing = new ShellOperationCapturing(shellStream, new ShellInstrumentationConfig());
            startTask.Wait();
            shellStream.WrittenLines.Count.Should().Be(1);
            shellStream.WrittenLines[0].StartsWith("PROMPT_COMMAND=").Should().BeTrue();


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