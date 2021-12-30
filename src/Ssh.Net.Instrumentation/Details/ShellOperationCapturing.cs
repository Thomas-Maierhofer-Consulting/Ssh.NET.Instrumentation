using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public bool WaitForReady(int millisecondsTimeout, uint commandNumber);
        public void PromptEnter(string text);
    }



    internal sealed class ShellOperationCapturing: IShellOperationCapturing
    {
        private readonly ShellInstrumentationConfig config;


        public bool IsDisposed { get; private set; }

        public bool IsReady => isReadyEvent.WaitOne(0);

        public ShellPromptInfo GetCurrentPromptInfo()
        {
            lock (readyUpdateLock)
            {
                return currentPromptInfo;
            }
        }

        private readonly IShellStream shellStream;
        private readonly ShellOutputReader outputReader;

        private readonly object readyUpdateLock = new object();
        private readonly ManualResetEvent isReadyEvent = new ManualResetEvent(false);
        private ShellPromptInfo currentPromptInfo = new ShellPromptInfo();
        private uint readyWaitCommandNumber;

        public ShellOperationCapturing(IShellStream shellStream, ShellInstrumentationConfig config)
        {
            this.shellStream = shellStream ?? throw new ArgumentNullException(nameof(shellStream));
            this.config = config ?? throw new ArgumentNullException(nameof(config));

            Console.WriteLine();
            Console.WriteLine("Setup Operation Capturing");
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

                if (!WaitForReady(5000, 2))
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

        private void OnNewShellOutput(string text, ShellPromptInfo? readyPrompt)
        {
            Console.Write(text);

            if (readyPrompt != null)
            {
                lock (readyUpdateLock)
                {
                    currentPromptInfo = readyPrompt;
                    isReadyEvent.Set();
                }
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
                outputReader?.Dispose();

                shellStream.ErrorOccurred -= OnErrorOccurred;
                shellStream.Dispose();

                IsDisposed = true;
            }
        }

        public bool WaitForReady(int millisecondsTimeout, uint commandNumber = 0)
        {
            // Check if the prompt is already ready and on the right command number
            lock (readyUpdateLock)
            {
                readyWaitCommandNumber = commandNumber;
                if (isReadyEvent.WaitOne(0))
                {
                    var promptInfo = GetCurrentPromptInfo();
                    if(promptInfo.LastCommandNumber >= commandNumber) return true;

                    isReadyEvent.Reset();
                }
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.ElapsedMilliseconds < millisecondsTimeout)
            {
                
                int leftoverMilliseconds = millisecondsTimeout - (int) sw.ElapsedMilliseconds;
                if (isReadyEvent.WaitOne(leftoverMilliseconds))
                {
                    lock (readyUpdateLock)
                    {
                        var promptInfo = GetCurrentPromptInfo();
                        if (promptInfo.LastCommandNumber >= commandNumber) return true;

                        isReadyEvent.Reset();
                    }
                }

            }

            return false;
        }

        public void PromptEnter(string text)
        {
            RevokeReady();
            shellStream.WriteLine(text);
        }
    }
}