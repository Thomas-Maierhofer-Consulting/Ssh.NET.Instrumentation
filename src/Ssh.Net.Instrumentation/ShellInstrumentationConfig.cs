using System;

namespace Ssh.Net.Instrumentation
{
    public class ShellInstrumentationConfig
    {
        public string ShellTerminalName { get; set; } = "INSTRUMENTATION";

        public uint ShellColumns { get; set; } = 1024;
        public uint ShellRows { get; set; } = 128;
        public uint ShellWidth { get; set; } = 1024;
        public uint ShellHeight { get; set; } = 128;
        public int ShellBufferSize { get; set; } = 4096;

        public TimeSpan ShellPromptReadyWaitTime { get; set; } = TimeSpan.FromMilliseconds(200);

    }
}