using FindJobs.Web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Test.Web.Controllers
{
    public class CompanyController : BaseController
    {
        public CompanyController(IConfiguration configuration) : base(configuration)
        {
        }

        [HttpPost]
        public async Task<ResultDto> SaveCompany([FromForm] CompanyDto companyDto)
        {
            if (companyDto.ImageLogo is not null)
            {
                HttpContext.Session.Remove("ClientImage");
                var imageByte = await ConvertToByteAsync(companyDto.ImageLogo);
                companyDto.ImageLogoByte = imageByte;
                companyDto.FileImageLogo = companyDto.ImageLogo.FileName;
                companyDto.ImageLogo = null;
            }
            else
            {
                HttpContext.Session.Remove("ClientImage");
            }

            var data = await API.PostData<ResultDto<bool>>(configuration["GlobalSettings:ApiUrl"],
                "Company/UpdateCompany"
                , companyDto, Request.Cookies["AuthToken"]);

            if (data is null || data.MessageCode is not MessageCodes.Success)
                return new ResultDto(MessageCodes.UnHandleException);

            return new ResultDto(MessageCodes.Success);
        }
    }
}
