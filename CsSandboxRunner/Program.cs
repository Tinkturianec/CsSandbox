using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
			AppDomain.MonitoringIsEnabled = true;

			string address, token;
			int threadsCount;
			if (args.Length < 2)
			{
				try
				{
					var file = new FileInfo("config");
					var stream = file.OpenText();
					address = stream.ReadLine();
					token = stream.ReadLine();
					var threadCountString = stream.ReadLine();
					if (threadCountString == null || !int.TryParse(threadCountString, out threadsCount))
						threadsCount = Environment.ProcessorCount - 1;
					stream.Close();
				}
				catch (Exception)
				{
					return;
				}
			}
			else
			{
				address = args[0];
				token = args[1];
				if (args.Length < 3 || !int.TryParse(args[2], out threadsCount))
					threadsCount = Environment.ProcessorCount - 1;
			}

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
				catch (AggregateException)
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
						_client.SendResults(results);
					}
					catch (AggregateException)
					{
					}
				}
				Thread.Sleep(100);
			}
		}
	}
}
