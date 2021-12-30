using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace Ssh.Net.Instrumentation.Details
{
    internal sealed class ShellOutputReader : IDisposable
    {
        private readonly IShellStream shellStream;
        private readonly ShellInstrumentationConfig config;
        private readonly Action<string, ShellPromptInfo?> onNewShellOutputAction;
        private readonly AutoResetEvent readDataAvailable = new AutoResetEvent(true);
        private readonly Thread readWorkerThread;
        private volatile bool shutdownReadWorkerThread;
        private StringBuilder outputSinceLastReadyPrompt = new StringBuilder();

        public bool IsDisposed { get; private set; }

        public ShellOutputReader(IShellStream shellStream, ShellInstrumentationConfig config, Action<string, ShellPromptInfo?> onNewShellOutputAction)
        {
            this.shellStream = shellStream ?? throw new ArgumentNullException(nameof(shellStream));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.onNewShellOutputAction = onNewShellOutputAction ??
                                          throw new ArgumentNullException(nameof(onNewShellOutputAction));
            shellStream.DataReceived += OnDataReceived;
            readWorkerThread = new Thread(ReadWorkerFkt);
            readWorkerThread.Start();
        }

        public void OnDataReceived(object sender, ShellDataEventArgs e)
        {
            readDataAvailable.Set();
        }

        public void ReadWorkerFkt()
        {
            while (true)
            {
                readDataAvailable.WaitOne();
                if (shutdownReadWorkerThread) return;

                ShellPromptInfo? openPromptInfo = null;
                StringBuilder totalNewOutput = new StringBuilder();

                while (shellStream.DataAvailable)
                {
                    var newOutput = shellStream.Read();
                    outputSinceLastReadyPrompt.Append(newOutput);
                    totalNewOutput.Append(newOutput);

                    // Detecting an open prompt waiting for input
                    var promptIndex = newOutput.LastIndexOf(Constants.ShellPromptPrefix,StringComparison.Ordinal);
                    if (promptIndex >= 0 && newOutput.EndsWith(Constants.ShellPromptPostfix))
                    {
                        Console.WriteLine("On Open Prompt");
                        var promptText = newOutput.Substring(promptIndex);
                        Console.WriteLine(promptText);

                        var promptFields = promptText.Split(Constants.FieldSeparator);

                        // Check if this prompt is ill formed
                        if (promptFields.Length != 6 || promptFields[^1] != Constants.ShellPromptPostfix)
                        {
                            Console.WriteLine("Ill formed Prompt");
                        }

                        openPromptInfo = new ShellPromptInfo(int.Parse(promptFields[2]), int.Parse(promptFields[3]), promptFields[4], outputSinceLastReadyPrompt.ToString());

                        // Safe time to be really on the prompt and no additional data is dropping in
                        Thread.Sleep(config.ShellPromptReadyWaitTime);
                    }
                }

                if (totalNewOutput.Length > 0)
                {
                    if (openPromptInfo != null)
                    {
                        outputSinceLastReadyPrompt = new StringBuilder();
                    }

                    onNewShellOutputAction.Invoke(totalNewOutput.ToString(), openPromptInfo);
                }

            }
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                shutdownReadWorkerThread = true;
                readDataAvailable.Set();
                readWorkerThread.Join();
                shellStream.DataReceived -= OnDataReceived;
                IsDisposed = true;
            }
        }

    }
}