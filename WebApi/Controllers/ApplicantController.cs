using DotNek.Common.Dtos;
using FindJobs.Domain.Dtos;
using FindJobs.Domain.Dtos.Affinda;
using FindJobs.Domain.Enums;
using FindJobs.Domain.Services;
using FindJobs.Domain.ViewModels;
using FindJobs.WebApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using FindJobs.Domain.Dtos.FileManager;
using FindJobs.DataAccess.Entities;

namespace FindJobs.WebApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ApplicantController : Controller
    {
        private readonly IApplicantService applicantService;
        private readonly ICountryService countryService;
        private readonly ICompanyService companyService;
        private readonly IEncryption encryption;
        private readonly IConfiguration configurtion;
        private readonly IRazorPartialToStringService renderer;
        private readonly IFileManagerService fileManagerService;

        public ApplicantController(IApplicantService applicantService,
            IEncryption encryption,
            IConfiguration configurtion,
            ICompanyService companyService,
            IRazorPartialToStringService renderer,
            ICountryService countryService,
            IFileManagerService fileManagerService)
        {
            this.applicantService = applicantService;
            this.encryption = encryption;
            this.configurtion = configurtion;
            this.renderer = renderer;
            this.countryService = countryService;
            this.companyService = companyService;
            this.fileManagerService = fileManagerService;
        }

        [HttpGet]
        public async Task<IActionResult> ProfileImage(int id)
        {
            var byteFile = fileManagerService.ConvertFileTobyte(new List<string>()
                {
                    configurtion["FileManager:DirectoryFilesDefaultApplicant"]
                });

            if (id is 0)
                return new FileStreamResult(new MemoryStream(byteFile.Data.ByteFile), byteFile.Data.ContentType);

            var applicant = applicantService.GetApplicant(id);
            if (applicant is null)
                return new FileStreamResult(new MemoryStream(byteFile.Data.ByteFile), byteFile.Data.ContentType);

            var image = fileManagerService.ConvertFileTobyte(new List<string>()
                {
                    configurtion["FileManager:DirectoryFileApplicant"],
                    applicant.Id.ToString(),
                    applicant.ImageName,
                });

            if (image is null || image.MessageCode is not MessageCodes.Success)
                return new FileStreamResult(new MemoryStream(byteFile.Data.ByteFile), byteFile.Data.ContentType);

            return new FileStreamResult(new MemoryStream(image.Data.ByteFile), image.Data.ContentType);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<FileStreamResult>> ProfileDocument(int idDocument, string nameFile)
        {
            var emailApplicant = GetApplicantEmail().Data;
            var applicant = applicantService.GetApplicant(emailApplicant);

            if (applicant.Id is 0 || string.IsNullOrEmpty(nameFile))
                return new ResultDto<FileStreamResult>(
                    new FileStreamResult(new MemoryStream(), ""),
                    MessageCodes.BadRequest);

            var document = await applicantService.GetApplicantDocument(idDocument, nameFile);

            if (document is null)
                return new ResultDto<FileStreamResult>(null, MessageCodes.BadRequest);

            var fileByte = fileManagerService.ConvertFileTobyte(new List<string>()
            {
                configurtion["FileManager:DirectoryFileApplicant"],
                applicant.Id.ToString(),
                nameFile,
            });

            if (fileByte is null ||
                fileByte.Data is null ||
                fileByte.MessageCode is not MessageCodes.Success)
                return new ResultDto<FileStreamResult>(null, MessageCodes.BadRequest);

            return new ResultDto<FileStreamResult>(
                new FileStreamResult(new MemoryStream(fileByte.Data.ByteFile), fileByte.Data.ContentType),
                MessageCodes.Success);
        }

        #region Applicant
        [NonAction]
        private ResultDto<string> GetApplicantEmail()
        {
            var claims = User.Claims.ToList();
            var applicantEmail = claims.FirstOrDefault(x => x.Type.Equals(ClaimTypes.NameIdentifier))?.Value;
            return new ResultDto<string>(applicantEmail);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<ApplicantDto>> GetCurrentApplicant()
        {
            var model = applicantService.GetApplicant(GetApplicantEmail().Data);

            var allCountries = await Domain.Global.Global.GetCountryList(configurtion["GlobalSettings:ApiUrl"]);
            var allCities = await Domain.Global.Global.GetCityList(configurtion["GlobalSettings:ApiUrl"]);
            model.LimiteCVParser = await applicantService.CheckCVParser(GetApplicantEmail().Data);

            if (model.ImageFileName is not null)
            {

                var byteFile = fileManagerService.ConvertFileTobyte(new List<string>()
            {
                configurtion["FileManager:DirectoryFileApplicant"],
                model.Id.ToString() ,
                model.ImageName
            });

                if (byteFile.Data is not null)
                    model.ApplicantImageValue = Convert.ToBase64String(byteFile.Data.ByteFile);
            }


            if (model is not null)
            {
                if (model.CountryCode != null)
                    model.CountryName = allCountries.SingleOrDefault(x => x.Code == model.CountryCode).Name;

                if (model.CityId != null)
                    model.CityMainName = allCities.SingleOrDefault(x => x.Id == model.CityId).Name;
            }
            return new ResultDto<ApplicantDto>(model);
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<ApplicantPrivacyDto>> GetVisibilitiApplicant()
        {
            var applicantEmail = GetApplicantEmail().Data;
            var applicantSettings = await applicantService.GetApplicantSettingsAsync(applicantEmail);

            return new ResultDto<ApplicantPrivacyDto>(applicantSettings);
        }
        private ApplicantDto AffectApplicantPrivacy(ApplicantDto applicant)
        {
            applicant.Email = "";
            if (applicant.ShowGender) applicant.Gender = null;
            if (applicant.ShowPhone) applicant.Phone = null;
            if (applicant.ShowAddress) applicant.Address = null;
            if (applicant.ShowAge) applicant.DateOfBirth = null;
            if (applicant.ShowCountryOrCity) applicant.City = null;
            if (applicant.ShowCountryOrCity) applicant.Country = null;
            return applicant;
        }
        [HttpGet]
        public ResultDto<ApplicantDto> GetApplicantById(int id)
        {
            //IMPORTANT NOTE: (DO NOT DELETE THIS COMMENT)
            //WE USE THIS METHOD TO RETURN APPLICANT DATA FOR PUBLIC (NOT FOR HIMSELF/HERSELF)
            //IT MEANS WE SHOULD NOT HAVE PRIVATE FIELDS IN THIS METHOD
            var applicant = applicantService.GetApplicant(id);
            if (applicant != null)
            {
                AffectApplicantPrivacy(applicant);
                return new ResultDto<ApplicantDto>(applicant);
            }
            return new ResultDto<ApplicantDto>(null, MessageCodes.BadRequest);


        }
        [HttpGet]
        public ResultDto<ApplicantDto> GetApplicantsById(int id)
        {
            var applicant = applicantService.GetApplicant(id);
            if (applicant != null)
            {
                return new ResultDto<ApplicantDto>(applicant);
            }
            return new ResultDto<ApplicantDto>(null, MessageCodes.BadRequest);


        }
        [HttpGet]
        public ResultDto<PaginationDto<ApplicantDto>> GetApplicants(string? key, string? location, int currentPage = 1)
        {
            //IMPORTANT NOTE: (DO NOT DELETE THIS COMMENT)
            //WE USE THIS METHOD TO RETURN APPLICANT DATA FOR PUBLIC (NOT FOR HIMSELF/HERSELF)
            //IT MEANS WE SHOULD NOT HAVE PRIVATE FIELDS IN THIS METHOD
            var applicants = new ResultDto<PaginationDto<ApplicantDto>>(applicantService.GetApplicants(currentPage, key, location));
            if (applicants.Data.ItemsCount > 0)
            {
                foreach (var applicant in applicants.Data.Data)
                {
                    AffectApplicantPrivacy(applicant);
                }
            }
            return applicants;
        }
        [HttpPost]
        public ResultDto<PaginationDto<ApplicantDto>> ApplicantsFilter(ApplicantFilterDto filterDto)
        {

            var applicants = applicantService.ApplicantsFilter(filterDto);
            return new ResultDto<PaginationDto<ApplicantDto>>(applicants);

        }
        #endregion
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<bool>> UpdatePrivacy(ApplicantPrivacyDto privacyDto)
        {

            var applicantEmail = GetApplicantEmail().Data;
            privacyDto.Email = applicantEmail; 
            var result = await applicantService.UpdatePrivacy(privacyDto);

            return new ResultDto<bool>(result);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<string>> GetApplicantImage()
        {
            var applicant = await GetCurrentApplicant();

            var byteFile = fileManagerService.ConvertFileTobyte(new List<string>()
            {
                configurtion["FileManager:DirectoryFileApplicant"],
                applicant.Data.Id.ToString() ,
                applicant.Data.ImageFileName
            });

            if (applicant.Data != null)
            {
                return new ResultDto<string>(Convert.ToBase64String(byteFile.Data.ByteFile));
            }
            return new ResultDto<string>(string.Empty, MessageCodes.BadRequest);
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<bool>> IsApplicantExist()
        {
            bool available = await applicantService.IsApplicantExist(GetApplicantEmail().Data);
            if (available)
            {
                return new ResultDto<bool>(true);
            }
            return new ResultDto<bool>(false);
        }
        [HttpGet]
        public ResultDto<bool> UnsubscribeEmailPreferences(string EncryptedEmail)
        {
            var applicantEmail = encryption.Decrypt(HttpUtility.UrlDecode(EncryptedEmail), configurtion["GlobalSettings:EncryptionSalt"]);
            if (applicantService.UnsubscribeEmailPreferences(applicantEmail))
            {
                return new ResultDto<bool>(true, MessageCodes.Success);
            }
            return new ResultDto<bool>(false, MessageCodes.BadRequest);
        }
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<string>> UpdateApplicantImage(ImageFileDto imageFileDto)
        {
            var applicantEmail = GetApplicantEmail().Data;

            var applicant = applicantService.GetApplicant(applicantEmail);
            if (fileManagerService.IsImage(imageFileDto.ExtensionFile).Data)
            {
                var iformFile = fileManagerService.ConvertArrayToIFormFile(new Domain.Dtos.FileManager.ArrayFile()
                {
                    ByteArray = imageFileDto.ImageByte,
                    FileName = imageFileDto.NameFile,
                    Name = imageFileDto.NameFile
                });
                if (iformFile.MessageCode is not MessageCodes.Success)
                    return new ResultDto<string>("", MessageCodes.BadRequest);

                var webpFile = await fileManagerService.ConvertIFormFileToWebp(iformFile.Data);

                if (webpFile.MessageCode is not MessageCodes.Success)
                    return new ResultDto<string>("", MessageCodes.BadRequest);

                var changePlace = await fileManagerService.ChangePlaceWebpFile(new PlaceFileWebp()
                {
                    DestinationCombinePath = new List<string>() {
                                    configurtion["FileManager:DirectoryFileApplicant"],
                                    applicant.Id.ToString() },
                    StreamFile = webpFile.Data.Stream,
                    FileName = webpFile.Data.Name,
                    WhichTypeCreateName = NameType.NameWithDateTime
                });

                if (changePlace.MessageCode is not MessageCodes.Success)
                    return new ResultDto<string>("", MessageCodes.BadRequest);


                var result = await applicantService.UpdateApplicantImage(changePlace.Data.NameFile, changePlace.Data.NameInDataBase, applicantEmail);
                if (result)
                    return new ResultDto<string>(Convert.ToBase64String(imageFileDto.ImageByte), MessageCodes.Success);
                else
                    return new ResultDto<string>("", MessageCodes.BadRequest);
            }

            return new ResultDto<string>("", MessageCodes.BadRequest);

        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<string>> DeleteApplicantImage()
        {

            var applicantEmail = GetApplicantEmail().Data;
            if (string.IsNullOrEmpty(applicantEmail))
                return new ResultDto<string>(Res.ApplicantProfile.GetEmail, MessageCodes.BadRequest);

            var result = await applicantService.DeleteApplicantImage(applicantEmail);

            if (result)
                return new ResultDto<string>("", MessageCodes.Success);

            return new ResultDto<string>(Res.ApplicantProfile.deleteImage, MessageCodes.UnHandleException);
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ResultDto<CalculateProgressbarDto> CalculationProgressbar()
        {
            var progressbar = new CalculateProgressbarDto();
            var applicantdto = applicantService.GetApplicant(GetApplicantEmail().Data);
            int profileCompleted = 15;

            if (applicantdto.FirstName != null) profileCompleted += 5;
            if (applicantdto.LastName != null) profileCompleted += 5;
            if (applicantdto.DateOfBirth != null) profileCompleted += 5;
            if (applicantdto.AvailableDate != null) profileCompleted += 5;

            if (applicantdto.Phone != null) profileCompleted += 5;
            if (applicantdto.CountryCode != null) profileCompleted += 5;
            if (applicantdto.Address != null) profileCompleted += 5;
            if (applicantdto.PostalCode != null) profileCompleted += 5;

            if (applicantdto.ApplicantDocuments.Count > 0) profileCompleted += 5;
            if (applicantdto.ApplicantWorkExperiences.Count > 0) profileCompleted += 35;
            if (applicantdto.ApplicantEducations.Count > 0) profileCompleted += 5;
            decimal profileProgressBarOffset = 450 - 450 * (Convert.ToDecimal(profileCompleted) / 100);
            progressbar.ProfileCompleted = profileCompleted;
            progressbar.ProfileProgressBarOffset = profileProgressBarOffset;
            return new ResultDto<CalculateProgressbarDto>(progressbar);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ResultDto GetRoleClaims()
        {
            //TODO: we have a security issue. check this scenario:
            //Sign up as both applicant and company with the same email
            //login with a company and copy the token, then in swagger call UpdatePrivacy for applicant
            if (applicantService.GetApplicantUserClaims(User))
                return new ResultDto(MessageCodes.Success);
            else
                return new ResultDto(MessageCodes.Unauthorized);
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<bool>> SaveProfile(ApplicantDto profile)
        {
            profile.Email = GetApplicantEmail().Data;

            if (!await applicantService.SavePersonalInformation(new ApplicantProfileDto()
            {
                ApplicantImage = profile.ApplicantImage,
                AvailableDate = profile.AvailableDate,
                DateOfBirth = profile.DateOfBirth,
                Email = profile.Email,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Gender = (Gender)profile.Gender,
                JobPosition = profile.JobPosition,
                ReadyToWorkStatus = profile.ReadyToWorkStatus
            })) return new ResultDto<bool>(false, MessageCodes.BadRequest);

            if (!await applicantService.SaveApplicantContactDetail(new ApplicantContactDetailsDto()
            {
                Address = profile.Address,
                CityId = profile.CityId,
                CityMainName = profile.CityMainName,
                CityName = profile.CityName,
                CountryCode = profile.CountryCode,
                CountryName = profile.CountryName,
                Phone = profile.Phone,
                PostalCode = profile.PostalCode,
                StateName = profile.StateName,
            }, GetApplicantEmail().Data)) return new ResultDto<bool>(false, MessageCodes.BadRequest);

            if (!await applicantService.SaveApplicantAdditionalSection(new ApplicantAddtionalSectionDto()
            {
                Currency = profile.Currency,
                Email = profile.Email,
                HasDrivingLicense = profile.HasDrivingLicense,
                HasDrivingLicenseA = profile.HasDrivingLicenseA,
                HasDrivingLicenseB = profile.HasDrivingLicenseB,
                HasDrivingLicenseC = profile.HasDrivingLicenseC,
                HasDrivingLicenseD = profile.HasDrivingLicenseD,
                HourlyAverage = profile.HourlyAverage,
                HourlyFrom = profile.HourlyFrom,
                HourlyUntil = profile.HourlyUntil,
                Id = profile.Id,
                IsEuropeanUnion = profile.IsEuropeanUnion,
                IsFreelancer = profile.IsFreelancer,
                IsFullTime = profile.IsFullTime,
                IsHourlyRate = profile.IsHourlyRate,
                IsInternShip = profile.IsInternShip,
                IsOffSite = profile.IsOffSite,
                IsOnSite = profile.IsOnSite,
                IsPartialRemote = profile.IsPartialRemote,
                IsPartTime = profile.IsPartTime,
                IsSwitzerland = profile.IsSwitzerland,
                IsUnitedStatesOfAmerica = profile.IsUnitedStatesofAmerica,
                RateType = profile.RateType,
            })) return new ResultDto<bool>(false, MessageCodes.BadRequest);

            if (!await applicantService.UpdateVerifyStatusApplicant(new VerifyStatusApplicantProfileDto()
            {
                EmailApplicant = profile.Email,
                StatusVerifyApplicant = profile.VerifiedByUser
            })) return new ResultDto<bool>(false, MessageCodes.BadRequest);

            return new ResultDto<bool>(true, MessageCodes.Success);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<bool>> SetActiveDocument(int selectedDocumentId)
        {
            var applicantEmail = GetApplicantEmail().Data;
            var success = await applicantService.SetActiveDocument(selectedDocumentId, applicantEmail);

            if (success)
            {
                return new ResultDto<bool>(true, MessageCodes.Success);
            }
            else
            {
                return new ResultDto<bool>(false, MessageCodes.BadRequest);
            }


        }
        [HttpPost]
        public async Task<ResultDto<bool>> MarkAsRead(int id)
        {
            var result = await applicantService.UpdateMessageIsSeenStatus(id);
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
        public async Task<ResultDto<bool>> ReturnToConverstion(int appliedJobId)
        {
            var result = await applicantService.ReturnToConverstion(appliedJobId);
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
        public async Task<ResultDto<bool>> MarkAsUnRead(int id)
        {
            var result = await applicantService.MarkAsUnRead(id);
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
        public async Task<ResultDto<MessageCodes>> DeleteMessage(int id)
        {
            var result = await applicantService.DeleteMessage(id); 


            if (result.MessageCode == MessageCodes.BadRequest)
            {
                return new ResultDto<MessageCodes>(MessageCodes.BadRequest); 
            }


            if (result.MessageCode == MessageCodes.UnHandleException)
            {
                return new ResultDto<MessageCodes>(MessageCodes.UnHandleException); 
            }

            return new ResultDto<MessageCodes>(MessageCodes.Success);
        }
        #region ApplicantDocument
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<List<ApplicantDocumentDto>>> GetApplicantDocument(UploadDocumentType type)
        {
            var email = GetApplicantEmail().Data;
            var documentApplicant = applicantService.GetApplicantDocumentsByEmailAndType(email, type);

            if (documentApplicant is null)
                return new ResultDto<List<ApplicantDocumentDto>>(null, MessageCodes.BadRequest);


            return new ResultDto<List<ApplicantDocumentDto>>(applicantService.GetApplicantDocumentsByEmailAndType(email, type), MessageCodes.Success);
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<List<ApplicantDocumentDto>>> InsertApplicantDocument(ApplicantDocumentDto applicantDocumentsDto)
        {
            var email = GetApplicantEmail().Data;
            applicantDocumentsDto.ApplicantEmail = email;



            var applicant = applicantService.GetApplicant(email);

            if (fileManagerService.IsImage(applicantDocumentsDto.ExtensionFile).Data)
            {
                var iformFile = fileManagerService.ConvertArrayToIFormFile(new ArrayFile()
                {
                    ByteArray = applicantDocumentsDto.DocumentFile,
                    FileName = applicantDocumentsDto.NameFile,
                    Name = applicantDocumentsDto.Name
                });

                if (iformFile.MessageCode is not MessageCodes.Success)
                    return new ResultDto<List<ApplicantDocumentDto>>(null, MessageCodes.BadRequest);

                var webpFile = await fileManagerService.ConvertIFormFileToWebp(iformFile.Data);

                if (webpFile.MessageCode is not MessageCodes.Success)
                    return new ResultDto<List<ApplicantDocumentDto>>(null, MessageCodes.BadRequest);

                var changePlace = await fileManagerService.ChangePlaceWebpFile(new PlaceFileWebp()
                {
                    DestinationCombinePath = new List<string>() {
                                    configurtion["FileManager:DirectoryFileApplicant"],
                                    applicant.Id.ToString() },
                    StreamFile = webpFile.Data.Stream,
                    FileName = webpFile.Data.Name,
                    WhichTypeCreateName = NameType.NameWithDateTime
                });

                if (changePlace.MessageCode is not MessageCodes.Success)
                    return new ResultDto<List<ApplicantDocumentDto>>(null, MessageCodes.BadRequest);

                applicantDocumentsDto.NameFile = changePlace.Data.NameFile;
                applicantDocumentsDto.Name = changePlace.Data.NameInDataBase;

            }
            else
            {
                var iformFile = fileManagerService.ConvertArrayToIFormFile(new ArrayFile()
                {
                    ByteArray = applicantDocumentsDto.DocumentFile,
                    FileName = applicantDocumentsDto.NameFile,
                    Name = applicantDocumentsDto.Name
                });
                var changePlace = await fileManagerService.ChangePlaceIFormFile(new PlaceFileIFomFile()
                {
                    DestinationCombinePath = new List<string>() {
                                    configurtion["FileManager:DirectoryFileApplicant"],
                                    applicant.Id.ToString() },
                    File = iformFile.Data,
                    WhichTypeCreateName = NameType.NameWithDateTime
                });

                applicantDocumentsDto.NameFile = changePlace.Data.NameFile;
                applicantDocumentsDto.Name = changePlace.Data.NameInDataBase;
            }

            if (!await applicantService.InsertApplicantDocument(applicantDocumentsDto))
                return new ResultDto<List<ApplicantDocumentDto>>(null, MessageCodes.BadRequest);

            return new ResultDto<List<ApplicantDocumentDto>>(GetApplicantDocumentsByEmail().Data, MessageCodes.Success);
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ResultDto<List<ApplicantDocumentDto>> GetApplicantDocumentsByEmail()
        {
            var email = GetApplicantEmail().Data;
            List<ApplicantDocumentDto> model = applicantService.GetApplicantDocumentsByEmail(email);

            return new ResultDto<List<ApplicantDocumentDto>>(model);
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<FileApplicantDocumentDto>> GetFileApplicantDocument(int fileId)
        {
            var applicantEmail = GetApplicantEmail().Data;
            var applicant = applicantService.GetApplicant(applicantEmail);

            var applicantDocument = await applicantService.GetApplicantDocument(fileId, applicantEmail);

            if (applicantDocument is null)
                return new ResultDto<FileApplicantDocumentDto>(null, MessageCodes.BadRequest);


            var byteFile = fileManagerService.ConvertFileTobyte(new List<string>()
            {
                configurtion["FileManager:DirectoryFileApplicant"],
                applicant.Id.ToString() ,
                applicantDocument.Name
            });

            if (byteFile.MessageCode is not MessageCodes.Success)
                return new ResultDto<FileApplicantDocumentDto>(null, MessageCodes.BadRequest);

            return new ResultDto<FileApplicantDocumentDto>(byteFile.Data, MessageCodes.Success);
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<FileApplicantDocumentDto>> GetApplicantDocumentFile(int fileId)
        {

            var applicantDocument = await applicantService.GetApplicantDocumentFile(fileId);

            return applicantDocument;
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<List<ApplicantDocumentDto>>> RemoveApplicantDocument(int id)
        {
            if (!await applicantService.RemoveApplicantDocument(id))
                return new ResultDto<List<ApplicantDocumentDto>>(null, MessageCodes.BadRequest);
            return new ResultDto<List<ApplicantDocumentDto>>(GetApplicantDocumentsByEmail().Data, MessageCodes.Success);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<List<ApplicantDocumentDto>>> RemovedApplicantDocuments()
        {
            var email = GetApplicantEmail().Data;
            var listDocumentApplicant = await applicantService.GetRemovedApplicantDocumentsByEmail(email);
            return new ResultDto<List<ApplicantDocumentDto>>(listDocumentApplicant);
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<List<ApplicantDocumentDto>>> DeleteApplicantDocument(int id)
        {
            var email = GetApplicantEmail().Data;
            var applicant = applicantService.GetApplicant(email);
            var documentApplicant = await applicantService.GetApplicantDocument(id);
            if (documentApplicant is null)
                return new ResultDto<List<ApplicantDocumentDto>>(await applicantService.GetRemovedApplicantDocumentsByEmail(email), MessageCodes.BadRequest);

            var deleteApplicantDocument = await applicantService.DeleteApplicantDocument(id);

            if (deleteApplicantDocument is false)
                return new ResultDto<List<ApplicantDocumentDto>>(await applicantService.GetRemovedApplicantDocumentsByEmail(email), MessageCodes.BadRequest);

            var deleteFile = fileManagerService.RemoveFile(
                new RemoveFile()
                {
                    Directories = new List<string> {
                            configurtion["FileManager:DirectoryFileApplicant"],
                            applicant.Id.ToString() },
                    NameWithExtension = documentApplicant.Name
                }
                );

            if (deleteFile is null || deleteFile.MessageCode is not MessageCodes.Success ||
                deleteFile.Data is false)
                return new ResultDto<List<ApplicantDocumentDto>>(await applicantService.GetRemovedApplicantDocumentsByEmail(email), MessageCodes.BadRequest);
            return new ResultDto<List<ApplicantDocumentDto>>(await applicantService.GetRemovedApplicantDocumentsByEmail(email), MessageCodes.Success);
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<List<ApplicantDocumentDto>>> RestoreApplicantDocument(int id)
        {
            var email = GetApplicantEmail().Data;
            var applicant = applicantService.GetApplicant(email);
            var documentApplicant = await applicantService.GetApplicantDocument(id);
            if (documentApplicant is null)
                return new ResultDto<List<ApplicantDocumentDto>>(await applicantService.GetRemovedApplicantDocumentsByEmail(email), MessageCodes.BadRequest);

            var restoreApplicantDocument = await applicantService.RestoreApplicantDocument(id);

            if (restoreApplicantDocument is false)
                return new ResultDto<List<ApplicantDocumentDto>>(await applicantService.GetRemovedApplicantDocumentsByEmail(email), MessageCodes.BadRequest);

            return new ResultDto<List<ApplicantDocumentDto>>(await applicantService.GetRemovedApplicantDocumentsByEmail(email), MessageCodes.Success);
        }

        #endregion
        #region WorkExperience

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<List<ApplicantWorkExperienceDto>>> InsertOrUpdateWorkExperience(ApplicantWorkExperienceDto workExperienceDto)
        {
            workExperienceDto.ApplicantEmail = GetApplicantEmail().Data;
            if (!await applicantService.InsertOrUpdateWorkExperiance(workExperienceDto))
                return new ResultDto<List<ApplicantWorkExperienceDto>>(null, MessageCodes.BadRequest);
            return GetApplicantWorkExperienceByEmail();
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<ApplicantWorkExperienceDto>> GetWorkExperienceById(int id)
        {
            var workExperienceDto = await applicantService.GetWorkExperienceById(id);
            if (workExperienceDto == null)
                return new ResultDto<ApplicantWorkExperienceDto>(null, MessageCodes.BadRequest);

            return new ResultDto<ApplicantWorkExperienceDto>(workExperienceDto);
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<List<ApplicantWorkExperienceDto>>> DeleteWorkExperience(int id)
        {
            if (!await applicantService.DeleteApplicantWorkExperience(id))
                return new ResultDto<List<ApplicantWorkExperienceDto>>(null, MessageCodes.BadRequest);
            return GetApplicantWorkExperienceByEmail();
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ResultDto<List<ApplicantWorkExperienceDto>> GetApplicantWorkExperienceByEmail()
        {
            var email = GetApplicantEmail().Data;
            List<ApplicantWorkExperienceDto> model = applicantService.GetAllApplicantWorkExperienceByEmail(email);
            return new ResultDto<List<ApplicantWorkExperienceDto>>(model);
        }

        #endregion
        #region ApplicantEducation
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<List<ApplicantEducationDto>>> CreateOrUpdateApplicantEducation(ApplicantEducationDto applicantEducationDto)
        {
            applicantEducationDto.ApplicantEmail = GetApplicantEmail().Data;
            if (!await applicantService.CreateOrUpdateEducation(applicantEducationDto))
                return new ResultDto<List<ApplicantEducationDto>>(null, MessageCodes.BadRequest);
            return GetApplicanEducationListByEmail();
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<ApplicantEducationDto>> GetApplicantEducation(int id)
        {
            var ApplicantEducationDto = await applicantService.GetApplicantEducationById(id);
            if (ApplicantEducationDto == null)
                return new ResultDto<ApplicantEducationDto>(null, MessageCodes.BadRequest);

            return new ResultDto<ApplicantEducationDto>(ApplicantEducationDto, MessageCodes.Success);
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<List<ApplicantEducationDto>>> DeleteApplicantEducation(int id)
        {
            if (!await applicantService.DeleteApplicantEducation(id))
                return new ResultDto<List<ApplicantEducationDto>>(null, MessageCodes.BadRequest);
            return GetApplicanEducationListByEmail();
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ResultDto<List<ApplicantEducationDto>> GetApplicanEducationListByEmail()
        {
            var email = GetApplicantEmail().Data;
            List<ApplicantEducationDto> model = applicantService.GetAllApplicantEducation(email);
            return new ResultDto<List<ApplicantEducationDto>>(model);
        }
        #endregion
        #region Applicant Knowledge
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<List<ApplicantKnowledgeDto>>> CreateOrUpdateApplicantKnowledge(ApplicantKnowledgeDto knowledgeDto)
        {
            knowledgeDto.ApplicantEmail = GetApplicantEmail().Data;
            if (!await applicantService.CreateOrUpdateKhnowledge(knowledgeDto))
                return new ResultDto<List<ApplicantKnowledgeDto>>(null, MessageCodes.BadRequest);
            return GetApplicanKnowledgeListByEmail();
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<ApplicantKnowledgeDto>> GetApplicantKnowledge(int id)
        {
            var ApplicantKnowledgeDto = await applicantService.GetApplicantKnowledgeById(id);
            if (ApplicantKnowledgeDto == null)
                return new ResultDto<ApplicantKnowledgeDto>(null, MessageCodes.BadRequest);

            return new ResultDto<ApplicantKnowledgeDto>(ApplicantKnowledgeDto, MessageCodes.Success);
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<List<ApplicantKnowledgeDto>>> DeleteApplicantKnowledge(int id)
        {
            if (!await applicantService.DeleteApplicantKnowledge(id))
                return new ResultDto<List<ApplicantKnowledgeDto>>(null, MessageCodes.BadRequest);
            return GetApplicanKnowledgeListByEmail();
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ResultDto<List<KnowledgeDto>> GetKnowledgeList()
        {
            var knowledgeList = applicantService.GetKnowledgeList();
            return new ResultDto<List<KnowledgeDto>>(knowledgeList);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ResultDto<List<ApplicantKnowledgeDto>> GetApplicanKnowledgeListByEmail()
        {
            var email = GetApplicantEmail().Data;
            List<ApplicantKnowledgeDto> model = applicantService.GetAllApplicantKnowledge(email);
            return new ResultDto<List<ApplicantKnowledgeDto>>(model, MessageCodes.Success);
        }
        #endregion
        #region Applicant Language
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<List<ApplicantLanguageDto>>> CreateOrUpdateApplicantLanguage(ApplicantLanguageDto languageDto)
        {
            languageDto.ApplicantEmail = GetApplicantEmail().Data;
            if (!await applicantService.CreateOrUpdateLanguage(languageDto))
                return new ResultDto<List<ApplicantLanguageDto>>(null, MessageCodes.BadRequest);
            return GetApplicanLanguageListByEmail();
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<ApplicantLanguageDto>> GetApplicantLanguage(int id)
        {
            var applicantLanguageDto = await applicantService.GetApplicantLanguageById(id);
            if (applicantLanguageDto == null)
                return new ResultDto<ApplicantLanguageDto>(null, MessageCodes.BadRequest);

            return new ResultDto<ApplicantLanguageDto>(applicantLanguageDto, MessageCodes.Success);
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<List<ApplicantLanguageDto>>> DeleteApplicantLanguage(int id)
        {
            if (!await applicantService.DeleteApplicantLanguage(id))
                return new ResultDto<List<ApplicantLanguageDto>>(null, MessageCodes.BadRequest);
            return GetApplicanLanguageListByEmail();
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ResultDto<List<ApplicantLanguageDto>> GetApplicanLanguageListByEmail()
        {
            var email = GetApplicantEmail().Data;
            List<ApplicantLanguageDto> model = applicantService.GetAllApplicantLanguage(email);
            return new ResultDto<List<ApplicantLanguageDto>>(model, MessageCodes.Success);
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ResultDto<List<LanguageDto>> GetLanguageList()
        {
            var LanguageList = applicantService.GetLanguageList();
            return new ResultDto<List<LanguageDto>>(LanguageList, MessageCodes.Success);
        }
        #endregion
        #region PersonalInformation
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ResultDto<bool> CreateOrUpdateApplicantPersonalInformation(ApplicantProfileDto applicantProfileDto)
        {
            applicantProfileDto.Email = GetApplicantEmail().Data;
            if (!applicantService.SavePersonalInformation(applicantProfileDto).Result)
                return new ResultDto<bool>(false, MessageCodes.BadRequest);
            return new ResultDto<bool>(true, MessageCodes.Success);
        }

        #endregion
        #region Applicant Contact Detail
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<bool>> CreateOrUpdateApplicantContactDetail(ApplicantContactDetailsDto applicantContactDetailsDto)
        {
            if (!applicantService.SaveApplicantContactDetail(applicantContactDetailsDto, GetApplicantEmail().Data).Result)
                return new ResultDto<bool>(false, MessageCodes.BadRequest);
            return new ResultDto<bool>(true, MessageCodes.Success);
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<ApplicantDto>> GetApplicantContactDetail()
        {
            var applicantContactDetailsDto = applicantService.GetApplicantContactDetail(GetApplicantEmail().Data);
            if (applicantContactDetailsDto == null) return new ResultDto<ApplicantDto>(null, MessageCodes.BadRequest);
            return new ResultDto<ApplicantDto>(applicantContactDetailsDto, MessageCodes.Success);
        }
        #endregion
        #region Applicant Additional Section
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ResultDto<bool> CreateOrUpdateApplicantAdditionalSection(ApplicantAddtionalSectionDto addtionalSectionDto)
        {
            addtionalSectionDto.Email = GetApplicantEmail().Data;
            if (!applicantService.SaveApplicantAdditionalSection(addtionalSectionDto).Result)
                return new ResultDto<bool>(false, MessageCodes.BadRequest);

            return new ResultDto<bool>(true, MessageCodes.Success);
        }

        #endregion

        #region Offer
        [HttpGet]
        public ResultDto<PaginationDto<OfferDto>> SearchAllOffers()
        {
            var model = applicantService.SearchOffersAjax();

            return new ResultDto<PaginationDto<OfferDto>>(model, MessageCodes.Success);
        }
        #endregion

        #region Offers
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<ApplicantOffersFavouriteResult>> SaveApplicantOfferFavourite(Guid id)
        {
            if (id == Guid.Empty) return new ResultDto<ApplicantOffersFavouriteResult>(ApplicantOffersFavouriteResult.Success);
            var applicantOffersFavouriteDto = new ApplicantOfferFavouriteDto()
            {
                ApplicantEmail = GetApplicantEmail().Data,
                OfferId = id
            };
            var result = await applicantService.SaveApplicantOfferFavourite(applicantOffersFavouriteDto);
            if (result == ApplicantOffersFavouriteResult.Success)
            {
                return new ResultDto<ApplicantOffersFavouriteResult>(ApplicantOffersFavouriteResult.Success);
            }
            if (result == ApplicantOffersFavouriteResult.Badrequest)
            {
                return new ResultDto<ApplicantOffersFavouriteResult>(ApplicantOffersFavouriteResult.Badrequest);
            }

            return new ResultDto<ApplicantOffersFavouriteResult>(ApplicantOffersFavouriteResult.AddedBefore);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ResultDto<List<OfferDto>> GetAllFavourteApplicantOffers()
        {
            var model = applicantService.GetAllFavourteApplicantOffers(GetApplicantEmail().Data);
            return new ResultDto<List<OfferDto>>(model);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<PaginationDto<OfferMobileModel>>> GetAllFavourteApplicantOffersWithPageing(int currentPage, string company)
        {
            var applicantEmail = GetApplicantEmail().Data;
            var model = await applicantService.GetAllFavourteApplicantOffersWithPageing(applicantEmail, currentPage, company);
            return new ResultDto<PaginationDto<OfferMobileModel>>(model);
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<PaginationDto<OfferMobileModel>>> GetAllDeleteMessage(int currentPage, string company)
        {
            var applicantEmail = GetApplicantEmail().Data;
            var model = await applicantService.GetAllDeleteMessage(applicantEmail, currentPage, company);
            return new ResultDto<PaginationDto<OfferMobileModel>>(model);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<bool>> DeleteFavouriteOffer(Guid id)
        {
            var result = await applicantService.DeleteFavouriteOffer(id, GetApplicantEmail().Data);
            return new ResultDto<bool>(result);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ResultDto<int> GetCountFavouritApplicant()
        {
            var count = applicantService.GetCountFavouritApplicant(GetApplicantEmail().Data);
            return new ResultDto<int>(count);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ResultDto<List<ApplicantOfferFavouriteDto>> GetApplicantFavouriteOfferList()
        {
            var model = applicantService.applicantOffersFavouriteDtos(GetApplicantEmail().Data);
            return new ResultDto<List<ApplicantOfferFavouriteDto>>(model);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<ApplicantOfferFavouriteDto>> GetApplicantFavouriteOffer(Guid id)
        {
            var applicantemail = GetApplicantEmail().Data;
            var model = await applicantService.GetApplicantOfferFavouriteDtos(applicantemail, id);
            return new ResultDto<ApplicantOfferFavouriteDto>(model);
        }
        #endregion

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<string>> GetPartialAsString(string page)
        {
            var currentApplicant = await GetCurrentApplicant();
            var result = await renderer.RenderPartialToStringAsync(page, currentApplicant.Data);
            return new ResultDto<string>(result);
        }


        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<bool>> SendCv(AppliedJobDto dtos)
        {
            dtos.ApplicantEmail = GetApplicantEmail().Data;
            if (dtos is null) return new ResultDto<bool>(false, MessageCodes.BadRequest);
            var applicant = applicantService.GetApplicant(dtos.ApplicantEmail);
            if (applicant == null)
            {
                return new ResultDto<bool>(false, MessageCodes.BadRequest);
            }

            if (applicant.FirstName is null || applicant.LastName is null)
            {
                return new ResultDto<bool>(false, MessageCodes.UnHandleException);
            }
            foreach (var file in dtos.Files)
            {
                var iformFile = fileManagerService.ConvertArrayToIFormFile(new ArrayFile()
                {
                    FileName = file.FileName,
                    Name = file.Name,

                });

                var changePlace = await fileManagerService.ChangePlaceIFormFile(new PlaceFileIFomFile()
                {
                    DestinationCombinePath = new List<string>() {
                                configurtion["FileManager:DirectoryFileApplicant"],
                                 applicant.Id.ToString()
                                },
                    File = iformFile.Data,
                    WhichTypeCreateName = NameType.NameWithDateTime
                });

                dtos.NameFile = changePlace.Data.NameFile;
                dtos.Name = changePlace.Data.NameInDataBase;
            }

            var result = await applicantService.Apply(dtos);
            if (result)
                return new ResultDto<bool>(true, MessageCodes.Success);
            else
                return new ResultDto<bool>(false, MessageCodes.BadRequest);
        }




        #region ApplicantSystemDocument
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<List<ApplicantDocumentDto>>> InsertOrUpdateApplicantSystemDocument(ApplicantDocumentDto applicantdocumentDto)
        {
            var applicantEmail = GetApplicantEmail().Data;
            applicantdocumentDto.ApplicantEmail = applicantEmail;
            var checkForExists = applicantService.GetApplicantDocumentsByEmail(applicantEmail);
            if (checkForExists != null)
            {
                if (checkForExists.Any(x => x.Type == UploadDocumentType.Site_Generated))
                {

                    await applicantService.RemoveApplicantDocument(checkForExists.Find(x => x.Type == UploadDocumentType.Site_Generated).Id);
                    if (!await applicantService.InsertApplicantDocument(applicantdocumentDto))
                    {
                        return new ResultDto<List<ApplicantDocumentDto>>(null, MessageCodes.BadRequest);
                    }
                    return GetApplicantDocumentsByEmail();

                }
                else
                {
                    if (!await applicantService.InsertApplicantDocument(applicantdocumentDto))
                    {
                        return new ResultDto<List<ApplicantDocumentDto>>(null, MessageCodes.BadRequest);
                    }
                    return GetApplicantDocumentsByEmail();
                }
            }
            else
            {
                if (!await applicantService.InsertApplicantDocument(applicantdocumentDto))
                {
                    return new ResultDto<List<ApplicantDocumentDto>>(null, MessageCodes.BadRequest);
                }
                return GetApplicantDocumentsByEmail();
            }
        }
        #endregion

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<bool>> SaveMessage(AppliedJobMessageDto dtos)
        {
            var applicant = applicantService.GetApplicant(dtos.ApplicantEmail);

            var directoryPath = Path.Combine(configurtion["FileManager:DirectoryAppliedJobMessageAttach"], applicant.Id.ToString());

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            if (dtos.AppliedJobMessageAttachDtos != null && dtos.AppliedJobMessageAttachDtos.Count > 0)
            {
                foreach (var item in dtos.AppliedJobMessageAttachDtos)
                {
                    var iformFile = fileManagerService.ConvertArrayToIFormFile(new ArrayFile()
                    {
                        ByteArray = item.Data,
                        FileName = item.FileName,
                        Name = item.Name
                    });

                    if (iformFile.MessageCode != MessageCodes.Success)
                    {
                        return new ResultDto<bool>(false, MessageCodes.BadRequest);
                    }

                    if (fileManagerService.IsImage(Path.GetExtension(item.FileName)).Data)
                    {
                        var webpFile = await fileManagerService.ConvertIFormFileToWebp(iformFile.Data);

                        if (webpFile.MessageCode != MessageCodes.Success)
                        {
                            return new ResultDto<bool>(false, MessageCodes.BadRequest);
                        }

                        var changePlace = await fileManagerService.ChangePlaceWebpFile(new PlaceFileWebp()
                        {
                            DestinationCombinePath = new List<string>() {
                            configurtion["FileManager:DirectoryAppliedJobMessageAttach"],
                            applicant.Id.ToString() },
                            StreamFile = webpFile.Data.Stream,
                            FileName = webpFile.Data.Name,
                            WhichTypeCreateName = NameType.NameWithDateTime
                        });

                        if (changePlace.MessageCode != MessageCodes.Success)
                        {
                            return new ResultDto<bool>(false, MessageCodes.BadRequest);
                        }

                        item.FileName = changePlace.Data.NameFile;
                        item.Name = changePlace.Data.NameInDataBase;
                    }
                    else
                    {
                        var changePlace = await fileManagerService.ChangePlaceIFormFile(new PlaceFileIFomFile()
                        {
                            DestinationCombinePath = new List<string>() {
                            configurtion["FileManager:DirectoryAppliedJobMessageAttach"],
                            applicant.Id.ToString() },
                            File = iformFile.Data,
                            WhichTypeCreateName = NameType.NameWithDateTime
                        });

                        if (changePlace.MessageCode != MessageCodes.UnHandleException)
                        {
                            return new ResultDto<bool>(false, MessageCodes.BadRequest);
                        }

                        item.FileName = changePlace.Data.NameFile;
                        item.Name = changePlace.Data.NameInDataBase;
                    }
                }
            }

            var result = await applicantService.SaveMessage(dtos);
            if (result)
            {
                return new ResultDto<bool>(true, MessageCodes.Success);
            }
            else
            {
                return new ResultDto<bool>(false, MessageCodes.BadRequest);
            }


        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<bool>> IsOfferExistToOfferFavouriteList(Guid offerId)
        {
            var applicantEmail = GetApplicantEmail().Data;
            var isFavouriteResult = await applicantService.IsOfferExistFavouriteList(new ApplicantOfferFavouriteDto
            {
                ApplicantEmail = applicantEmail,
                OfferId = offerId
            });

            return new ResultDto<bool>(isFavouriteResult.Data, MessageCodes.Success);
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ResultDto<AppliedJobDto> ExistsAppliedJob(Guid offerId)
        {
            var applicantEmail = GetApplicantEmail().Data;
            var result = applicantService.GetAppliedJob(applicantEmail, offerId);
            return new ResultDto<AppliedJobDto>(result);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<AppliedJobsViewModelDto>> GetAppliedJobsList(int currentPage = 1)
        {
            var applicantEmail = GetApplicantEmail().Data;
            var result = await applicantService.GetAllAppliedJobsAsync(applicantEmail, currentPage);
            return new ResultDto<AppliedJobsViewModelDto>(result);
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<PaginationDto<ApplicantAppliedJobDto>>> GetApplicantAppliers(Guid offerId, ApplicationStatus appliedType, int currentPage = 1)
        {
            var companyEmail = GetCompanyEmail().Data;
            var applicantAppliedDtoList = await applicantService.GetApplicantAppliers(offerId, companyEmail, appliedType, currentPage);
            return new ResultDto<PaginationDto<ApplicantAppliedJobDto>>(applicantAppliedDtoList);
        }

        private ResultDto<string> GetCompanyEmail()
        {
            var claims = User.Claims.ToList();
            var companyEmail = claims.FirstOrDefault(x => x.Type.Equals(ClaimTypes.NameIdentifier))?.Value;
            return new ResultDto<string>(companyEmail);
        }


        #region GetEmailPreferences
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<List<ApplicantPreferenceDto>>> GetEmailPreferences()
        {
            return new ResultDto<List<ApplicantPreferenceDto>>(await applicantService.GetApplicantPreferences(GetApplicantEmail().Data));

        }


        #endregion

        #region Affinda

        private async Task<ResultDto<Resume>> AffindaParser(ResumeFileDto resume)
        {
            try
            {
                var options = new RestClientOptions($"{configurtion["Affinda:ResumeParserUrl"]}");
                var client = new RestClient(options);
                var request = new RestRequest("");
                request.AlwaysMultipartFormData = true;
                request.AddHeader("accept", "application/json");
                request.FormBoundary = configurtion["Affinda:FormBoundary"];
                request.AddHeader("authorization", $"Bearer {configurtion["Affinda:APIKey"]}");
                request.AddParameter("wait", "true");
                request.AddFile("file", resume.FileByte, resume.FileName);
                request.AddParameter("collection", $"{configurtion["Affinda:collections"]}");
                request.AddParameter("workspace", $"{configurtion["Affinda:workspaces"]}");
                var response = await client.PostAsync(request);

                if (response.StatusCode is not System.Net.HttpStatusCode.OK)
                    return new ResultDto<Resume>(null, MessageCodes.BadRequest);

                return new ResultDto<Resume>(JsonConvert.DeserializeObject<Resume>(response.Content), MessageCodes.Success);

            }
            catch (Exception)
            {
                return new ResultDto<Resume>(null, MessageCodes.BadRequest);
            }
        }

        private async Task<ResultDto<Resume>> FakeResume()
        {
            var directory = Path.Combine("wwwroot\\ResumeJsonFile");
            var files = Directory.GetFiles(directory);
            var rand = new Random();
            var chooseFile = files[rand.Next(files.Length)];

            var readFile = System.IO.File.ReadAllText(chooseFile);
            using (StreamReader r = new StreamReader(chooseFile))
            {
                var value = r.ReadToEnd();
                var newValue = value.Substring(1, value.Length - 2);
                var resumeValue = JsonConvert.DeserializeObject<Resume>(newValue);
                return new ResultDto<Resume>(resumeValue, MessageCodes.Success);
            }
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<bool>> UploadResumeForAffinda(ResumeFileDto resume)
        {
            try
            {
                var resumeValue = new Resume();

                if (Convert.ToBoolean(configurtion["Affinda:FakeResume"]))
                    resumeValue = FakeResume().Result.Data;
                else
                {
                    if (await applicantService.CheckCVParser(resume.EmailApplicant) > Convert.ToInt32(configurtion["Affinda:Limit"]))
                        return new ResultDto<bool>(false, MessageCodes.BadRequest);

                    var parser = AffindaParser(resume).Result.Data;

                    if (parser is null)
                        resumeValue = FakeResume().Result.Data;
                    else
                        resumeValue = parser;
                }

                await applicantService.InsertApplicantDocument(new ApplicantDocumentDto()
                {
                    ApplicantEmail = resume.EmailApplicant,
                    DocumentFile = resume.FileByte,
                    ExtensionFile = ".pdf",
                    Type = UploadDocumentType.Site_Generated
                });


                var resumeConvert = resumeValue.Data;

                var listOfError = new List<string>();

                var personalInformation = new ApplicantProfileDto();
                personalInformation.Email = resume.EmailApplicant;

                if (!string.IsNullOrEmpty(resumeConvert.Name.First))
                    personalInformation.FirstName = resumeConvert.Name.First;

                if (!string.IsNullOrEmpty(resumeConvert.Name.Last))
                    personalInformation.LastName = resumeConvert.Name.Last;

                if (!string.IsNullOrEmpty(resumeConvert.DateOfBirth))
                    personalInformation.DateOfBirth = Convert.ToDateTime(resumeConvert.DateOfBirth);

                var resultPersonalInforamtion = await applicantService.SavePersonalInformation(personalInformation);

                if (resultPersonalInforamtion is false)
                    listOfError.Add("Error For Save Personal Inforamtion");


                var contentDetail = new ApplicantContactDetailsDto();

                if (resumeConvert.PhoneNumbers.Count > 0)
                    contentDetail.Phone = resumeConvert.PhoneNumbers[0];

                if (!string.IsNullOrEmpty(resumeConvert.Location.Country))
                    contentDetail.CountryCode = countryService.GetCountryCode(resumeConvert.Location.Country);

                if (!string.IsNullOrEmpty(resumeConvert.Location.City))
                    contentDetail.CityName = resumeConvert.Location.City;

                if (!string.IsNullOrEmpty(resumeConvert.Location.PostalCode))
                    contentDetail.PostalCode = resumeConvert.Location.PostalCode;

                if (!string.IsNullOrEmpty(resumeConvert.Location.Street))
                    contentDetail.Address = resumeConvert.Location.Street + resumeConvert.Location.StreetNumber;

                var resultContactDetail = await applicantService.SaveApplicantContactDetail(contentDetail, resume.EmailApplicant);

                if (resultContactDetail is false)
                    listOfError.Add("Error For Save Contact Detail");

                if (resumeConvert.WorkExperience.Count() > 0)
                {
                    foreach (var item in resumeConvert.WorkExperience)
                    {
                        var workExperience = new ApplicantWorkExperienceDto();
                        workExperience.ApplicantEmail = resume.EmailApplicant;
                        workExperience.JobTitle = item.JobTitle;
                        workExperience.JobPosition = item.Occupation.JobTitle;
                        if (item.Dates is not null)
                        {
                            workExperience.StartWork = !string.IsNullOrEmpty(item.Dates.StartDate) ? Convert.ToDateTime(item.Dates.StartDate) : null;
                            workExperience.EndWork = !string.IsNullOrEmpty(item.Dates.EndDate) ? Convert.ToDateTime(item.Dates.EndDate) : null;
                        }
                        var resultWorkExperience = await applicantService.InsertOrUpdateWorkExperiance(workExperience);
                        if (resultWorkExperience is false)
                            listOfError.Add($"Error For Save Work Experience From {item.JobTitle}");
                    }
                }

                if (resumeConvert.Education.Count() > 0)
                {
                    foreach (var item in resumeConvert.Education)
                    {
                        var education = new ApplicantEducationDto();
                        education.ApplicantEmail = resume.EmailApplicant;
                        education.EducationLevel = EducationLevel.HighSchool;
                        education.InstituteName = !string.IsNullOrEmpty(item.Accreditation.InputStr) ? item.Accreditation.InputStr : "";
                        education.CourseName = item.Accreditation.Education;
                        if (item.Dates is not null)
                        {
                            education.StartEducation = !string.IsNullOrEmpty(item.Dates.StartDate) ? Convert.ToDateTime(item.Dates.StartDate) : null;
                            education.EndEducation = !string.IsNullOrEmpty(item.Dates.StartDate) ? Convert.ToDateTime(item.Dates.EndDate) : null;
                        }
                        var resultEducation = await applicantService.CreateOrUpdateEducation(education);
                        if (resultEducation is false)
                            listOfError.Add($"Error For Save Education From {item.Accreditation.Education}");
                    }
                }


                if (resumeConvert.Skills.Count() > 0)
                {
                    foreach (var item in resumeConvert.Skills)
                    {
                        var knowledge = new ApplicantKnowledgeDto();
                        knowledge.ApplicantEmail = resume.EmailApplicant;
                        knowledge.KnowledgeName = item.Name;
                        knowledge.KnowledgeLevel = KnowledgeLevel.Intermediate;

                        var resultKnowledge = await applicantService.CreateOrUpdateKhnowledge(knowledge);
                        if (resultKnowledge is false)
                            listOfError.Add($"Error For Save Knowledge From {item.Name}");
                    }
                }

                if (resumeConvert.Languages.Count() > 0)
                {
                    foreach (var item in resumeConvert.Languages)
                    {
                        var language = new ApplicantLanguageDto();
                        language.ApplicantEmail = resume.EmailApplicant;
                        language.LanguageLevel = SkillLevel.MotherTongue;
                        language.LanguageName = item;

                        var resultLanguage = await applicantService.CreateOrUpdateLanguage(language);
                        if (resultLanguage is false)
                            listOfError.Add($"Error For Save Language From {item}");
                    }
                }

                return new ResultDto<bool>(true, MessageCodes.Success);
            }
            catch (Exception)
            {

                return new ResultDto<bool>(false, MessageCodes.BadRequest);
            }
        }
        #endregion

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<bool>> UpdateVerifyStatusProfile(VerifyStatusApplicantProfileDto status)
        {
            var result = await applicantService.UpdateVerifyStatusApplicant(status);
            if (result is true)
                return new ResultDto<bool>(true, MessageCodes.Success);
            else
                return new ResultDto<bool>(false, MessageCodes.BadRequest);
        }

        #region Blacklist Compnay 
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<bool>> UpdateApplicantBlacklistOfCompany([FromBody] List<string> selectedEmails)
        {
            var emailApplicant = GetApplicantEmail().Data;

            if (emailApplicant == null)
            {
                return new ResultDto<bool>(false, MessageCodes.Unauthorized);
            }


            var result = await applicantService.UpdateApplicantBlacklistOfCompany(selectedEmails, emailApplicant);


            if (result.MessageCode == MessageCodes.Success)
            {
                return new ResultDto<bool>(true, MessageCodes.Success);
            }
            else if (result.MessageCode == MessageCodes.Unauthorized)
            {
                return new ResultDto<bool>(false, MessageCodes.Unauthorized);
            }
            else if (result.MessageCode == MessageCodes.BadRequest)
            {
                return new ResultDto<bool>(false, MessageCodes.BadRequest);
            }
            else
            {
                return new ResultDto<bool>(false, MessageCodes.UnHandleException);
            }
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<ApplicantBlacklistOfCompanyResult>> SaveApplicantBlacklistOfCompany(Guid offerId, string companyEmail)
        {
            if (offerId == Guid.Empty) return new ResultDto<ApplicantBlacklistOfCompanyResult>(ApplicantBlacklistOfCompanyResult.Success);
            var applicantBlackListOfCompanyDto = new ApplicantBlacklistOfCompanyDto()
            {
                ApplicantEmail = GetApplicantEmail().Data,
                OfferId = offerId
            };
            if (companyEmail.Equals(applicantBlackListOfCompanyDto.ApplicantEmail))
            {
                return new ResultDto<ApplicantBlacklistOfCompanyResult>(ApplicantBlacklistOfCompanyResult.YouCannotBlock);
            }
            var result = await applicantService.SaveApplicantBlacklistOfCompany(applicantBlackListOfCompanyDto);
            if (result == ApplicantBlacklistOfCompanyResult.Success)
            {
                return new ResultDto<ApplicantBlacklistOfCompanyResult>(ApplicantBlacklistOfCompanyResult.Success);
            }
            if (result == ApplicantBlacklistOfCompanyResult.Badrequest)
            {
                return new ResultDto<ApplicantBlacklistOfCompanyResult>(ApplicantBlacklistOfCompanyResult.Badrequest);
            }

            return new ResultDto<ApplicantBlacklistOfCompanyResult>(ApplicantBlacklistOfCompanyResult.AddedBefore);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ResultDto<List<OfferDto>> GetAllBlackListOfCompany()
        {
            var model = applicantService.GetAllBlackListOfCompany(GetApplicantEmail().Data);
            return new ResultDto<List<OfferDto>>(model);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<BlacklistOfCompanyDto>> GetAllBlacklistOfCompanyData(int currentPage, string company)
        {
            var offers = await applicantService.GetAllBlackListOfCompanyWithPageingAsync(GetApplicantEmail().Data, currentPage, company);
            var companies = applicantService.GetAllCompanies();

            var data = new BlacklistOfCompanyDto
            {
                Offers = offers,
                Companies = companies
            };

            return new ResultDto<BlacklistOfCompanyDto>(data);
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ResultDto<bool>> DeleteBlackLisCompany(Guid offerId)
        {
            var result = await applicantService.DeleteBlackLisCompany(offerId, GetApplicantEmail().Data);
            return new ResultDto<bool>(result);
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ResultDto<int> GetCountBlackLisOfCompany()
        {
            var count = applicantService.GetCountBlackLisOfCompany(GetApplicantEmail().Data);
            return new ResultDto<int>(count);
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ResultDto<List<ApplicantBlacklistOfCompanyDto>> GetApplicantBlacklistOfCompanyDtos()
        {
            var model = applicantService.applicantBlacklistOfCompanyDtos(GetApplicantEmail().Data);
            return new ResultDto<List<ApplicantBlacklistOfCompanyDto>>(model);
        }
        #endregion
    }
}