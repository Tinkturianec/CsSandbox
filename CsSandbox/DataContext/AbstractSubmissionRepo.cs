using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CsSandbox.Models;
using CsSandboxApi;
using CsSandboxRunnerApi;

namespace CsSandbox.DataContext
{
	public abstract class AbstractSubmissionRepo : ISubmissionRepo
	{
		abstract protected SubmissionDetails Find(string id);
		abstract protected void Save(SubmissionDetails submission);
		abstract public IEnumerable<SubmissionDetails> GetAllSubmissions(string userId, int max, int skip);
		abstract protected List<SubmissionDetails> FindAll(List<string> submissions);
		abstract protected void SaveAll(IEnumerable<SubmissionDetails> result);

		private static readonly ConcurrentQueue<string> Unhandled = new ConcurrentQueue<string>();

		public SubmissionDetails FindUnhandled()
		{
			string id;
			if (!Unhandled.TryDequeue(out id))
				return null;
			var submission = Find(id);
			submission.Status = SubmissionStatus.Running;
			Save(submission);
			return submission;
		}

		public SubmissionDetails AddSubmission(string userId, SubmissionModel submission)
		{
			var submissionDetail = new SubmissionDetails
			{
				Id = Guid.NewGuid().ToString(),
				UserId = userId,
				Code = submission.Code,
				Input = submission.Input,
				Status = SubmissionStatus.Waiting,
				Verdict = Verdict.NA,
				Timestamp = DateTime.Now,
				NeedRun = submission.NeedRun,
				DisplayName = submission.DisplayName
			};
			Save(submissionDetail);

			Unhandled.Enqueue(submissionDetail.Id);

			return submissionDetail;
		}

		public SubmissionDetails FindDetails(string id)
		{
			return Find(id);
		}

		public void SaveResults(RunningResults result)
		{
			var submission = Find(result.Id);
			submission = UpdateSubmission(submission, result);
			Save(submission);
		}

		public void SaveAllResults(List<RunningResults> results)
		{
			var resultsDict = results.ToDictionary(result => result.Id);
			var submissions = FindAll(results.Select(result => result.Id).ToList());
			var res = submissions.Select(submission => UpdateSubmission(submission, resultsDict[submission.Id])).ToList();
			SaveAll(res);
		}

		private static SubmissionDetails UpdateSubmission(SubmissionDetails submission, RunningResults result)
		{
			submission.CompilationOutput = result.CompilationOutput;
			submission.Verdict = result.Verdict;
			submission.Output = result.Output;
			submission.Error = result.Error;
			submission.Status = SubmissionStatus.Done;
			return submission;
		}

		public List<SubmissionDetails> GetUnhandled(int count, CancellationToken token)
		{
			var submissions = new List<string>();
			while (!token.IsCancellationRequested && Unhandled.IsEmpty)
				Thread.Sleep(100);

			for (var i = 0; i < count; ++i)
			{
				string submission;
				if (!Unhandled.TryDequeue(out submission))
					break;
				submissions.Add(submission);
			}
			var result = FindAll(submissions);
			foreach (var details in result)
			{
				details.Status = SubmissionStatus.Running;
			}
			SaveAll(result);
			return result;
		}
	}
}