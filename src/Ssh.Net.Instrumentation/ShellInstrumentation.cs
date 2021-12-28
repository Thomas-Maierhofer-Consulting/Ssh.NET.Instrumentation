using System;
using System.Diagnostics;
using Ssh.Net.Instrumentation.Details;

namespace Ssh.Net.Instrumentation
{
    public class ShellInstrumentation: IDisposable
    {
        private readonly IShellOperationCapturing shellOperationCapturing; 

        internal  ShellInstrumentation(IShellStream shellStream, ShellInstrumentationConfig config)
        {
            shellOperationCapturing = new ShellOperationCapturing(shellStream, config);
        }

        internal ShellInstrumentation(IShellOperationCapturing shellOperationCapturing)
        {
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
            shellOperationCapturing.WaitForReady();
        }

        public bool WaitForReady(int milliseconds)
        {
            return shellOperationCapturing.WaitForReady(milliseconds);
        }

        public bool WaitForReady(TimeSpan timeSpan)
        {
            return shellOperationCapturing.WaitForReady(timeSpan);
        }


        public ShellPromptInfo GetCurrentPromptInfo()
        {
            return shellOperationCapturing.GetCurrentPromptInfo();
        }
    }
}