using CodeRushBackend.Controllers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CodeRushBackend.Services
{
    public class CodeExecutionService
    {
        public CodeExecutionResult ValidateUserCode(string userCode, List<TestCase> testCases)
        {
            string completeCode = userCode;

            Assembly assembly;
            try
            {
                assembly = CompileCode(completeCode);
            }
            catch (Exception ex)
            {
                return new CodeExecutionResult
                {
                    Success = false,
                    Errors = new List<string> { ex.Message }
                };
            }

            if (assembly == null)
            {
                return new CodeExecutionResult
                {
                    Success = false,
                    Errors = new List<string> { "Failed to compile the code." }
                };
            }

            var results = new List<TestResult>();
            foreach (var testCase in testCases)
            {
                try
                {
                    var output = ExecuteCode(assembly, testCase.Input);
                    results.Add(new TestResult
                    {
                        Input = testCase.Input,
                        ExpectedOutput = testCase.ExpectedOutput,
                        ActualOutput = output,
                        IsSuccess = output == testCase.ExpectedOutput
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new TestResult
                    {
                        Input = testCase.Input,
                        ExpectedOutput = testCase.ExpectedOutput,
                        ActualOutput = ex.Message,
                        IsSuccess = false
                    });
                }
            }

            bool isAllTestsPassed = results.All(r => r.IsSuccess);

            return new CodeExecutionResult
            {
                Success = isAllTestsPassed,
                TestResults = results
            };
        }

        private Assembly CompileCode(string code)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .ToList();

            references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(Console).Assembly.Location));

            var compilation = CSharpCompilation.Create(
                assemblyName: "UserCodeAssembly",
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

            using var ms = new MemoryStream();
            var emitResult = compilation.Emit(ms);

            if (!emitResult.Success)
            {
                var errorMessages = emitResult.Diagnostics
                    .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                    .Select(diagnostic => diagnostic.GetMessage())
                    .ToList();

                throw new Exception(string.Join("\n", errorMessages));
            }

            ms.Seek(0, SeekOrigin.Begin);
            return Assembly.Load(ms.ToArray());
        }

        private string ExecuteCode(Assembly assembly, string input)
        {
            var programType = assembly.GetType("Program");
            if (programType == null)
            {
                throw new Exception("Class 'Program' not found in user code.");
            }

            var mainMethod = programType.GetMethod("Main", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (mainMethod == null)
            {
                throw new Exception("Main method not found in 'Program' class.");
            }

            using (var inputReader = new StringReader(input))
            using (var outputWriter = new StringWriter())
            {
                var originalIn = Console.In;
                var originalOut = Console.Out;

                try
                {
                    Console.SetIn(inputReader);
                    Console.SetOut(outputWriter);

                    mainMethod.Invoke(null, mainMethod.GetParameters().Length > 0 ? new object[] { new string[0] } : null);

                    return outputWriter.ToString().Trim();
                }
                finally
                {
                    Console.SetIn(originalIn);
                    Console.SetOut(originalOut);
                }
            }
        }
    }
}