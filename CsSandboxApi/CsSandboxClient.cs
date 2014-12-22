﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

namespace CsSandboxApi
{
	public class CsSandboxClient
	{
		private readonly string _token;
		private readonly HttpClient _httpClient;

		public CsSandboxClient(string token, string baseAddress)
		{
			_token = token;
			_httpClient = new HttpClient {BaseAddress = new Uri(baseAddress)};
			_httpClient.DefaultRequestHeaders.Accept.Clear();
			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		}

		public async Task<Submission> CreateSubmit(string code, string input, bool needRun = true)
		{
			var model = new SubmissionModel
			{
				Code = code,
				Input = input,
				NeedRun = needRun,
				Token = _token
			};

			var response = await _httpClient.PostAsJsonAsync("/CreateSubmission", model);

			if (!response.IsSuccessStatusCode)
			{
				throw CsSandboxClientException.Create(response);
			}

			var submissionId = await response.Content.ReadAsStringAsync();
			return new Submission(submissionId, this);
		}

		public async Task<SubmissionStatus> GetSubmissionStatus(string submissionId)
		{
			var uri = GetUriForSubmission("/GetSubmissionStatus", submissionId);
			var response = await _httpClient.GetAsync(uri);
			if (!response.IsSuccessStatusCode)
			{
				throw CsSandboxClientException.Create(response);
			}

			return await response.Content.ReadAsAsync<SubmissionStatus>();
		}

		public async Task<PublicSubmissionDetails> GetSubmissionDetails(string submissionId)
		{
			var uri = GetUriForSubmission("/GetSubmissionDetails", submissionId);
			var response = await _httpClient.GetAsync(uri);
			if (!response.IsSuccessStatusCode)
			{
				throw CsSandboxClientException.Create(response);
			}

			return await response.Content.ReadAsAsync<PublicSubmissionDetails>();
		}

		public async Task<PublicSubmissionDetails> Submit(string code, string input)
		{
			var submission = await CreateSubmit(code, input);

			var count = 0;
			var lastStatus = await submission.GetStatus();
			while (lastStatus != SubmissionStatus.Done && count < 30)
			{
				await Task.Delay(1000);
				++count;
				lastStatus = await submission.GetStatus();
			}
			if (lastStatus != SubmissionStatus.Done)
				return null;

			return await submission.GetDetails();
		}

		private string GetUriForSubmission(string path, string submissionId)
		{
			var query = HttpUtility.ParseQueryString(string.Empty);
			query["id"] = submissionId;
			query["token"] = _token;
			return path + "/?" + query;
		}
	}
}
