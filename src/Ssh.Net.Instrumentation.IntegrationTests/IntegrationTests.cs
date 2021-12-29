using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Renci.SshNet;
using Renci.SshNet.Common;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Ssh.Net.Instrumentation.IntegrationTests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TestPriorityAttribute : Attribute
    {
        public int Priority { get; private set; }

        public TestPriorityAttribute(int priority) => Priority = priority;
    }

    public class PriorityOrderer : ITestCaseOrderer
    {

        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(
            IEnumerable<TTestCase> testCases) where TTestCase : ITestCase
        {
            string assemblyName = typeof(TestPriorityAttribute).AssemblyQualifiedName!;
            var sortedMethods = new SortedDictionary<int, List<TTestCase>>();
            foreach (TTestCase testCase in testCases)
            {
                int priority = testCase.TestMethod.Method
                    .GetCustomAttributes(assemblyName)
                    .FirstOrDefault()
                    ?.GetNamedArgument<int>(nameof(TestPriorityAttribute.Priority)) ?? 0;

                GetOrCreate(sortedMethods, priority).Add(testCase);
            }

            foreach (TTestCase testCase in
                sortedMethods.Keys.SelectMany(
                    priority => sortedMethods[priority].OrderBy(
                        testCase => testCase.TestMethod.Method.Name)))
            {
                yield return testCase;
            }
        }

        private static TValue GetOrCreate<TKey, TValue>(
            IDictionary<TKey, TValue> dictionary, TKey key)
            where TKey : struct
            where TValue : new() =>
            dictionary.TryGetValue(key, out TValue result)
                ? result
                : (dictionary[key] = new TValue());
    }



    public class IntegrationTestFixture : IDisposable
    {
        public SshClient Client { get; private set; }


        // These are the settings / credentials for the locally docker image with the test ssh server
        private const string SshServerHost = "localhost";
        private const int SshServerPort = 2222;
        private const string SshServerUser = "test";
        private const string SshServerPassword = "test";

        public IntegrationTestFixture()
        {
            Client = new SshClient(new ConnectionInfo(SshServerHost, SshServerPort, SshServerUser,
                new PasswordAuthenticationMethod(SshServerUser, SshServerPassword)));

        }

        public void Dispose()
        {
            Client.Dispose();
        }
    }


    [CollectionDefinition("IntegrationTests")]
    public class IntegrationTestsCollection : ICollectionFixture<IntegrationTestFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    [Collection("IntegrationTests")]
    [TestCaseOrderer("Ssh.Net.Instrumentation.IntegrationTests.PriorityOrderer",
        "Ssh.Net.Instrumentation.IntegrationTests")]
    public class CreateDisposeIntegrationTests : IDisposable
    {
        private readonly IntegrationTestFixture fixture;

        public CreateDisposeIntegrationTests(IntegrationTestFixture fixture)
        {
            this.fixture = fixture;

        }


        [Fact, TestPriority(1)]
        public void InstrumentationCreateDisposeTest()
        {
            fixture.Client.Connect();

            ShellInstrumentation instrumentation;
            using (instrumentation = fixture.Client.CreateShellInstrumentation(new ShellInstrumentationConfig()))
            {
                instrumentation.IsDisposed.Should().BeFalse();
                instrumentation.IsReady.Should().BeTrue();
            }

            instrumentation.IsDisposed.Should().BeTrue();
        }

        [Fact, TestPriority(1)]
        public void ClientNotConnectedTest()
        {
            Action act = () =>
            {
                using var instrumentation = fixture.Client.CreateShellInstrumentation(new ShellInstrumentationConfig());
            };

            act.Should().Throw<SshConnectionException>()
                .WithMessage("Client not connected.");

        }


        public void Dispose()
        {
            if (fixture.Client.IsConnected)
            {
                fixture.Client.Disconnect();
            }
        }
    }



    [Collection("IntegrationTests")]
    [TestCaseOrderer("Ssh.Net.Instrumentation.IntegrationTests.PriorityOrderer",
        "Ssh.Net.Instrumentation.IntegrationTests")]
    public class CommandExecutionIntegrationTests: IDisposable
    {

        private const string TestApplicationDirectory =
            "/app/src/Ssh.Net.Instrumentation.ServerSideTestApp/bin/Debug/netcoreapp3.1";

        private const string NotExistingDirectory = "/Tom/Jerry";



        private readonly IntegrationTestFixture fixture;

        public CommandExecutionIntegrationTests(IntegrationTestFixture fixture)
        {
            this.fixture = fixture;
            fixture.Client.Connect();
        }


        [Fact, TestPriority(1)]
        public void InstrumentationCreateDisposeTest()
        {
            ShellInstrumentation instrumentation;
            using (instrumentation = fixture.Client.CreateShellInstrumentation(new ShellInstrumentationConfig()))
            {

                using (new AssertionScope("Assert"))
                {
                    instrumentation.IsDisposed.Should().BeFalse();
                    instrumentation.IsReady.Should().BeTrue();
                }
            }

            instrumentation.IsDisposed.Should().BeTrue();
        }

        [Fact, TestPriority(2)]
        public void ChangeDirectoryTest()
        {
            using var instrumentation = fixture.Client.CreateShellInstrumentation(new ShellInstrumentationConfig());

            using (new AssertionScope("Expect"))
            {
                instrumentation.IsDisposed.Should().BeFalse();
                instrumentation.IsReady.Should().BeTrue();
            }

            instrumentation.PromptEnter($"cd {TestApplicationDirectory}");
            instrumentation.WaitForReady();
            var promptInfo = instrumentation.GetCurrentPromptInfo();

            using (new AssertionScope("Assert"))
            {
                instrumentation.IsReady.Should().BeTrue();
                promptInfo.LastExitCode.Should().Be(0);
                promptInfo.CurrentDirectory.Should().Be(TestApplicationDirectory);
            }

        }

        [Fact, TestPriority(3)]
        public void ChangeNotExistingDirectoryTest()
        {
            using var instrumentation = fixture.Client.CreateShellInstrumentation(new ShellInstrumentationConfig());

            using (new AssertionScope())
            {
                instrumentation.IsDisposed.Should().BeFalse();
                instrumentation.IsReady.Should().BeTrue();
            }

            instrumentation.PromptEnter($"cd {NotExistingDirectory}");
            instrumentation.WaitForReady();
            var promptInfo = instrumentation.GetCurrentPromptInfo();

            using (new AssertionScope())
            {
                instrumentation.IsReady.Should().BeTrue();
                promptInfo.LastExitCode.Should().Be(1);
                promptInfo.CurrentDirectory.Should().NotBe(NotExistingDirectory);
            }
        }

        [Fact, TestPriority(4)]
        public void ExecuteApplicationRegularTest()
        {
            using var instrumentation = fixture.Client.CreateShellInstrumentation(new ShellInstrumentationConfig());

            using (new AssertionScope())
            {
                instrumentation.IsDisposed.Should().BeFalse();
                instrumentation.IsReady.Should().BeTrue();

                instrumentation.PromptEnter($"cd {TestApplicationDirectory}");
                instrumentation.WaitForReady();
                var promptInfo = instrumentation.GetCurrentPromptInfo();
                promptInfo.LastExitCode.Should().Be(0);
                promptInfo.CurrentDirectory.Should().Be(TestApplicationDirectory);
            }

            instrumentation.PromptEnter($"dotnet Ssh.Net.Instrumentation.ServerSideTestApp.dll");
            instrumentation.WaitForReady();

            using (new AssertionScope())
            {
                var promptInfo = instrumentation.GetCurrentPromptInfo();
                instrumentation.IsReady.Should().BeTrue();
                promptInfo.LastExitCode.Should().Be(42);
                promptInfo.CurrentDirectory.Should().Be(TestApplicationDirectory);
            }

        }

        public void Dispose()
        {
            if (fixture.Client.IsConnected)
            {
                fixture.Client.Disconnect();
            }
        }
    }
}

