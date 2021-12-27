namespace Ssh.Net.Instrumentation
{
    public class ShellPromptInfo
    {
        public ShellPromptInfo() : this(-1, 0, string.Empty)
        {
        }

        public ShellPromptInfo(int lastCommandNumber, int lastExitCode, string currentDirectory)
        {
            LastCommandNumber = lastCommandNumber;
            CurrentDirectory = currentDirectory;
            LastExitCode = lastExitCode;
        }

        public int LastCommandNumber { get; }

        public int LastExitCode { get; }

        public string CurrentDirectory { get; }


        public override string ToString()
        {
            return $"PROMPT INFO: COMMAND#: {LastCommandNumber}, EXIT CODE: {LastExitCode}, CURRENT DIR: {CurrentDirectory}";
        }
    }
}