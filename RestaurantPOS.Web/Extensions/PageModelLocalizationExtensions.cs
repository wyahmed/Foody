using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using RestaurantPOS.Web.Resources;

namespace RestaurantPOS.Web.Extensions;

public static class PageModelLocalizationExtensions
{
    public static string T(this PageModel pageModel, string key, params object[] arguments)
    {
        var localizer = pageModel.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>();
        return arguments.Length == 0 ? localizer[key] : localizer[key, arguments];
    }

    public static void SetSuccessMessage(this PageModel pageModel, string key, params object[] arguments)
        => pageModel.TempData["Success"] = pageModel.T(key, arguments);

    public static void SetErrorMessage(this PageModel pageModel, string key, params object[] arguments)
        => pageModel.TempData["Error"] = pageModel.T(key, arguments);
}
