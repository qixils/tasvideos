﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using TASVideos.Data;
using TASVideos.Data.Entity;
using TASVideos.Data.Helpers;
using TASVideos.Extensions;
using TASVideos.MovieParsers;
using TASVideos.Pages.Submissions.Models;
using TASVideos.Services;
using TASVideos.Services.ExternalMediaPublisher;

namespace TASVideos.Pages.Submissions
{
	[RequirePermission(true, PermissionTo.SubmitMovies, PermissionTo.EditSubmissions)]
	public class EditModel : SubmissionBasePageModel
	{
		private readonly MovieParser _parser;
		private readonly IWikiPages _wikiPages;
		private readonly ExternalMediaPublisher _publisher;

		public EditModel(
			ApplicationDbContext db,
			MovieParser parser,
			IWikiPages wikiPages,
			ExternalMediaPublisher publisher)
			: base(db)
		{
			_parser = parser;
			_wikiPages = wikiPages;
			_publisher = publisher;
		}

		[FromRoute]
		public int Id { get; set; }

		[BindProperty]
		public SubmissionEditModel Submission { get; set; } = new SubmissionEditModel();

		[Display(Name = "Status")]
		public IEnumerable<SubmissionStatus> AvailableStatuses { get; set; } = new List<SubmissionStatus>();

		public IEnumerable<SelectListItem> AvailableTiers { get; set; }

		public IEnumerable<SelectListItem> AvailableRejectionReasons { get; set; } = new List<SelectListItem>();

		public async Task<IActionResult> OnGet()
		{
			// TODO: set up auto-mapper and use ProjectTo<>
			Submission = await Db.Submissions
				.Where(s => s.Id == Id)
				.Select(s => new SubmissionEditModel // It is important to use a projection here to avoid querying the file data which not needed and can be slow
				{
					SystemDisplayName = s.System.DisplayName,
					SystemCode = s.System.Code,
					GameName = s.GameName,
					GameVersion = s.GameVersion,
					RomName = s.RomName,
					Branch = s.Branch,
					Emulator = s.EmulatorVersion,
					FrameCount = s.Frames,
					FrameRate = s.SystemFrameRate.FrameRate,
					RerecordCount = s.RerecordCount,
					CreateTimestamp = s.CreateTimeStamp,
					Submitter = s.Submitter.UserName,
					LastUpdateTimeStamp = s.WikiContent.LastUpdateTimeStamp,
					LastUpdateUser = s.WikiContent.LastUpdateUserName,
					Status = s.Status,
					EncodeEmbedLink = s.EncodeEmbedLink,
					Markup = s.WikiContent.Markup,
					Judge = s.Judge != null ? s.Judge.UserName : "",
					TierId = s.IntendedTierId,
					RejectionReason = s.RejectionReasonId
				})
				.SingleOrDefaultAsync();

			if (Submission == null)
			{
				return NotFound();
			}

			Submission.Authors = await Db.SubmissionAuthors
				.Where(sa => sa.SubmissionId == Id)
				.Select(sa => sa.Author.UserName)
				.ToListAsync();

			// If user can not edit submissions then they must be an author or the original submitter
			if (!User.Has(PermissionTo.EditSubmissions))
			{
				if (Submission.Submitter != User.Identity.Name
					&& !Submission.Authors.Contains(User.Identity.Name))
				{
					return AccessDenied();
				}
			}

			await PopulateDropdowns();

			AvailableStatuses = SubmissionHelper.AvailableStatuses(
				Submission.Status,
				User.Permissions(),
				Submission.CreateTimestamp,
				Submission.Submitter == User.Identity.Name || Submission.Authors.Contains(User.Identity.Name),
				Submission.Judge == User.Identity.Name);

			return Page();
		}

		public async Task<IActionResult> OnPost()
		{
			if (User.Has(PermissionTo.ReplaceSubmissionMovieFile) && Submission.MovieFile != null)
			{
				if (!Submission.MovieFile.IsZip())
				{
					ModelState.AddModelError(nameof(SubmissionCreateModel.MovieFile), "Not a valid .zip file");
				}

				if (Submission.MovieFile.Length > 150 * 1024)
				{
					ModelState.AddModelError(
						nameof(SubmissionCreateModel.MovieFile),
						".zip is too big, are you sure this is a valid movie file?");
				}
			}
			else if (!User.Has(PermissionTo.ReplaceSubmissionMovieFile))
			{
				Submission.MovieFile = null;
			}

			// TODO: this is bad, an author can null out these values,
			// but if we treat null as no choice, then we have no way to unset these values
			if (!User.Has(PermissionTo.JudgeSubmissions))
			{
				Submission.TierId = null;
			}

			var subInfo = await Db.Submissions
				.Where(s => s.Id == Id)
				.Select(s => new
				{
					UserIsJudge = s.Judge != null && s.Judge.UserName == User.Identity.Name,
					UserIsAuthorOrSubmitter = s.Submitter.UserName == User.Identity.Name || s.SubmissionAuthors.Any(sa => sa.Author.UserName == User.Identity.Name),
					CurrentStatus = s.Status,
					CreateDate = s.CreateTimeStamp
				})
				.SingleOrDefaultAsync();

			if (subInfo == null)
			{
				return NotFound();
			}

			var availableStatus = SubmissionHelper.AvailableStatuses(
				subInfo.CurrentStatus,
				User.Permissions(),
				subInfo.CreateDate,
				subInfo.UserIsAuthorOrSubmitter,
				subInfo.UserIsJudge)
				.ToList();

			if (!Submission.TierId.HasValue
				&& (Submission.Status == SubmissionStatus.Accepted || Submission.Status == SubmissionStatus.PublicationUnderway))
			{
				ModelState.AddModelError($"{nameof(Submission)}.{nameof(Submission.TierId)}", "A submission can not be accepted without a Tier");
			}

			if (!availableStatus.Contains(Submission.Status))
			{
				ModelState.AddModelError($"{nameof(Submission)}.{nameof(Submission.Status)}", $"Invalid status: {Submission.Status}");
			}

			if (!ModelState.IsValid)
			{
				await PopulateDropdowns();
				AvailableStatuses = availableStatus;
				return Page();
			}

			// If user can not edit submissions then they must be an author or the original submitter
			if (!User.Has(PermissionTo.EditSubmissions))
			{
				if (!subInfo.UserIsAuthorOrSubmitter)
				{
					return AccessDenied();
				}
			}

			var submission = await Db.Submissions
				.Include(s => s.Judge)
				.Include(s => s.Publisher)
				.Include(s => s.System)
				.Include(s => s.SystemFrameRate)
				.Include(s => s.SubmissionAuthors)
				.ThenInclude(sa => sa.Author)
				.SingleAsync(s => s.Id == Id);

			if (Submission.MovieFile != null)
			{
				// TODO: check warnings
				var parseResult = _parser.Parse(Submission.MovieFile.OpenReadStream());
				await MapParsedResult(parseResult, submission);

				if (!ModelState.IsValid)
				{
					return Page();
				}

				submission.MovieFile = await FormFileToBytes(Submission.MovieFile);
			}

			// If a judge is claiming the submission
			if (Submission.Status == SubmissionStatus.JudgingUnderWay
				&& submission.Status != SubmissionStatus.JudgingUnderWay)
			{
				submission.Judge = await Db.Users.SingleAsync(u => u.UserName == User.Identity.Name);
			}
			else if (submission.Status == SubmissionStatus.JudgingUnderWay // If judge is unclaiming, remove them
				&& Submission.Status == SubmissionStatus.New
				&& submission.Judge != null)
			{
				submission.Judge = null;
			}

			if (Submission.Status == SubmissionStatus.PublicationUnderway
				&& submission.Status != SubmissionStatus.PublicationUnderway)
			{
				submission.Publisher = await Db.Users.SingleAsync(u => u.UserName == User.Identity.Name);
			}
			else if (submission.Status == SubmissionStatus.Accepted // If publisher is unclaiming, remove them
				&& Submission.Status == SubmissionStatus.PublicationUnderway)
			{
				submission.Publisher = null;
			}

			bool statusHasChanged = false;
			if (submission.Status != Submission.Status)
			{
				statusHasChanged = true;
				var history = new SubmissionStatusHistory
				{
					SubmissionId = submission.Id,
					Status = Submission.Status
				};
				submission.History.Add(history);
				Db.SubmissionStatusHistory.Add(history);

				submission.RejectionReasonId = Submission.Status == SubmissionStatus.Rejected
					? Submission.RejectionReason
					: null;
			}

			submission.IntendedTier = Submission.TierId.HasValue
				? await Db.Tiers.SingleAsync(t => t.Id == Submission.TierId.Value)
				: null;

			submission.GameVersion = Submission.GameVersion;
			submission.GameName = Submission.GameName;
			submission.EmulatorVersion = Submission.Emulator;
			submission.Branch = Submission.Branch;
			submission.RomName = Submission.RomName;
			submission.EncodeEmbedLink = Submission.EncodeEmbedLink;
			submission.Status = Submission.Status;

			var revision = new WikiPage
			{
				PageName = $"{LinkConstants.SubmissionWikiPage}{Id}",
				Markup = Submission.Markup,
				MinorEdit = Submission.MinorEdit,
				RevisionMessage = Submission.RevisionMessage,
			};
			await _wikiPages.Add(revision);

			submission.WikiContent = revision;

			submission.GenerateTitle();
			await Db.SaveChangesAsync();

			if (!Submission.MinorEdit)
			{
				string title;
				if (statusHasChanged)
				{
					var statusStr = Submission.Status == SubmissionStatus.Accepted
						? $"{Submission.Status.ToString()} to {(await Db.Tiers.SingleAsync(t => t.Id == Submission.TierId)).Name}" 
						: Submission.Status.ToString();
					title = $"Submission {submission.Title} {statusStr} by {User.Identity.Name}";
				}
				else
				{
					title = $"Submission {submission.Title} edited by {User.Identity.Name}";
				}

				_publisher.SendSubmissionEdit(title, $"{BaseUrl}/{Id}S");
			}

			return Redirect($"/{Id}S");
		}

		private async Task PopulateDropdowns()
		{
			AvailableTiers = await Db.Tiers
				.ToDropdown()
				.ToListAsync();

			AvailableRejectionReasons = await Db.SubmissionRejectionReasons
				.ToDropdown()
				.ToListAsync();
		}
	}
}
