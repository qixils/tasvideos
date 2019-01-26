﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc.Rendering;

using TASVideos.Data;
using TASVideos.Data.Constants;
using TASVideos.Data.Entity.Forum;

namespace TASVideos.Models
{
	public interface IForumTopicActionBar
	{
		int Id { get; }
		bool IsLocked { get; }
		bool IsWatching { get; }
		string Title { get; }
	}

	public interface IForumTopicBreadCrumb
	{
		int Id { get; }
		string Title { get; }
		bool IsLocked { get; }
		int ForumId { get; }
		string ForumName { get; }
	}

	public class ForumTopicModel : IForumTopicActionBar, IForumTopicBreadCrumb
	{
		public int Id { get; set; }
		public bool IsWatching { get; set; }
		public bool IsLocked { get; set; }
		public string Title { get; set; }
		public int ForumId { get; set; }
		public string ForumName { get; set; }

		public PageOf<ForumPostEntry> Posts { get; set; }
		public PollModel Poll { get; set; }

		public class ForumPostEntry
		{
			public int Id { get; set; }
			public int TopicId { get; set; }
			public bool Highlight { get; set; }
			public int PosterId { get; set; }
			public string PosterName { get; set; }
			public string PosterAvatar { get; set; }
			public string PosterLocation { get; set; }
			public int PosterPostCount { get; set; }
			public DateTime PosterJoined { get; set; }
			public IEnumerable<string> PosterRoles { get; set; }
			public string Text { get; set; }
			public string RenderedText { get; set; }
			public string Subject { get; set; }
			public string Signature { get; set; }
			public string RenderedSignature { get; set; }

			public IEnumerable<AwardDisplayModel> Awards { get; set; } = new List<AwardDisplayModel>();

			public bool EnableHtml { get; set; }
			public bool EnableBbCode { get; set; }

			[Sortable]
			public DateTime CreateTimestamp { get; set; }

			public bool IsLastPost { get; set; }
			public bool IsEditable { get; set; }
			public bool IsDeletable { get; set; }
		}

		public class PollModel
		{
			public int PollId { get; set; }
			public string Question { get; set; }

			public IEnumerable<PollOptionModel> Options { get; set; } = new List<PollOptionModel>();

			public class PollOptionModel
			{
				public string Text { get; set; }
				public int Ordinal { get; set; }
				public ICollection<int> Voters { get; set; } = new List<int>();
			}
		}
	}

	public class ForumRequest : PagedModel
	{
		public ForumRequest()
		{
			PageSize = 50;
			SortDescending = true;
			SortBy = nameof(ForumModel.ForumTopicEntry.CreateTimestamp);
		}
	}

	public class ForumModel
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }

		public PageOf<ForumTopicEntry> Topics { get; set; }

		public class ForumTopicEntry
		{
			public int Id { get; set; }
			public string Title { get; set; }

			[Display(Name = "Author")]
			public string CreateUserName { get; set; }

			[Sortable]
			public DateTime CreateTimestamp { get; set; }

			public int PostCount { get; set; }
			public int Views { get; set; }

			public ForumTopicType Type { get; set; }

			public DateTime? LastPost { get; set; }
		}
	}

	public class TopicRequest : PagedModel
	{
		public TopicRequest()
		{
			PageSize = ForumConstants.PostsPerPage;
			SortDescending = false;
			SortBy = nameof(ForumTopicModel.ForumPostEntry.CreateTimestamp);
		}

		public int? Highlight { get; set; }
	}

	public class PostPositionModel
	{
		public int Page { get; set; }
		public int TopicId { get; set; }
	}

	// TODO: rename me? This is a confusing name
	public class TopicCreatePostModel
	{
		public string ForumName { get; set; }

		[Required]
		[StringLength(100, MinimumLength = 5)]
		public string Title { get; set; }

		[Required]
		[StringLength(1000, MinimumLength = 5)]
		public string Post { get; set; }

		public ForumTopicType Type { get; set; } = ForumTopicType.Regular;
	}

	/// <summary>
	/// Data necessary to create a post
	/// </summary>
	public class ForumPostModel
	{
		public string TopicTitle { get; set; }
		public string Subject { get; set; }
		public string Text { get; set; }
	}

	/// <summary>
	/// Data necessary to present to the user for creating a post
	/// as well as the data necessary to create a post
	/// </summary>
	public class ForumPostCreateModel : ForumPostModel
	{
		public bool IsLocked { get; set; }
		public string UserAvatar { get; set; }
		public string UserSignature { get; set; }
	}

	public class ForumPostEditModel
	{
		public int PosterId { get; set; }
		public string PosterName { get; set; }
		public DateTime CreateTimestamp { get; set; }

		public bool EnableBbCode { get; set; }
		public bool EnableHtml { get; set; }

		public int TopicId { get; set; }
		public string TopicTitle { get; set; }

		public string Subject { get; set; }
		public string Text { get; set; }
		public string RenderedText { get; set; }

		public bool IsLastPost { get; set; }
	}

	public class PollResultModel
	{
		public string TopicTitle { get; set; }
		public int TopicId { get; set; }

		public int PollId { get; set; }
		public string Question { get; set; }

		public IEnumerable<VoteResult> Votes { get; set; } = new List<VoteResult>();

		public class VoteResult
		{
			public int UserId { get; set; }

			[Display(Name = "User")]
			public string UserName { get; set; }

			public int Ordinal { get; set; }

			[Display(Name = "Option")]
			public string OptionText { get; set; }

			[Display(Name = "Vote On")]
			public DateTime CreateTimestamp { get; set; }

			[Display(Name = "IP Address")]
			public string IpAddress { get; set; }
		}
	}

	public class PostsSinceLastVisitModel
	{
		public int Id { get; set; }
		public DateTime CreateTimestamp { get; set; }
		public bool EnableBbCode { get; set; }
		public bool EnableHtml { get; set; }
		public string Text { get; set; }
		public string RenderedText { get; set; }
		public string Subject { get; set; }
		public int TopicId { get; set; }
		public string TopicTitle { get; set; }
		public int ForumId { get; set; }
		public string ForumName { get; set; }

		public int PosterId { get; set; }
		public string PosterName { get; set; }
		public string PosterAvatar { get; set; }
		public string PosterLocation { get; set; }
		public IEnumerable<string> PosterRoles { get; set; }
		public int PosterPostCount { get; set; }
		public DateTime PosterJoined { get; set; }
		public string Signature { get; set; }
		public string RenderedSignature { get; set; }

		public IEnumerable<AwardDisplayModel> Awards { get; set; } = new List<AwardDisplayModel>();
	}

	public class UnansweredPostsModel
	{
		public int ForumId { get; set; }

		[Display(Name = "Forum")]
		public string ForumName { get; set; }

		public int TopicId { get; set; }

		[Display(Name = "Topic")]
		public string TopicName { get; set; }

		public int AuthorId { get; set; }

		[Display(Name = "Author")]
		public string AuthorName { get; set; }

		[Display(Name = "Posted On")]
		public DateTime PostDate { get; set; }
	}

	public class MoveTopicModel
	{
		[Display(Name = "New Forum")]
		public int ForumId { get; set; }

		[Display(Name = "Topic")]
		public string TopicTitle { get; set; }

		[Display(Name = "Current Forum")]
		public string ForumName { get; set; }
	}

	public class ForumEditModel
	{
		[Required]
		public string Name { get; set; }

		[Required]
		public string Description { get; set; }
	}

	public class CategoryEditModel
	{
		[Required]
		[StringLength(30)]
		public string Title { get; set; }

		public string Description { get; set; }

		public IList<ForumEditModel> Forums { get; set; } = new List<ForumEditModel>();

		public class ForumEditModel
		{
			public int Id { get; set; }

			[Required]
			[StringLength(50)]
			public string Name { get; set; }

			public string Description { get; set; }
			public int Ordinal { get; set; }
		}
	}

	public class SplitTopicModel
	{
		[Required]
		[Display(Name = "Split On Post")]
		public int? PostToSplitId { get; set; }

		[Display(Name = "Create New Topic In")]
		public int SplitToForumId { get; set; }

		[Required]
		[Display(Name = "New Topic Name")]
		public string SplitTopicName { get; set; }

		public string Title { get; set; }

		public int ForumId { get; set; }
		public string ForumName { get; set; }

		public IEnumerable<Post> Posts { get; set; } = new List<Post>();

		public class Post
		{
			public int Id { get; set; }
			public DateTime PostCreateTimeStamp { get; set; }
			public bool EnableHtml { get; set; }
			public bool EnableBbCode { get; set; }
			public string Subject { get; set; }
			public string Text { get; set; }
			public int PosterId { get; set; }
			public string PosterName { get; set; }
			public string PosterAvatar { get; set; }
		}
	}

	public class UserPostsRequest : PagedModel
	{
		public UserPostsRequest()
		{
			PageSize = ForumConstants.PostsPerPage;
			SortDescending = true;
			SortBy = nameof(ForumTopicModel.ForumPostEntry.CreateTimestamp);
		}
	}

	public class UserPostsModel
	{
		public int Id { get; set; }
		public string UserName { get; set; }
		public DateTime Joined { get; set; }
		public string Location { get; set; }
		public string Avatar { get; set; }
		public string Signature { get; set; }
		public string RenderedSignature { get; set; }

		public IEnumerable<string> Roles { get; set; } = new List<string>();
		public IEnumerable<AwardDisplayModel> Awards { get; set; } = new List<AwardDisplayModel>();

		public PageOf<Post> Posts { get; set; }

		public class Post
		{
			public int Id { get; set; }

			[Sortable]
			public DateTime CreateTimestamp { get; set; }
			public bool EnableBbCode { get; set; }
			public bool EnableHtml { get; set; }
			public string Text { get; set; }
			public string RenderedText { get; set; }
			public string Subject { get; set; }
			public int TopicId { get; set; }
			public string TopicTitle { get; set; }
			public int ForumId { get; set; }
			public string ForumName { get; set; }
		}
	}
}
