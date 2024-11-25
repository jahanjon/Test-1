using DotNek.Common.Dtos;
using DotNek.Common.Helpers;
using DotNek.WebComponents.Areas.Auth.Models;
using DotNek.WebComponents.Areas.ResourceManagement;
using FindJobs.Domain.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FindJobs.Web.Controllers
{
    public class BaseController : Controller
    {
        protected readonly IConfiguration configuration;

        public BaseController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {

            var culture = (filterContext.RouteData.Values["culture"] ?? new CultureDto().CultureCode).ToString();
            ViewData["Culture"] = culture;
            ChangeThreadCulture(culture);
            var cultureList = Domain.Global.Global.GetCultureList(configuration["GlobalSettings:ApiUrl"]).Result;
            ViewData["CultureList"] = cultureList;
            var countryList = Domain.Global.Global.GetCountryList(configuration["GlobalSettings:ApiUrl"]).Result;
            ViewData["CountryList"] = countryList;

            var cultureDto = culture is not null
                ? cultureList.Find(x =>
                    String.Equals(x.CultureCode, culture, StringComparison.CurrentCultureIgnoreCase))
                : new CultureDto();

            ReadAllColors(cultureDto);

            ViewData["jobCategoryWithKeyValue"] = new WebResources().GetViewData(configuration["UISettings:ResourceAssembly"], culture.ToString(), "JobCategories");

            if (ViewData["AuthModel"] == null)
            {
                ViewData["AuthModel"] = new AuthConfiguration()
                {
                    DarkColor = ViewData["DarkColor"].ToString(),
                    LightColor = ViewData["LightColor"].ToString(),
                    TermsLink = configuration["AuthSettings:TermsLink"],
                    GoogleAuthClientId = configuration["AuthSettings:GoogleAuthClientId"],
                    FacebookAppId = configuration["AuthSettings:FacebookAppId"],
                    LinkedinClientId = configuration["AuthSettings:ClientIdLinkedin"],

                    CaptchaSiteKey = configuration["GlobalSettings:GoogleCaptchaSiteKey"],
                    IsCaptchaVisible = false,
                    Culture = culture,
                    AuthLoginTabs = new List<AuthLoginTab>
                    {
                        new() {Role=Res.Layout.LoginRole1,RedirectUrl= Url.Action("Profile", "Applicant") ,HasSocialButton=true},
                        new() {Role=Res.Layout.LoginRole2,RedirectUrl= Url.Action("AddJobOffer", "Company") ,HasSocialButton=false},
                    }
                };

            }

            

            if (ViewData["Regions"] == null || ViewData["JobCategoy"] == null || ViewData["Categories"] == null)
            {
                if (cultureDto.CountryCode == "US" || cultureDto.CountryCode == "CA")
                {
                    if (ViewData["Regions"] is null)
                    {
                        ViewData["Regions"] = GetAllStates(cultureDto.CountryCode).Result;
                    }

                }
                else
                {
                    if (ViewData["Regions"] is null)
                    {
                        ViewData["Regions"] = GetAllCities(cultureDto.CountryCode).Result.Take(10).ToList();
                    }
                }

                ViewData["JobCategoy"] = Startup.JobCategories.ToList();
                ViewData["Categories"] = ConvertCategories();
            }

            CheckForTheTokenState(culture);

            var metaTag = GetMetaData(filterContext.RouteData.Values["controller"].ToString(), filterContext.RouteData.Values["action"].ToString());
            ViewData["DescriptionPage"] = metaTag.MetaDescription;
            ViewData["KeywordsPage"] = metaTag.MetaKeywords;

        }

        private void CheckForTheTokenState(string culture)
        {
            if (!string.IsNullOrEmpty(Request.Cookies["AuthToken"]) && HttpContext.Session.Get("ClientImage") == null)
            {
                var ClientImage = GetClientImage(Request.Cookies["AuthToken"]);
                if (ClientImage != null)
                    HttpContext.Session.SetString("ClientImage", ClientImage);
                if (string.IsNullOrWhiteSpace(ClientImage))
                {
                    ClearBrowserCache(culture);
                }
            }
        }

        private void ReadAllColors(CultureDto cultureDto)
        {
            if (ViewData["ProjectName"] == null)
            {
                ViewData["CurrentCultureDto"] = cultureDto;
                ViewData["ProjectName"] = configuration["GlobalSettings:ProjectName"];
                ViewData["DarkColor"] = configuration["UISettings:DarkColor"];
                ViewData["LightColor"] = configuration["UISettings:LightColor"];
                ViewData["HeaderColorDark"] = ViewData["DarkColor"];
                ViewData["HeaderColorLight"] = ViewData["LightColor"];
                ViewData["ColorWhite"] = configuration["UISettings:ColorWhite"];
                ViewData["ColorGray"] = configuration["UISettings:ColorGray"];
                ViewData["ColorLightGray"] = configuration["UISettings:ColorLightGray"];
                ViewData["DarkBlue"] = configuration["UISettings:DarkBlue"];
                ViewData["LightRed"] = configuration["UISettings:LightRed"];
                ViewData["DarkRed"] = configuration["UISettings:DarkRed"];
            }
        }

        private void ClearBrowserCache(string culture)
        {
            HttpContext.Response.Cookies.Delete("AuthToken");
            HttpContext.Session.Clear();
            Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Add("Pragma", "no-cache");
            Response.Headers.Add("Expires", "0");
            Redirect("/" + culture + "/");
        }

        private static void ChangeThreadCulture(string culture)
        {
            var newCulture = new CultureInfo(culture);
            CultureInfo.DefaultThreadCurrentCulture = newCulture;
            CultureInfo.DefaultThreadCurrentUICulture = newCulture;
            Thread.CurrentThread.CurrentCulture = newCulture;
            Thread.CurrentThread.CurrentUICulture = newCulture;
        }

        public static string PlaceholderImage(int width, int height)
        {
            byte[] byteArray;
            var stream = new MemoryStream();
            if (width == 0 || height == 0)
            {
                byteArray = new byte[] { };
            }
            else
            {
                if (width > 50 || height > 50)
                {
                    width = (int)Math.Round((double)width / 10, 0);
                    height = (int)Math.Round((double)height / 10, 0);
                }
                var info = new SKImageInfo(width, height);
                var surface = SKSurface.Create(info);
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);
                var image = surface.Snapshot();
                var data = image.Encode(SKEncodedImageFormat.Png, 100);
                data.SaveTo(stream);
            }
            byteArray = stream.ToArray();
            return "data:image/png;base64, " + Convert.ToBase64String(byteArray);
        }

        public string GetClientImage(string token)
        {
            var currentImage = API.GetData<ResultDto<string>>(configuration["GlobalSettings:ApiUrl"], "Shared/GetCurrentUserImage", token).Result;
            if (currentImage is not null)
                return currentImage.Data;
            return null;
        }

        private async Task<List<CityDto>> GetAllCities(string countryCode)
        {
            return
               (await API.GetData<ResultDto<List<CityDto>>>
               (configuration["GlobalSettings:ApiUrl"],
                $"Culture/GetCities?countryCode={countryCode}")).Data;
        }

        private async Task<List<StateDto>> GetAllStates(string countryCode)
        {
            return
               (await API.GetData<ResultDto<List<StateDto>>>
               (configuration["GlobalSettings:ApiUrl"],
                $"Culture/GetCities?countryCode={countryCode}")).Data;
        }

        public List<JobCategoryDto> ConvertCategories()
        {

            var culture = ViewData["Culture"];
            var job = new WebResources().GetViewData(configuration["UISettings:ResourceAssembly"], culture.ToString(), "JobCategories");

            var result = new List<JobCategoryDto>();
            if (ViewData["JobCategoy"] is not null)
            {
                foreach (var item in ViewData["JobCategoy"] as List<JobCategoryDto>)
                {
                    foreach (var resKey in job)
                    {
                        if (resKey.Key == item.Jobcategory)
                        {
                            result.Add(new JobCategoryDto
                            {
                                Id = item.Id,
                                Jobcategory = resKey.Value + " " + @Res.Layout.In + "  " + Thread.CurrentThread.CurrentCulture.NativeName.Split('(')[1].Replace(")", string.Empty)
                            });
                        }

                    }

                }
            }
            return result;
        }

        private MetaTagDto GetMetaData(string controller,string action)
        {
            var description = Res.MetaTag.ResourceManager.GetString(controller+"_"+action+"_"+"description");
            var keywords = Res.MetaTag.ResourceManager.GetString(controller+"_"+action+"_"+"keywords");

            return new MetaTagDto()
            {
                MetaDescription = description,
                MetaKeywords = keywords,
            };
        }

    }
}