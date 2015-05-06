using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CsSandboxApi;
using CsSandboxRunnerApi;

namespace CsSandboxRunner
{
	static class Program
	{
		private static readonly BlockingCollection<InternalSubmissionModel> Unhandled = new BlockingCollection<InternalSubmissionModel>();
		private static readonly ConcurrentQueue<RunningResults> Results = new ConcurrentQueue<RunningResults>();
		private static Client _client;

		static void Main(string[] args)
		{
			if (args.Length < 2)
			{
				Console.Error.WriteLine("Format: <address> <token> [<threads count>]");
				return;
			}

			AppDomain.MonitoringIsEnabled = true;

			var address = args[0];
			var token = args[1];
			int threadsCount;
			if (args.Length < 3 || !int.TryParse(args[2], out threadsCount))
				threadsCount = Environment.ProcessorCount - 1;

			Console.Error.WriteLine("Start with {0} threads", threadsCount);

			_client = new Client(address, token);

			for (var i = 0; i < threadsCount; ++i)
			{
				new Thread(Handle).Start();
			}
			new Thread(Send).Start();

			while (true)
			{
				if (Unhandled.Count >= (threadsCount + 1)/2)
				{
					Thread.Sleep(100);
					continue;
				}
				List<InternalSubmissionModel> unhandled;
				try
				{
					unhandled = _client.TryGetSubmissions(threadsCount).Result;
				}
				catch (TaskCanceledException)
				{
					unhandled = new List<InternalSubmissionModel>();
				}
				foreach (var submission in unhandled)
					Unhandled.Add(submission);
			}
		}

		private static void Handle()
		{	
			foreach (var submission in Unhandled.GetConsumingEnumerable())
			{
				Console.Out.WriteLine(submission.Id + " start");
				Console.Out.Flush();
				RunningResults result;
				try
				{
					result = new SandboxRunner(submission).Run();
				}
				catch (Exception ex)
				{
					result = new RunningResults
					{
						Id = submission.Id,
						Verdict = Verdict.SandboxError,
						Error = ex.ToString()
					};
				}
				Results.Enqueue(result);
				Console.Out.WriteLine(submission.Id + " finish");
				Console.Out.Flush();
			}
		}

		private static void Send()
		{
			while (true)
			{
				if (!Results.IsEmpty)
				{
					var results = new List<RunningResults>();
					RunningResults result;
					while (Results.TryDequeue(out result))
						results.Add(result);
					try
					{
						Console.Out.WriteLine("send {0} results", results.Count);
						_client.SendResults(results);
					}
					catch (TaskCanceledException)
					{
					}
				}
				Thread.Sleep(100);
			}
		}
	}
}
