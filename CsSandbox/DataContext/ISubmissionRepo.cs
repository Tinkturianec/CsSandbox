using System.Collections.Generic;
using System.Threading;
using CsSandbox.Models;
using CsSandboxApi;
using CsSandboxRunnerApi;

namespace CsSandbox.DataContext
{
	public interface ISubmissionRepo
	{
		SubmissionDetails AddSubmission(string userId, SubmissionModel submission);
		SubmissionDetails FindDetails(string id);
		IEnumerable<SubmissionDetails> GetAllSubmissions(string userId, int max, int skip);
		void SaveResults(RunningResults result);
		SubmissionDetails FindUnhandled();
		List<SubmissionDetails> GetUnhandled(int count, CancellationToken token);
		void SaveAllResults(List<RunningResults> results);
	}
}
