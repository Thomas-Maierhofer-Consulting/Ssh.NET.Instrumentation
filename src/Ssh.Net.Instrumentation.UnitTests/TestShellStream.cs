using System;
using System.Collections.Generic;
using Renci.SshNet.Common;
using Ssh.Net.Instrumentation.Details;

namespace Ssh.Net.Instrumentation.UnitTests
{
    public class TestShellStream : IShellStream
    {
        public static string CorrectPrompt => $"{Constants.ShellNewlineSeparator}{Constants.ShellPromptPrefix}0{Constants.FieldSeparator}0{Constants.FieldSeparator}~{Constants.FieldSeparator}{Constants.ShellPromptPostfix}";


        public List<string> WrittenLines { get; private set; } = new List<string>();

        private Queue<string> readerQueue = new Queue<string>();

        public int DisposeCalled { get; private set; } = 0;


        public void Dispose()
        {
            ++DisposeCalled;
        }

        public event EventHandler<ExceptionEventArgs> ErrorOccurred;
        public event EventHandler<ShellDataEventArgs> DataReceived;
        public bool DataAvailable { get; private set; }
        public string Read()
        {
            if (readerQueue.Count <= 1) DataAvailable = false;
            if (readerQueue.Count == 0) return string.Empty;
            return readerQueue.Dequeue();
        }

        public void WriteLine(string text)
        {
            WrittenLines.Add(text);
        }

        public void ProvideReaderInput(ICollection<string> data)
        {
            readerQueue = new Queue<string>(data);
            DataAvailable = true;
            if (DataReceived != null)
            {
                DataReceived.Invoke(this, new ShellDataEventArgs(String.Empty));
            }
        }
    }
}