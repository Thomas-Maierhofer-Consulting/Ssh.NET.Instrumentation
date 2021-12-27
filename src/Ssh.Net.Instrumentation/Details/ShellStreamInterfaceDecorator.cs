using System;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace Ssh.Net.Instrumentation.Details
{
    public class ShellStreamInterfaceDecorator: IShellStream
    {
        private readonly ShellStream shellStream;

        public ShellStreamInterfaceDecorator(ShellStream shellStream)
        {
            this.shellStream = shellStream ?? throw new ArgumentNullException(nameof(shellStream));
            shellStream.DataReceived += OnDataReceived;
            shellStream.ErrorOccurred += OnErrorOccurred;
        }

        private void OnDataReceived(object sender, ShellDataEventArgs e)
        {
            if (DataReceived != null)
            {
                DataReceived.Invoke(sender,e);
            }
        }

        private void OnErrorOccurred(object sender, ExceptionEventArgs e)
        {
            if (ErrorOccurred != null)
            {
                ErrorOccurred.Invoke(sender, e);
            }
        }

        public void Dispose()
        {
            shellStream.ErrorOccurred -= OnErrorOccurred;
            shellStream.DataReceived -= OnDataReceived;
            shellStream.Dispose();
        }

        public event EventHandler<ExceptionEventArgs> ErrorOccurred;
        public event EventHandler<ShellDataEventArgs> DataReceived;
        public bool DataAvailable => shellStream.DataAvailable;
        public string Read()
        {
            return shellStream.Read();
        }

        public void WriteLine(string text)
        {
            shellStream.WriteLine(text);
        }
    }
}