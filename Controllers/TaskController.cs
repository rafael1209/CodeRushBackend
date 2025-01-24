using CodeRushBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeRushBackend.Controllers
{
    [ApiController]
    [Route("api/v1/task")]
    public class TaskController(CodeExecutionService codeExecutionService) : ControllerBase
    {
        [HttpGet("{id}")]
        public IActionResult GetTask(int id)
        {
            var task = new TaskDescription
            {
                Id = 1,
                Title = "Check Even or Odd",
                Description = "Write a C# program that checks if a number is even or odd.",
                Examples = new List<Example>
                {
                    new Example { Input = "4", Output = "Even" },
                    new Example { Input = "7", Output = "Odd" }
                },
                Instructions = new List<string>
                {
                    "Use the modulus operator (%) to check if the number is even or odd.",
                    "If the number is divisible by 2, print 'Even'.",
                    "Otherwise, print 'Odd'."
                }
            };

            if (id != task.Id)
            {
                return NotFound("Task not found.");
            }

            return Ok(task);
        }

        [HttpPost("submit-answer")]
        public IActionResult SubmitAnswer([FromBody] TaskAnswerModel answer)
        {
            string userCode = answer.Code;

            var testCases = new List<TestCase>
            {
                new TestCase { Input = "0", ExpectedOutput = "Even" },
                new TestCase { Input = "2", ExpectedOutput = "Even" },
                new TestCase { Input = "4", ExpectedOutput = "Even" },
                new TestCase { Input = "10", ExpectedOutput = "Even" },
                new TestCase { Input = "100", ExpectedOutput = "Even" },
                new TestCase { Input = "1", ExpectedOutput = "Odd" },
                new TestCase { Input = "3", ExpectedOutput = "Odd" },
                new TestCase { Input = "7", ExpectedOutput = "Odd" },
                new TestCase { Input = "9", ExpectedOutput = "Odd" },
                new TestCase { Input = "101", ExpectedOutput = "Odd" },

                new TestCase { Input = "-2", ExpectedOutput = "Even" },
                new TestCase { Input = "-4", ExpectedOutput = "Even" },
                new TestCase { Input = "-1", ExpectedOutput = "Odd" },
                new TestCase { Input = "-3", ExpectedOutput = "Odd" },
                new TestCase { Input = "123456", ExpectedOutput = "Even" },
                new TestCase { Input = "123457", ExpectedOutput = "Odd" }
            };

            var result = codeExecutionService.ValidateUserCode(userCode, testCases);

            if (result.Errors != null && result.Errors.Count > 0)
            {
                return Ok(new
                {
                    message = "Compilation error",
                    errors = result.Errors
                });
            }

            if (result.Success)
            {
                return Ok(new
                {
                    message = "All tests passed!",
                    testResults = result.TestResults
                });
            }

            return Ok(new
            {
                message = "Some tests failed.",
                testResults = result.TestResults.Select(tr => new
                {
                    tr.Input,
                    tr.ExpectedOutput,
                    tr.ActualOutput,
                    tr.IsSuccess
                })
            });
        }
    }

    public class TaskDescription
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<Example> Examples { get; set; }
        public List<string> Instructions { get; set; }
    }

    public class Example
    {
        public string Input { get; set; }
        public string Output { get; set; }
    }

    public class TaskAnswerModel
    {
        public required string Code { get; set; }
    }

    public class TestCase
    {
        public string Input { get; set; }
        public string ExpectedOutput { get; set; }
    }

    public class TestResult
    {
        public string Input { get; set; }
        public string ExpectedOutput { get; set; }
        public string ActualOutput { get; set; }
        public bool IsSuccess { get; set; }
    }

    public class CodeExecutionResult
    {
        public bool Success { get; set; }
        public List<string> Errors { get; set; }
        public List<TestResult> TestResults { get; set; }
    }
}