using Renci.SshNet;
using Ssh.Net.Instrumentation.Details;

namespace Ssh.Net.Instrumentation
{
    public static class SshClientExtensions
    {
        public static ShellInstrumentation CreateShellInstrumentation(this SshClient client, ShellInstrumentationConfig config)
        {

            var shellStream = client.CreateShellStream(config.ShellTerminalName,
                config.ShellColumns,
                config.ShellRows,
                config.ShellWidth,
                config.ShellHeight,
                config.ShellBufferSize);

            try
            {
                return new ShellInstrumentation(new ShellStreamInterfaceDecorator(shellStream), config);
            }
            catch
            {
                shellStream.Dispose();
                throw;
            }
        }
    }
}