using DotNek.Common.Dtos;
using FindJobs.Domain.Dtos;
using FindJobs.Domain.Enums;
using FindJobs.Domain.Services;
using FindJobs.Domain.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FindJobs.Domain.Dtos.FileManager;
using System;
using FindJobs.DataAccess.Migrations;

namespace FindJobs.WebApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService companyService;
        private readonly IConfiguration configuration;
        private readonly ICountryService countryService;
        private readonly ICitiesService cityService;
        private readonly IFileManagerService fileManagerService;

        public CompanyController(ICompanyService companyService, IConfiguration configuration, ICitiesService cityService, ICountryService countryService, IFileManagerService fileManagerService)
        {
            this.companyService = companyService;
            this.configuration = configuration;
            this.cityService = cityService;
            this.countryService = countryService;
            this.fileManagerService = fileManagerService;
        }

        [HttpGet]
        public async Task<IActionResult> ProfileLogo(int id)
        {
            var byteFile = fileManagerService.ConvertFileTobyte(new List<string>()
                {
                    configuration["FileManager:DirectoryFilesDefaultCompany"],
                });

            if (id is 0)
                return new FileStreamResult(new MemoryStream(byteFile.Data.ByteFile), byteFile.Data.ContentType);

            var company = await companyService.GetCompanyById(id);
            if (company is null)
                return new FileStreamResult(new MemoryStream(byteFile.Data.ByteFile), byteFile.Data.ContentType);

            var image = fileManagerService.ConvertFileTobyte(new List<string>()
                {
                    configuration["FileManager:DirectoryFileCompany"],
                    company.Id.ToString(),
                    company.FileImageLogo,
                });

            if (image is null || image.MessageCode is not MessageCodes.Success)
                return new FileStreamResult(new MemoryStream(byteFile.Data.ByteFile), byteFile.Data.ContentType);

            return new FileStreamResult(new MemoryStream(image.Data.ByteFile), image.Data.ContentType);
        }
        [HttpPost]
        public async Task<ResultDto> SendOffer(string applicantEmail, Guid id)
        {
            var result = await companyService.SendOfferEmails(applicantEmail, id);
            if (result == EmailSendResult.Success)
            {
                return new ResultDto(MessageCodes.Success);
            }
            return new ResultDto(MessageCodes.BadRequest);
        }
        [HttpGet]
        public async Task<ResultDto<OfferDto>> GetOffer(Guid id)
        {
            var result = await companyService.GetOfferById(id);
            if (result != null)
                return new ResultDto<OfferDto>(result);
            return new ResultDto<OfferDto>(null, MessageCodes.BadRequest);
        }
        [HttpGet]
        public async Task<ResultDto<List<CompanyDto>>> GetTopEmployers()
        {
            var topEmployers = await companyService.GetTopEmployers();
            return new ResultDto<List<CompanyDto>>(topEmployers);
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult GetRoleClaims()
        {
            //TODO: securiry issue
            if (companyService.GetCompanyRole(User))
                return new JsonResult(new ResultDto(MessageCodes.Success));
            else
                return new JsonResult(new ResultDto(MessageCodes.Unauthorized));
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<JobOfferViewModel>> GetOfferViewModel()
        {
            var email = GetCompanyEmail().Data;
            var offerViewModel = await companyService.GetJobOfferViewModel(email);
            var countriesWithCurrencies = countryService.GetCurrenciesFromCountries(offerViewModel.CompanyDto.CountryCode);
            offerViewModel.OfferDto.CurrencyCode = countriesWithCurrencies;
            offerViewModel.JobCategories = await Domain.Global.Global.GetJobCategories(configuration["GlobalSettings:ApiUrl"]);
            var resultDto = new ResultDto<JobOfferViewModel>(offerViewModel);
            return resultDto;
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<JobOfferViewModel>> GetJobOfferViewModelForUpdate(Guid offerId)
        {
            var email = GetCompanyEmail().Data;
            var offerViewModel = await companyService.GetJobOfferViewModelForUpdate(offerId, email);

            if (offerViewModel != null)
            {
                offerViewModel.JobCategories = await Domain.Global.Global.GetJobCategories(configuration["GlobalSettings:ApiUrl"]);
            }

            var resultDto = new ResultDto<JobOfferViewModel>(offerViewModel);
            return resultDto;
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ResultDto<PaginationDto<OfferMobileModel>> GetOffersByCompanyEmail(int currentPage = 1, OfferStatus status = OfferStatus.All, string offerName = "")
        {
            var email = GetCompanyEmail().Data;
            var OfferMobileList = companyService.GetOffersByCompanyEmail(email, currentPage, status, offerName);
            return new ResultDto<PaginationDto<OfferMobileModel>>(OfferMobileList);
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<CompanyDto>> GetCompany()
        {
            var claims = User.Claims.ToList();
            var companyEmail = claims.FirstOrDefault(x => x.Type.Equals(ClaimTypes.NameIdentifier))?.Value;

            var companyDto = await companyService.GetCompanyByEmail(companyEmail);

            var resultDto = new ResultDto<CompanyDto>(companyDto);
            return resultDto;
        }
        private ResultDto<string> GetCompanyEmail()
        {
            var claims = User.Claims.ToList();
            var companyEmail = claims.FirstOrDefault(x => x.Type.Equals(ClaimTypes.NameIdentifier))?.Value;
            return new ResultDto<string>(companyEmail);
        }
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<Guid>> SaveOrUpdateJobOffer(OfferDto offerDto)
        {
            offerDto.CompanyEmail = GetCompanyEmail().Data;
            var offerId = await companyService.SaveOrUpdateJobOffer(offerDto);
            if (offerId != Guid.Empty)
            {
                return new ResultDto<Guid>(offerId);
            }
            return new ResultDto<Guid>(Guid.Empty, MessageCodes.BadRequest);
        }


        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<bool>> UpdateCompany([FromBody] CompanyDto companyDto)
        {
            companyDto.Email = GetCompanyEmail().Data;
            companyDto.CountryCode = countryService.GetCountryCode(companyDto.CountryCode);
            long cityId;
            if (!string.IsNullOrEmpty(companyDto.StateName))
            {
                cityId = cityService.GetCityIdByName(companyDto.CityName, companyDto.StateName);
            }
            else
            {
                cityId = cityService.GetCityIdByName(companyDto.CityName);
            }

            if (cityId != 0)
            {
                companyDto.CityId = cityId;
            }

            if (companyDto.FileImageLogo is not null)
            {
                var company = await companyService.GetCompanyByEmail(companyDto.Email);
                var idCompany = company.Id;
                var extensionFile = Path.GetExtension(companyDto.FileImageLogo);
                if (fileManagerService.IsImage(extensionFile).Data)
                {
                    var iformFile = fileManagerService.ConvertArrayToIFormFile(new ArrayFile()
                    {
                        ByteArray = companyDto.ImageLogoByte,
                        FileName = companyDto.FileImageLogo,
                        Name = companyDto.FileImageLogo
                    });
                    if (iformFile.MessageCode is not MessageCodes.Success)
                        return new ResultDto<bool>(false, MessageCodes.BadRequest);

                    var webpFile = await fileManagerService.ConvertIFormFileToWebp(iformFile.Data);

                    if (webpFile.MessageCode is not MessageCodes.Success)
                        return new ResultDto<bool>(false, MessageCodes.BadRequest);

                    var changePlace = await fileManagerService.ChangePlaceWebpFile(new PlaceFileWebp()
                    {
                        DestinationCombinePath = new List<string>() {
                                    configuration["FileManager:DirectoryFileCompany"],
                                    idCompany.ToString() },
                        StreamFile = webpFile.Data.Stream,
                        FileName = webpFile.Data.Name,
                        WhichTypeCreateName = NameType.NameWithDateTime
                    });

                    if (changePlace.MessageCode is not MessageCodes.Success)
                        return new ResultDto<bool>(false, MessageCodes.BadRequest);

                    companyDto.Logo = changePlace.Data.NameFile;
                    companyDto.FileImageLogo = changePlace.Data.NameInDataBase;
                    companyDto.ImageLogoByte = null;

                    var result = await companyService.UpdateCompany(companyDto);
                    if (result)
                        return new ResultDto<bool>(true, MessageCodes.Success);
                    else
                        return new ResultDto<bool>(false, MessageCodes.BadRequest);
                }
                return new ResultDto<bool>(false, MessageCodes.BadRequest);
            }
            else
            {
                var result = await companyService.UpdateCompany(companyDto);
                if (result)
                    return new ResultDto<bool>(true, MessageCodes.Success);
                else
                    return new ResultDto<bool>(false, MessageCodes.BadRequest);
            }
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<CompanyDto>> GetCompanyByEmail()
        {
            var result = await companyService.GetCompanyByEmail(GetCompanyEmail().Data);
            if (result.Logo is not null)
            {
                var byteFile = fileManagerService.ConvertFileTobyte(new List<string>()
                {
                configuration["FileManager:DirectoryFileCompany"],
                    result.Id.ToString() ,
                result.FileImageLogo
                });
            }
            return new ResultDto<CompanyDto>(result);
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<FileApplicantDocumentDto>> GetApplicantDocument(int fileId)
        {
            var result = await companyService.GetApplicantDocument(fileId);
            return result;
        } 
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<FileApplicantDocumentDto>> GetCompanyDocumentFile(int fileId)
        {
            var result = await companyService.GetCompanyDocumentFile(fileId);
            return result;
        }
        [HttpPost]
        public async Task<ResultDto<PaginationDto<OfferMobileModel>>> GetOffersByPaginationList([FromBody] AdvanceSearchFilterModelDto advanceSearchFilterModelDto)
        {
            var model = await companyService.GetoffersFiltersAsync(advanceSearchFilterModelDto.JobCategories, advanceSearchFilterModelDto.TypeOfEmployees, advanceSearchFilterModelDto.Languages, advanceSearchFilterModelDto.WorkAreas, advanceSearchFilterModelDto.Page, 5, advanceSearchFilterModelDto.MinSalary, advanceSearchFilterModelDto.MaxSalary, advanceSearchFilterModelDto.Keyword, advanceSearchFilterModelDto.Location, advanceSearchFilterModelDto.Currency, advanceSearchFilterModelDto.CountryName, advanceSearchFilterModelDto.UpdatedInTheLastDays);
            return new ResultDto<PaginationDto<OfferMobileModel>>(model);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<bool>> GetCompanyProfileCheckUp(string email)
        {
            var currentCompany = await companyService.GetCompanyByEmail(email);
            if (!string.IsNullOrWhiteSpace(currentCompany.Name))
                return new ResultDto<bool>(true);
            else
                return new ResultDto<bool>(false);

        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<bool>> UpdateApplieType(int appliedId, ApplicationStatus status)
        {
            return new ResultDto<bool>(await companyService.AppliedTypeUpdate(appliedId, status));
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<PaginationDto<OfferMobileModel>>> ChangeJobType(Guid id, OfferStatus status)
        {
            var result = await companyService.ChangeJobType(id, status);
            if (result)
            {
                if (status == OfferStatus.InActiveJobs)
                {

                    var email = GetCompanyEmail().Data;
                    var OfferMobileList = companyService.GetOffersByCompanyEmail(email, 1, OfferStatus.InActiveJobs);
                    return new ResultDto<PaginationDto<OfferMobileModel>>(OfferMobileList);
                }
                if (status == OfferStatus.ActiveJob)
                {
                    var email = GetCompanyEmail().Data;
                    var OfferMobileList = companyService.GetOffersByCompanyEmail(email, 1, OfferStatus.ActiveJob);
                    return new ResultDto<PaginationDto<OfferMobileModel>>(OfferMobileList);
                }

            }
            return new ResultDto<PaginationDto<OfferMobileModel>>();


        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<List<OfferDto>>> GetOfferByCompanyEmail()
        {
            var email = GetCompanyEmail().Data;
            var result = await companyService.GetOfferByCompanyEmail(email);
            if (result.Data.Count == 0)
            {
                result.MessageCode = MessageCodes.BadRequest;
            }
            return result;
        }
        [HttpPost]
        public async Task<ResultDto<int>> SaveRequestCompany(Guid id, string applicantEmail)
        {
            var result = await companyService.SaveRequestCompany(id, applicantEmail);
            if (result.Data > 0)
            {
                return result;
            }
            else
            {
                return new ResultDto<int>(0, MessageCodes.BadRequest);
            }
        }
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<OfferViewModel>> GetPreviewItems(OfferDto offerDto)
        {
            var result = await companyService.GetPreviewItems(offerDto);
            var companyDtoResult = await GetCompany();
            var companyDto = companyDtoResult.Data;

            result.offerDto.CompanyDto = companyDto;

            return new ResultDto<OfferViewModel>(result);
        }
        [HttpPost]
        public async Task<ResultDto<bool>> ReturnToConverstion(Guid offerId, int applicantId)
        {
            var result = await companyService.ReturnToConversation(offerId, applicantId);
            if (result)
            {
                return new ResultDto<bool>
                {
                    Data = true,
                    MessageCode = MessageCodes.Success
                };
            }
            else
            {
                return new ResultDto<bool>
                {
                    Data = false,
                    MessageCode = MessageCodes.BadRequest
                };
            }
        }
        [HttpPost]
        public async Task<bool> MarkAsRead(int id)
        {
            var result = await companyService.UpdateMessageIsSeenStatus(id);
            if (result)
            {
                return true;
            }
            return false;
        }
        [HttpPost]
        public async Task<bool> MarkAsUnRead(int id)
        {
            var result = await companyService.MarkAsUnRead(id);
            if (result)
            {
                return true;
            }
            return false;
        }
        [HttpPost]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var result = await companyService.DeleteMessage(id);

            if (result)
            {
                return Ok(new ResultDto<bool>
                {
                    MessageCode = (int)MessageCodes.Success,
                    Data = true
                });
            }
            else
            {
                return BadRequest(new ResultDto<bool>
                {
                    Data = false
                });
            }
        }
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ResultDto<PaginationDto<ApplicantDto>> CompanyFilter(ApplicantFilterDto filterDto)
        {
            var companyEmail = GetCompanyEmail().Data;
            var applicants = companyService.CompanyFilter(filterDto, companyEmail);
            return new ResultDto<PaginationDto<ApplicantDto>>(applicants);

        }
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ResultDto<PaginationDto<ApplicantDto>> ApplicantTrash(ApplicantFilterDto filterDto)
        {
            var companyEmail = GetCompanyEmail().Data;
            var applicants = companyService.ApplicantTrash(filterDto,companyEmail);
            return new ResultDto<PaginationDto<ApplicantDto>>(applicants);

        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<ApplicantOffersFavouriteResult>> SaveCompanyApplicnatFavourite(string applicantEmail)
        {
            if (applicantEmail == null) return new ResultDto<ApplicantOffersFavouriteResult>(ApplicantOffersFavouriteResult.Badrequest);
            var companyApplicantFavouriteDto = new CompanyApplicantFavouriteDto()
            {
                CompanyEmail = GetCompanyEmail().Data,
                ApplicantEmail = applicantEmail
            };
            var result = await companyService.SaveCompanyApplicnatFavourite(companyApplicantFavouriteDto);
            return new ResultDto<ApplicantOffersFavouriteResult>(result);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<bool>> DeleteFavouriteApplicant(string applicantEmail)
        {
            var companyEmail = GetCompanyEmail().Data;
            var result = await companyService.DeleteFavouriteApplicant(applicantEmail, companyEmail);
            return new ResultDto<bool>(result);
        }
        [HttpGet]
        public async Task<ResultDto<OfferDto>> GetCurrencyByOfferId(Guid id, string currencyRegion)
        {
            var result = await companyService.GetCurrencyByOfferId(id, currencyRegion);
            if (result != null)
                return new ResultDto<OfferDto>(result);
            return new ResultDto<OfferDto>(null, MessageCodes.BadRequest);
        }
        #region fakeSuccess
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<bool>> UpdateOfferPaymentStatus(Guid orderId, bool status = true)
        {
            var result = await companyService.ChangeThePaymentStatus(orderId, status);
            return new ResultDto<bool>(result);

        }
        #endregion

    }
}
