using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RestaurantPOS.Application.Features.Auth.Commands;
using RestaurantPOS.Infrastructure.Identity;
using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Web.Pages.Account;

/// <summary>Login page model – validates credentials via MediatR and sets cookie authentication.</summary>
public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IMediator _mediator;

    public LoginModel(SignInManager<ApplicationUser> signInManager, IMediator mediator)
    {
        _signInManager = signInManager;
        _mediator = mediator;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsArabic => Request.Cookies[".AspNetCore.Culture"]?.Contains("ar") ?? true;

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/Dashboard");
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/Dashboard");

        if (!ModelState.IsValid) return Page();

        var result = await _signInManager.PasswordSignInAsync(
            Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return LocalRedirect(returnUrl);
        }
        if (result.IsLockedOut)
        {
            ErrorMessage = IsArabic ? "الحساب مقفل. حاول لاحقاً." : "Account locked. Try again later.";
            return Page();
        }

        ErrorMessage = IsArabic ? "بيانات دخول غير صحيحة." : "Invalid credentials.";
        return Page();
    }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}
