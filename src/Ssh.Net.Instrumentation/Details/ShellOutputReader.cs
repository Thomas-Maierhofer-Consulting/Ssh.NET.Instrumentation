using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace Ssh.Net.Instrumentation.Details
{
    internal sealed class ShellOutputReader : IDisposable
    {
        private readonly IShellStream shellStream;
        private readonly ShellInstrumentationConfig config;
        private readonly Action<List<string>, ShellPromptInfo?> onNewShellOutputAction;
        private readonly AutoResetEvent readDataAvailable = new AutoResetEvent(true);
        private readonly Thread readWorkerThread;
        private volatile bool shutdownReadWorkerThread;

        public bool IsDisposed { get; private set; }

        public ShellOutputReader(IShellStream shellStream, ShellInstrumentationConfig config, Action<List<string>, ShellPromptInfo?> onNewShellOutputAction)
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

                List<string> newLines = new List<string>();
                ShellPromptInfo? openPromptInfo = null;

                while (shellStream.DataAvailable)
                {
                    var newData = shellStream.Read();
                    var lines = newData.Split(Constants.ShellNewlineSeparator).ToList();
                    if (lines.Count == 0) continue;
                    
                    openPromptInfo = null;
                    newLines.AddRange(lines);

                    if (lines[^1].StartsWith(Constants.ShellPromptPrefix))
                    {
                        var promptFields = lines[^1].Split(Constants.FieldSeparator);

                        // Check if this is an open prompt
                        if (promptFields.Length != 6 || promptFields[^1] != Constants.ShellPromptPostfix) continue;

                        openPromptInfo = new ShellPromptInfo(int.Parse(promptFields[2]), int.Parse(promptFields[3]), promptFields[4]);

                        // Safe time to be really on the prompt and no additional data is dropping in
                        Thread.Sleep(config.ShellPromptReadyWaitTime);
                    }
                }

                if (newLines.Count > 0)
                {
                    onNewShellOutputAction.Invoke(newLines, openPromptInfo);
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