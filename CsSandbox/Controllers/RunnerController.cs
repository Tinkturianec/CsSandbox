using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using CsSandbox.DataContext;
using CsSandbox.Models;
using CsSandbox.Sandbox;
using CsSandboxRunnerApi;

namespace CsSandbox.Controllers
{
    public class RunnerController : ApiController
    {
		private readonly UserRepo _user = new UserRepo();
		private readonly SubmissionRepo _submissionsRepo = new SubmissionRepo();
		private readonly SandboxHandler _sandboxHandler = new SandboxHandler();

		/// <summary>
		/// Return one submission for testing purposes
		/// </summary>
		/// <param name="token"> Runner autherization token </param>
		[HttpGet]
		[Route("TryGetSubmission")]
	    public InternalSubmissionModel TryGetSubmission(string token)
	    {
			CheckRunner(token);

		    var submission = _submissionsRepo.FindUnhandled();
		    if (submission == null)
			    return null;
			return new InternalSubmissionModel
			{
				Id = submission.Id,
				Code = submission.Code,
				Input = submission.Input,
				NeedRun = submission.NeedRun
			};
	    }

	    /// <summary>
	    /// Return list of submissions for testing purposes
	    /// </summary>
	    /// <param name="token"> Runner autherization token </param>
	    /// <param name="count"> Count of submission </param>
	    [HttpGet]
	    [Route("GetSubmissions")]
	    public List<InternalSubmissionModel> GetSubmissions([FromUri] string token, [FromUri] int count)
	    {
			CheckRunner(token);
		    var submissions = _submissionsRepo.GetUnhandled(count, new CancellationTokenSource(30000).Token);
		    return submissions
			    .Select(details => new InternalSubmissionModel
			    {
				    Id = details.Id,
				    Code = details.Code,
				    Input = details.Input,
				    NeedRun = details.NeedRun
			    })
			    .ToList();
	    }

	    /// <summary>
	    /// Get testing result
	    /// </summary>
		/// <param name="token"> Runner autherization token </param>
	    [HttpPost]
		[Route("PostResult")]
	    public void PostResult([FromUri]string token, RunningResults result)
	    {
			if (!ModelState.IsValid)
				throw new HttpResponseException(HttpStatusCode.BadRequest);
			CheckRunner(token);

			_sandboxHandler.SaveResult(result);
	    }

		/// <summary>
		/// Get testing results
		/// </summary>
		/// <param name="token"> Runner autherization token </param>
		[HttpPost]
		[Route("PostResults")]
		public void PostResults([FromUri]string token, List<RunningResults> results)
		{
			if (!ModelState.IsValid)
				throw new HttpResponseException(HttpStatusCode.BadRequest);
			CheckRunner(token);

			_sandboxHandler.SaveResults(results);
		}


	    private void CheckRunner(string token)
	    {
			var roles = _user.FindRoles(token);
			if (!roles.Contains(Role.Runner))
				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.Forbidden));
	    }
    }
}
