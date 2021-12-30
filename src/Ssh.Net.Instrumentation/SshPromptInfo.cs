using System.Collections.Generic;

namespace Ssh.Net.Instrumentation
{
    public class ShellPromptInfo
    {
        public ShellPromptInfo() : this(-1, 0, string.Empty, string.Empty)
        {
        }

        public ShellPromptInfo(int lastCommandNumber, int lastExitCode, string currentDirectory, string output)
        {
            LastCommandNumber = lastCommandNumber;
            CurrentDirectory = currentDirectory;
            LastExitCode = lastExitCode;
            Output = output;
        }

        public int LastCommandNumber { get; }

        public int LastExitCode { get; }

        public string CurrentDirectory { get; }
        public string Output { get; }

        public override string ToString()
        {
            return $"PROMPT INFO: COMMAND#: {LastCommandNumber}, EXIT CODE: {LastExitCode}, CURRENT DIR: {CurrentDirectory}\n{Output}";
        }
    }
}