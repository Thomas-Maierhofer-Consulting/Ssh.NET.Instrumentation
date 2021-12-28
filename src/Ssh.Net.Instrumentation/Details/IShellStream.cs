using System;
using Renci.SshNet.Common;

namespace Ssh.Net.Instrumentation.Details
{
    public interface IShellStream: IDisposable
    {
        event EventHandler<ExceptionEventArgs> ErrorOccurred;
        event EventHandler<ShellDataEventArgs> DataReceived;
        bool DataAvailable { get; }
        string Read();
        void WriteLine(string text);
    }
}