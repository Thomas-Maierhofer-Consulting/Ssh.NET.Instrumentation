using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace Ssh.Net.Instrumentation.Details
{
    internal interface IShellOperationCapturing : IDisposable
    {
        public bool IsDisposed { get; }
        public bool IsReady { get; }
        public ShellPromptInfo GetCurrentPromptInfo();
        public bool WaitForReady();
        public bool WaitForReady(TimeSpan timeout);
        public bool WaitForReady(int millisecondsTimeout);
        public void Execute(string commands);
    }



    internal sealed class ShellOperationCapturing: IShellOperationCapturing
    {
        private readonly ShellInstrumentationConfig config;


        public bool IsDisposed { get; private set; }

        public bool IsReady => isReadyEvent.WaitOne(0);

        public ShellPromptInfo GetCurrentPromptInfo()
        {
            return currentPromptInfo;
        }


        private readonly ManualResetEvent isReadyEvent = new ManualResetEvent(false);
        private readonly ShellStream shellStream;
        private readonly ShellOutputReader outputReader;

        private ShellPromptInfo currentPromptInfo = new ShellPromptInfo();

        public ShellOperationCapturing(IShellStream shellStream, ShellInstrumentationConfig config)
        {
            if (shellStream == null) throw new ArgumentNullException(nameof(shellStream));
            this.config = config ?? throw new ArgumentNullException(nameof(config));

            try
            {
                shellStream.ErrorOccurred += OnErrorOccurred;
                outputReader = new ShellOutputReader(shellStream,this.config, OnNewShellOutput);

                // Setting up the shell prompt
                // The shell prompt will be forced to a newline to avoid appending to other command output
                // The shell prompt starts with a defined prefix to be detected as a prompt 
                StringBuilder shellSetup = new StringBuilder();
                shellSetup.Append($"PROMPT_COMMAND='RET=$?;\\{Constants.ShellNewlineSeparator}");
                shellSetup.Append(
                    $"export PS1=\"{Constants.ShellNewlineSeparatorEscaped}{Constants.ShellPromptPrefix}\\#{Constants.FieldSeparator}$RET{Constants.FieldSeparator}\\w{Constants.FieldSeparator}{Constants.ShellPromptPostfix}\";'");

                var shellSetupText = shellSetup.ToString();
                shellStream.WriteLine(shellSetupText);

                if (!WaitForReady(5000))
                {
                    throw new TimeoutException("Shell not entering ready state");
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void OnNewShellOutput(List<string> lines, ShellPromptInfo? readyPrompt)
        {
            foreach (var line in lines)
            {
                Console.WriteLine(line);
                if (line.StartsWith("dotnet"))
                {
                    Console.WriteLine("dotnet detected");
                }
            }

            if (readyPrompt != null)
            {
                Interlocked.Exchange<ShellPromptInfo>(ref currentPromptInfo, readyPrompt);
                isReadyEvent.Set();
            }
        }

        private void OnErrorOccurred(object? sender, ExceptionEventArgs e)
        {
            Console.WriteLine(e.Exception);
        }

        private void RevokeReady()
        {
            // Console.WriteLine(">> REVOKE READY");
            isReadyEvent.Reset();
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                if (shellStream != null)
                {
                    shellStream.ErrorOccurred -= OnErrorOccurred;
                    shellStream.Dispose();
                }

                outputReader?.Dispose();
                IsDisposed = true;
            }
        }

        public bool WaitForReady()
        {
            return isReadyEvent.WaitOne();
        }

        public bool WaitForReady(TimeSpan timeout)
        {
            return isReadyEvent.WaitOne(timeout);
        }

        public bool WaitForReady(int millisecondsTimeout)
        {
            return isReadyEvent.WaitOne(millisecondsTimeout);
        }

        public void Execute(string commands)
        {
            Console.WriteLine($"EXECUTE : {commands}");

            RevokeReady();
            shellStream.WriteLine(commands);


        }
    }
}