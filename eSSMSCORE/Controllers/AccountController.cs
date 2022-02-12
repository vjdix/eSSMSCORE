using eSSMSCORE.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using eSSMSCORE.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace eSSMSCORE.Controllers
{

    public class AccountController : BaseController
    {
        Account Account = new Account();

        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<Account> _logger;
        private readonly IEmailSender _emailSender;

        public AccountController(SignInManager<IdentityUser> signInManager, ILogger<Account> logger, UserManager<IdentityUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl = "/Home/Index")
        {
            if (!string.IsNullOrEmpty(Account.ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, Account.ErrorMessage);
            }
            returnUrl ??= Url.Content("~/");
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            Account.ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            Account.ReturnUrl = returnUrl;
            ViewData["ReturnUrl"] = returnUrl;
            return View(Account);
        }

        [HttpPost]
        public async Task<IActionResult> Login(string Email = "", string Password = "", bool RememberMe = false, string returnUrl = "/Home/Index")
        {
            returnUrl ??= Url.Content("~/");

            Account.ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(Email, Password, RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToAction("LoginWith2fa", new { returnUrl = returnUrl, RememberMe = RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View();
                }
            }

            // If we got this far, something failed, redisplay form
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> LoginWith2fa(bool RememberMe, string returnUrl = "/Home/Index")
        {
            // Ensure the user has gone through the username & password screen first
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

            if (user == null)
            {
                throw new InvalidOperationException($"Unable to load two-factor authentication user.");
            }

            Account.ReturnUrl = returnUrl;
            Account.RememberMe = RememberMe;

            return View(Account);
        }

        [HttpPost]
        public async Task<IActionResult> LoginWithTwoFactor(string TwoFactorCode = "", bool RememberMachine = false, bool RememberMe = false, string returnUrl = "/Home/Index")
        {
            if (!ModelState.IsValid)
            {
                return View("LoginWith2fa");
            }

            returnUrl = returnUrl ?? Url.Content("~/");

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new InvalidOperationException($"Unable to load two-factor authentication user.");
            }

            var authenticatorCode = TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, RememberMe, RememberMachine);

            if (result.Succeeded)
            {
                _logger.LogInformation("User with ID '{UserId}' logged in with 2fa.", user.Id);
                return LocalRedirect(returnUrl);
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning("User with ID '{UserId}' account locked out.", user.Id);
                return RedirectToPage("./Lockout");
            }
            else
            {
                _logger.LogWarning("Invalid authenticator code entered for user with ID '{UserId}'.", user.Id);
                ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
                return View("LoginWith2fa");
            }
        }

    }
}
