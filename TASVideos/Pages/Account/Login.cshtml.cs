﻿using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TASVideos.Core.Services;

namespace TASVideos.RazorPages.Pages.Account
{
	[AllowAnonymous]
	[IpBanCheck]
	public class LoginModel : BasePageModel
	{
		private readonly SignInManager _signInManager;

		public LoginModel(SignInManager signInManager)
		{
			_signInManager = signInManager;
		}

		[FromQuery]
		public string? ReturnUrl { get; set; }

		[BindProperty]
		[Required]
		[Display(Name = "User Name")]
		public string UserName { get; set; } = "";

		[BindProperty]
		[Required]
		[DataType(DataType.Password)]
		public string Password { get; set; } = "";

		[BindProperty]
		[Display(Name = "Remember me?")]
		public bool RememberMe { get; set; }

		public async Task OnGet()
		{
			await HttpContext.SignOutAsync();
		}

		public async Task<IActionResult> OnPost()
		{
			if (!ModelState.IsValid)
			{
				return Page();
			}

			var result = await _signInManager.SignInWithLegacySupport(UserName, Password, RememberMe);

			if (result.Succeeded)
			{
				return RedirectToLocal(ReturnUrl);
			}

			if (result.IsLockedOut)
			{
				return RedirectToPage("/Account/Lockout");
			}

			ModelState.AddModelError(string.Empty, "Invalid login attempt.");
			return Page();
		}
	}
}
