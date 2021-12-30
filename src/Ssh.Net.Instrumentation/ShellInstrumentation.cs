using System;
using System.Diagnostics;
using Ssh.Net.Instrumentation.Details;

namespace Ssh.Net.Instrumentation
{
    public class ShellInstrumentation: IDisposable
    {
        private readonly ShellInstrumentationConfig config;
        private readonly IShellOperationCapturing shellOperationCapturing; 

        internal  ShellInstrumentation(IShellStream shellStream, ShellInstrumentationConfig config)
        {
            this.config = config;
            shellOperationCapturing = new ShellOperationCapturing(shellStream, config);
        }

        internal ShellInstrumentation(IShellOperationCapturing shellOperationCapturing, ShellInstrumentationConfig config)
        {
            this.config = config;
            this.shellOperationCapturing = shellOperationCapturing;
        }

        public bool IsReady => shellOperationCapturing.IsReady;
        
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                shellOperationCapturing.Dispose();
                IsDisposed = true;
            }

            GC.SuppressFinalize(this);
        }



        public void PromptEnter(string text)
        {
            shellOperationCapturing.PromptEnter(text);
        }

        public void WaitForReady()
        {
            WaitForReady(config.DefaultWaitTime);
        }

        public bool WaitForReady(int milliseconds)
        {
            if (milliseconds < 0) throw new ArgumentException("negative timeout value", nameof(milliseconds));

            return shellOperationCapturing.WaitForReady(milliseconds, 0);
        }

        public bool WaitForReady(TimeSpan timeSpan)
        {
            return WaitForReady((int) timeSpan.TotalMilliseconds);
        }


        public ShellPromptInfo GetCurrentPromptInfo()
        {
            return shellOperationCapturing.GetCurrentPromptInfo();
        }
    }
}