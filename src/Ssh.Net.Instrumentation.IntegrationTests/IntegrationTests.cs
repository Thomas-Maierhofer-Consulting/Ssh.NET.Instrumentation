using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Renci.SshNet;
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


        // Adjust Values to your machine setup - this is a WSL2 Ubuntu on Windows with .NET Core 3.1 installed


        private const string SshServerHost = "172.25.139.27";
        private const string SshServerUser = "ubuntu";
        private const string SshServerPassword = "ubuntu";

        public IntegrationTestFixture()
        {
            Client = new SshClient(new ConnectionInfo(SshServerHost, SshServerUser,
                new PasswordAuthenticationMethod(SshServerUser, SshServerPassword)));
            Client.Connect();
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
    public class IntegrationTests
    {

        private const string TestApplicationDirectory =
            "/mnt/c/Projects/TMC/Ssh.NET.Instrumentation/src/Ssh.Net.Instrumentation.ServerSideTestApp/bin/Debug/netcoreapp3.1";

        private const string NotExistingDirectory = "/Tom/Jerry";



        private readonly IntegrationTestFixture fixture;

        public IntegrationTests(IntegrationTestFixture fixture)
        {
            this.fixture = fixture;
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

            instrumentation.Execute($"cd {TestApplicationDirectory}");
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

            instrumentation.Execute($"cd {NotExistingDirectory}");
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

                instrumentation.Execute($"cd {TestApplicationDirectory}");
                instrumentation.WaitForReady();
                var promptInfo = instrumentation.GetCurrentPromptInfo();
                promptInfo.LastExitCode.Should().Be(0);
                promptInfo.CurrentDirectory.Should().Be(TestApplicationDirectory);
            }

            instrumentation.Execute($"dotnet Ssh.Net.Instrumentation.ServerSideTestApp.dll");
            instrumentation.WaitForReady();

            using (new AssertionScope())
            {
                var promptInfo = instrumentation.GetCurrentPromptInfo();
                instrumentation.IsReady.Should().BeTrue();
                promptInfo.LastExitCode.Should().Be(0);
                promptInfo.CurrentDirectory.Should().Be(TestApplicationDirectory);
            }

        }

    }
}

