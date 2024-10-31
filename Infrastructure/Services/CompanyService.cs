using AutoMapper;
using DotNek.Common.Dtos;
using DotNek.Common.Interfaces;
using DotNek.WebComponents.Areas.Payment.Dtos;
using DotNek.WebComponents.Areas.Payment.Interfaces;
using FindJobs.DataAccess.Entities;
using FindJobs.Domain.Dtos;
using FindJobs.Domain.Dtos.Email;
using FindJobs.Domain.Enums;
using FindJobs.Domain.Global;
using FindJobs.Domain.Repositories;
using FindJobs.Domain.Services;
using FindJobs.Domain.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using EmailSendResult = FindJobs.Domain.Enums.EmailSendResult;

namespace FindJobs.Infrastructure.Services
{
    public class CompanyService : ICompanyService
    {

        private readonly IMailService senderService;
        private readonly IGenericRepository<Company> companyRepository;
        private readonly IGenericRepository<Offer> offerRepository;
        private readonly IMapper mapper;
        private readonly IConfiguration configuration;
        private readonly IEncryption encryption;
        private readonly IGenericRepository<Language> languageRepository;
        private readonly IGenericRepository<Benefits> benefitsReposiotry;
        private readonly IGenericRepository<Knowledge> knowledgeRepository;
        private readonly IGenericRepository<Currency> currencyRepository;
        private readonly IGenericRepository<Package> packageRepository;
        private readonly IGenericRepository<OfferLanguage> offerLanguageRepository;
        private readonly IGenericRepository<OfferBenefits> offerBenefitsRepository;
        private readonly IGenericRepository<OfferKnowledge> offerKnowledgeRepository;
        private readonly IGenericRepository<OfferJobCategory> offerJobCategoryRepository;
        private readonly IGenericRepository<Applicant> applicantRepository;
        private readonly ICountryService countryService;
        private readonly IGenericRepository<AppliedJobMessage> appliedJobMessageRepository;
        private readonly ICitiesService citiesService;
        private readonly IApplicantService applicantService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IVATService vatService;
        private readonly IGenericRepository<AppliedJob> applicantAppliedJobsRepository;
        private readonly IGenericRepository<CompanyApplicantFavourite> companyApplicantFavouriteRepository;
        private readonly IPaymentDataService paymentDataService;
        private readonly IGenericRepository<ApplicantDocument> applicantDocumentRepository;
        private readonly IFileManagerService fileManagerService;
        private readonly IGenericRepository<Applicant> profileRepository;
        private readonly IGenericRepository<JobCategory> jobCategoryRepository;
        private readonly IGenericRepository<Benefits> benefitsRepository;
        private readonly IGenericRepository<Payment> paymentRepository;
        private readonly IGenericRepository<FindJobs.DataAccess.Entities.AppliedJobMessageAttach> appliedJobMessageAttachRepository;
        public CompanyService(IMailService senderService,
            IGenericRepository<Company> companyRepository,
           IGenericRepository<Offer> offerRepository,
           IMapper mapper,
           IConfiguration configuration,
           IEncryption encryption,
           IGenericRepository<Language> LanguageRepository,
           IGenericRepository<Knowledge> KnowledgeRepository,
           IGenericRepository<Currency> CurrencyRepository,
           IGenericRepository<Package> PackageRepository,
             IGenericRepository<OfferLanguage> OfferLanguageRepository,
              IGenericRepository<OfferKnowledge> OfferKnowledgeRepository,
              IGenericRepository<OfferJobCategory> offerJobCategoryRepository,
              IGenericRepository<Applicant> applicantRepository,
              ICountryService countryService,
              ICitiesService citiesService,
              IApplicantService applicantService,
              IWebHostEnvironment webHostEnvironment
,
              IVATService vatService
,
              IPaymentDataService paymentDataService,
              IGenericRepository<Benefits> benefitsReposiotry,
              IGenericRepository<OfferBenefits> offerBenefitsRepository,
              IGenericRepository<ApplicantDocument> applicantDocumentRepository,
              IFileManagerService fileManagerService,
              IGenericRepository<Applicant> profileRepository,
              IGenericRepository<AppliedJobMessage> appliedJobMessageRepository,
              IGenericRepository<JobCategory> jobCategoryRepository,
              IGenericRepository<Benefits> benefitsRepository,
              IGenericRepository<City> cityRepository,
              IGenericRepository<Country> countryRepository,
              IGenericRepository<ApplicantBlacklistOfCompany> applicantBlacklistOfCompanyRepository,
              IGenericRepository<ApplicantOfferFavourite> applicantOfferFavouritRepository,
              IGenericRepository<CompanyApplicantFavourite> companyApplicantFavouriteRepository,
              IGenericRepository<AppliedJob> applicantAppliedJobsRepository,
              IGenericRepository<DataAccess.Entities.AppliedJobMessageAttach> appliedJobMessageAttachRepository)
        {

            this.senderService = senderService;
            this.companyRepository = companyRepository;
            this.offerRepository = offerRepository;
            this.mapper = mapper;
            this.configuration = configuration;
            this.encryption = encryption;
            languageRepository = LanguageRepository;
            knowledgeRepository = KnowledgeRepository;
            currencyRepository = CurrencyRepository;
            packageRepository = PackageRepository;
            offerLanguageRepository = OfferLanguageRepository;
            offerKnowledgeRepository = OfferKnowledgeRepository;
            this.offerJobCategoryRepository = offerJobCategoryRepository;
            this.applicantRepository = applicantRepository;
            this.countryService = countryService;
            this.citiesService = citiesService;
            this.applicantService = applicantService;
            this.webHostEnvironment = webHostEnvironment;
            this.vatService = vatService;
            this.paymentDataService = paymentDataService;
            this.benefitsReposiotry = benefitsReposiotry;
            this.offerBenefitsRepository = offerBenefitsRepository;
            this.applicantDocumentRepository = applicantDocumentRepository;
            this.fileManagerService = fileManagerService;
            this.profileRepository = profileRepository;
            this.appliedJobMessageRepository = appliedJobMessageRepository;
            this.jobCategoryRepository = jobCategoryRepository;
            this.benefitsRepository = benefitsRepository;
            this.companyApplicantFavouriteRepository = companyApplicantFavouriteRepository;
            this.applicantAppliedJobsRepository = applicantAppliedJobsRepository;
            this.appliedJobMessageAttachRepository = appliedJobMessageAttachRepository;
        }

        public async Task<EmailSendResult> SendOfferEmails(string applicantEmail, Guid offerId)
        {
            var offer = offerRepository.GetEntities().FirstOrDefault(x => x.Id.Equals(offerId));
            var offerDto = mapper.Map<OfferDto>(offer);

            SendOfferEmail sendOfferEmail = new SendOfferEmail
            {
                Model = new List<OfferDto> { offerDto },
                HeaderTitle = Res.Email.HeaderTitle,
                HeaderSubTitle = Res.Email.HeaderSubTitle,
                InstagramLink = configuration["GlobalSettings:InstagramLink"],
                FacebookLink = configuration["GlobalSettings:FacebookLink"],
                TwitterLink = configuration["GlobalSettings:TwitterLink"],
            };

            var encryptedEmail = HttpUtility.UrlEncode(encryption.Encrypt(applicantEmail, configuration["GlobalSettings:EncryptionSalt"]));
            sendOfferEmail.UnsubscribeLink = configuration["GlobalSettings:WebUrl"] + "UnSubscribe/" + encryptedEmail;
            offerDto.Link = configuration["GlobalSettings:WebUrl"] + "Offer/" + offerDto.Id;

            var designAttachments = new string[] { "logo.png", "instagram.png", "twitter.png", "facebook.png" };
            var emailBody = await senderService.CreateBodyFromView<dynamic>("/Views/Email/JobOffers.cshtml", sendOfferEmail, designAttachments, webHostEnvironment.WebRootPath + "/images/");

            var result = await senderService.SendMail(new MailSenderConfig(configuration["MailSettings:SmtpServer"], configuration["MailSettings:EmailUsername"], configuration["MailSettings:EmailPassword"], applicantEmail, Res.Email.sendOffer, emailBody.Body, emailBody.AlternateView));

            if (result.Data)
                return EmailSendResult.Success;

            return EmailSendResult.NotSend;
        }
        public async Task<EmailSendResult> SendInvoiceBeforePayment(OfferDto offerDto)
        {
            var offer = offerRepository.GetEntities().FirstOrDefault(x => x.Id == offerDto.Id);
            double totalPrice = (double)offerDto.Price;
            double vat = (double)offerDto.Vat;
            double PayableAmount = totalPrice + vat;
            var sendDtoLink = new SendLinkDto
            {
                Email = offerDto.CompanyEmail,
                Id = offerDto.Id,
                PayableAmount = PayableAmount,
                Price = offerDto.Price,
                PackageName = offerDto.PackageName,
                Vat = (int)vat,
                RedirectUrl = "http://example.com/pay",
                Authority = ""
            };

            var emailBody = await paymentDataService.GenerateEmailBody(sendDtoLink);
            var result = await senderService.SendMail(new MailSenderConfig(configuration["MailSettings:SmtpServer"], configuration["MailSettings:EmailUsername"], configuration["MailSettings:EmailPassword"], offerDto.CompanyEmail, "Payment link", emailBody.Body, emailBody.AlternateView));

            if (result.Data)
                return EmailSendResult.Success;
            return EmailSendResult.NotSend;
        }
        public async Task<string> GetEmailCompanyById(int id)
        {
            var company = await companyRepository.GetEntities().FirstOrDefaultAsync(c => c.Id == id);
            return company.Email;
        }
        public async Task<CompanyDto> GetCompanyById(int id)
        {
            var company = await companyRepository.GetEntities().Include(x => x.City).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (company == null) return null;
            var companyDto = mapper.Map<CompanyDto>(company);

            companyDto.CountryName = await countryService.GetCountryName(company.CountryCode);
            if (company.CityId != null)
                companyDto.StateName = citiesService.GetStateByCityId((long)company.CityId);

            return companyDto;
        }
        public async Task<CompanyDto> GetCompanyByEmail(string email)
        {
            var company = await companyRepository.GetEntities().Include(x => x.City).AsNoTracking().FirstOrDefaultAsync(x => x.Email.Equals(email));
            if (company == null) return null;
            var companyDto = mapper.Map<CompanyDto>(company);

            companyDto.CountryName = await countryService.GetCountryName(company.CountryCode);
            if (company.CityId != null)
            {
                companyDto.StateName = citiesService.GetStateByCityId((long)company.CityId);
            }


            return companyDto;
        }

        public List<OfferDto> GetOffersList()
        {
            var offers = offerRepository.GetEntities()
                .Include(x => x.ApplicantOffersFavourites)
                 .Include(x => x.Company)
                 .ThenInclude(x => x.City)
                 .Include(x => x.OfferLanguages)
                 .ThenInclude(x => x.Language)
                 .Include(x => x.OfferKnowledges)
                 .ThenInclude(x => x.Knowledge)
                 .Include(x => x.OfferBenefits)
                 .ThenInclude(x => x.Benefits)
                .Include(x => x.OfferJobCategories)
                .ThenInclude(x => x.JobCategory).AsQueryable();
            var offersDto = mapper.Map<List<OfferDto>>(offers.ToList());
            return offersDto;
        }

        public PaginationDto<OfferMobileModel> GetOffersByCompanyEmail(string companyEmail, int currentPage = 1, OfferStatus status = OfferStatus.All, string offerName = "")
        {
            var itemPerPage = 5;
            var offerDtoList = GetOffersList().Where(x => x.CompanyEmail.Equals(companyEmail));

            if (!string.IsNullOrEmpty(offerName))
            {
                offerDtoList = offerDtoList.Where(x => x.JobTitle.Contains(offerName, StringComparison.OrdinalIgnoreCase));
            }
            if (status != OfferStatus.All)
            {
                if (status == OfferStatus.ActiveJob)
                {
                    offerDtoList = offerDtoList.Where(x => x.Status == OfferStatus.ActiveJob && x.ExprationDate >= DateTime.Now && x.PaymentStatus == true);
                }
                else if (status == OfferStatus.InActiveJobs)
                {
                    offerDtoList = offerDtoList.Where(x => x.Status == OfferStatus.InActiveJobs);
                }
            }

            var model = GetOfferMobilesList(offerDtoList.ToList());
            var count = model.Count;
            var skip = Math.Min((currentPage - 1) * itemPerPage, count - 1);
            var filterOfferMobile = model.Skip(skip).Take(itemPerPage).ToList();

            var result = new PaginationDto<OfferMobileModel>
            {
                Data = filterOfferMobile,
                PageCount = (int)Math.Ceiling(((double)count / itemPerPage)),
                ItemsCount = count,
                Page = currentPage
            };

            return result;
        }


        public async Task<OfferDto> GetOfferById(Guid id)
        {
            var jobOffer = mapper.Map<OfferDto>(GetOffersList().FirstOrDefault(offer => offer.Id == id));


            return jobOffer;
        }
        public async Task<OfferDto> GetCurrencyByOfferId(Guid id, string currencyCodeUser)
        {
            var jobOffer = mapper.Map<OfferDto>(GetOffersList().FirstOrDefault(offer => offer.Id == id));

            jobOffer.BasicSalaryRegion = await ConvertCurrency(currencyCodeUser, jobOffer.CurrencyCode, (decimal)jobOffer.BasicSalary);
            jobOffer.UpperLimitRegion = await ConvertCurrency(currencyCodeUser, jobOffer.CurrencyCode, (decimal)jobOffer.UpperLimit);
            jobOffer.CurrencyCodeRegion = currencyCodeUser;

            return jobOffer;
        }
        private async Task<decimal> ConvertCurrency(string fromCurrency, string toCurrency, decimal amount)
        {

            var fromRate = await currencyRepository.GetEntities().FirstOrDefaultAsync(c => c.Code == fromCurrency);
            var toRate = await currencyRepository.GetEntities().FirstOrDefaultAsync(c => c.Code == toCurrency);

            decimal convertedAmount = amount * (toRate.CurrencyRate / fromRate.CurrencyRate);
            return convertedAmount;
        }

        public async Task<List<OfferDto>> GetOffers()
        {
            return mapper.Map<List<OfferDto>>(await offerRepository.GetEntities().ToListAsync());
        }
        public async Task<List<CompanyDto>> GetTopEmployers()
        {
            var companies = await companyRepository.GetEntities().Where(x => x.IsTop).ToListAsync();
            return mapper.Map<List<CompanyDto>>(companies);
        }

        public async Task<bool> UpdateCompany(CompanyDto companyDto)
        {
            try
            {
                var anyCompany = await companyRepository.GetEntities().AsNoTracking()
                   .FirstOrDefaultAsync(x => x.CompanyRegistrationId == companyDto.CompanyRegistrationId);
                if (anyCompany != null)
                {
                    if (anyCompany.CountryCode == companyDto.CountryCode && companyDto.Email != anyCompany.Email)
                    {
                        return false;
                    }
                }

                var company = await companyRepository.GetEntities().AsNoTracking().SingleOrDefaultAsync(x => x.Id == companyDto.Id);
                if (company == null)
                    return false;
                var companyNew = mapper.Map<Company>(companyDto);
                companyRepository.UpdateEntity(companyNew);
                await companyRepository.SaveChange();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool GetCompanyRole(ClaimsPrincipal user)
        => user.Claims.Where(a => a.Type.Equals(ClaimTypes.Role)).ToList().Select(x => x.Value.Equals((int)RoleType.Company)).FirstOrDefault();

        public async Task<JobOfferViewModel> GetJobOfferViewModel(string email)
        {

            var getBenefits = await benefitsReposiotry.GetEntities().ToListAsync();
            var benefits = mapper.Map<List<BenefitsDto>>(getBenefits);
            var company = await companyRepository.GetEntities().SingleOrDefaultAsync(x => x.Email.Equals(email));

            var allKnowledgeDtos = mapper.Map<List<KnowledgeDto>>(await knowledgeRepository.GetEntities().ToListAsync());
            var allLanguageDtos = mapper.Map<List<LanguageDto>>(await languageRepository.GetEntities().ToListAsync());
            var packageDtos = mapper.Map<List<PackageDto>>(await packageRepository.GetEntities().ToListAsync());
            var jobCategories = mapper.Map<List<JobCategoryDto>>(await jobCategoryRepository.GetEntities().ToListAsync());
            var jobOfferViewModel = new JobOfferViewModel
            {
                CurrencyDtos = mapper.Map<List<CurrencyDto>>(await currencyRepository.GetEntities().ToListAsync()),
                AllKnowledgeDtos = allKnowledgeDtos,
                LanguageDtos = allLanguageDtos,
                PackageDtos = packageDtos,
                CompanyDto = mapper.Map<CompanyDto>(company),
                AllBenefits = benefits,
                OfferDto = new OfferDto(),

                RequiredSeniorKnowledges = new List<KnowledgeDto>(),
                RequiredJuniorKnowledges = new List<KnowledgeDto>(),
                OptionalKnowledges = new List<KnowledgeDto>(),
                SelectedRequiredLanguages = new List<LanguageDto>(),
                SelectedOptionalLanguages = new List<LanguageDto>(),
                SelectedBenefits = new List<BenefitsDto>(),
                SelectedJobCategories = new List<int>(),
            };

            return jobOfferViewModel;
        }
        public async Task<Guid> SaveOrUpdateJobOffer(OfferDto offerDto)
        {
            var offer = await offerRepository.GetEntities().FirstOrDefaultAsync(x => x.Id == offerDto.Id);

            if (offer == null)
            {
                offer = mapper.Map<Offer>(offerDto);
                offer.Company = await companyRepository.GetEntities().FirstOrDefaultAsync(x => x.Email == offer.CompanyEmail);

                var package = packageRepository.GetEntities().FirstOrDefault(x => x.PackageName.Equals(offerDto.PackageName));
                if (package != null)
                {
                    var currencyRating = currencyRepository.GetEntities().SingleOrDefault(x => x.Code.Equals(offerDto.CurrencyCode));
                    if (currencyRating != null)
                        offer.Price = package.Price * currencyRating.CurrencyRate;

                    offer.ExprationDate = offer.CreateDate.AddDays(package.DurationInDays);

                    if (vatService.IsPriceIncludedVAT(offer.Company.CountryCode, offer.Company.VatNumber).Data)
                    {
                        if (vatService.ValidateVATID(offer.Company.Name, offer.Company.CountryCode, offer.Company.VatNumber).Data)
                        {
                            var vatAmount = configuration["TaxSettings:SiteOwnerVatAmount"];
                            offer.Vat = (Convert.ToDecimal(offer.Price) * (Convert.ToDecimal(vatAmount) / 100));
                        }
                    }
                    else
                    {
                        offer.Vat = 0;
                    }
                }

                await offerRepository.AddEntity(offer);
            }
            else
            {

                mapper.Map(offerDto, offer);
                offer.Company = await companyRepository.GetEntities().FirstOrDefaultAsync(x => x.Email == offer.CompanyEmail);
                var package = packageRepository.GetEntities().FirstOrDefault(x => x.PackageName.Equals(offerDto.PackageName));
                if (package != null)
                {
                    var currencyRating = currencyRepository.GetEntities().SingleOrDefault(x => x.Code.Equals(offerDto.CurrencyCode));
                    if (currencyRating != null)
                        offer.Price = package.Price * currencyRating.CurrencyRate;

                    offer.ExprationDate = offer.CreateDate.AddDays(package.DurationInDays);

                }

                offerRepository.UpdateEntity(offer);
            }

            await offerRepository.SaveChange();

            if (offerDto.LanguageSkillRequireds != null)
            {
                foreach (var item in offerDto.LanguageSkillRequireds)
                {
                    var existingOfferLanguage = await GetByOfferIdAndLanguageIdAsync(offer.Id, item);

                    if (existingOfferLanguage != null)
                    {
                        existingOfferLanguage.LanguageLevel = OfferLanguageType.Required;
                        offerLanguageRepository.UpdateEntity(existingOfferLanguage);
                    }
                    else
                    {

                        var offerLanguage = new OfferLanguage()
                        {
                            LanguageId = item,
                            LanguageLevel = OfferLanguageType.Required,
                            OfferId = offer.Id
                        };
                        await offerLanguageRepository.AddEntity(offerLanguage);
                    }
                }
                await offerLanguageRepository.SaveChange();
            }


            if (offerDto.LanguageProficiencyStatus != null)
            {
                foreach (var item in offerDto.LanguageProficiencyStatus)
                {
                    var existingOfferLanguage = await GetByOfferIdAndLanguageIdAsync(offer.Id, (int)item);

                    if (existingOfferLanguage != null)
                    {

                        existingOfferLanguage.LanguageProficiencyStatus = item;
                        existingOfferLanguage.LanguageLevel = OfferLanguageType.Optional;
                        offerLanguageRepository.UpdateEntity(existingOfferLanguage);
                    }
                    else
                    {

                        var offerLanguage = new OfferLanguage()
                        {
                            LanguageId = (int)item,
                            LanguageProficiencyStatus = item,
                            LanguageLevel = OfferLanguageType.Optional,
                            OfferId = offer.Id
                        };
                        await offerLanguageRepository.AddEntity(offerLanguage);
                    }
                }
                await offerLanguageRepository.SaveChange();
            }


            if (offerDto.JobCategories != null)
            {
                foreach (var item in offerDto.JobCategories)
                {
                    var existingOfferJobCategory = await GetByOfferIdAndJobCategoryIdAsync(offer.Id);

                    if (existingOfferJobCategory != null)
                    {
                        existingOfferJobCategory.JobCategoryId = item;
                        offerJobCategoryRepository.UpdateEntity(existingOfferJobCategory);
                    }
                    else
                    {
                        var offerJobCategory = new OfferJobCategory()
                        {
                            JobCategoryId = item,
                            OfferId = offer.Id
                        };
                        await offerJobCategoryRepository.AddEntity(offerJobCategory);
                    }
                }
                await offerJobCategoryRepository.SaveChange();
            }


            if (offerDto.Benefits != null)
            {
                int benefitsIdCounter = 1;
                foreach (var item in offerDto.Benefits)
                {
                    var existingOfferBenefit = await GetByOfferIdAndBenefitsIdAsync(offer.Id, benefitsIdCounter);

                    if (existingOfferBenefit != null)
                    {

                        existingOfferBenefit.BenefitsImage = item;
                        offerBenefitsRepository.UpdateEntity(existingOfferBenefit);
                    }
                    else
                    {
                        var offerBenefits = new OfferBenefits()
                        {
                            BenefitsId = benefitsIdCounter,
                            BenefitsImage = item,
                            OfferId = offer.Id
                        };
                        await offerBenefitsRepository.AddEntity(offerBenefits);
                    }
                    benefitsIdCounter++;
                }
                await offerBenefitsRepository.SaveChange();
            }

            if (offerDto.RequiredSeniorKnowledges != null)
            {
                foreach (var item in offerDto.RequiredSeniorKnowledges)
                {
                    var existingOfferKnowledge = await GetByOfferIdAndKnowledgeIdAsync(offer.Id, item);

                    if (existingOfferKnowledge != null)
                    {

                        existingOfferKnowledge.KnowledgeLevel = (int)OfferKnowledgeType.RequiredKnowledge;
                        offerKnowledgeRepository.UpdateEntity(existingOfferKnowledge);
                    }
                    else
                    {
                        var offerKnowledge = new OfferKnowledge()
                        {
                            KnowledgeId = item,
                            KnowledgeLevel = (int)OfferKnowledgeType.RequiredKnowledge,
                            OfferId = offer.Id
                        };
                        await offerKnowledgeRepository.AddEntity(offerKnowledge);
                    }
                }
                await offerKnowledgeRepository.SaveChange();
            }

            if (offerDto.RequiredJuniorKnowledges != null)
            {
                foreach (var item in offerDto.RequiredJuniorKnowledges)
                {
                    var existingOfferKnowledge = await GetByOfferIdAndKnowledgeIdAsync(offer.Id, item);

                    if (existingOfferKnowledge != null)
                    {

                        existingOfferKnowledge.KnowledgeLevel = (int)OfferKnowledgeType.AddvantageRequiredKnowledge;
                        offerKnowledgeRepository.UpdateEntity(existingOfferKnowledge);
                    }
                    else
                    {
                        var offerKnowledge = new OfferKnowledge()
                        {
                            KnowledgeId = item,
                            KnowledgeLevel = (int)OfferKnowledgeType.AddvantageRequiredKnowledge,
                            OfferId = offer.Id
                        };
                        await offerKnowledgeRepository.AddEntity(offerKnowledge);
                    }
                }
                await offerKnowledgeRepository.SaveChange();
            }


            if (offerDto.OptionalKnowledges != null)
            {
                foreach (var item in offerDto.OptionalKnowledges)
                {
                    var existingOfferKnowledge = await GetByOfferIdAndKnowledgeIdAsync(offer.Id, item);

                    if (existingOfferKnowledge != null)
                    {

                        existingOfferKnowledge.KnowledgeLevel = (int)OfferKnowledgeType.AddvantageOptionalKnowledge;
                        offerKnowledgeRepository.UpdateEntity(existingOfferKnowledge);
                    }
                    else
                    {
                        var offerKnowledge = new OfferKnowledge()
                        {
                            KnowledgeId = item,
                            KnowledgeLevel = (int)OfferKnowledgeType.AddvantageOptionalKnowledge,
                            OfferId = offer.Id
                        };
                        await offerKnowledgeRepository.AddEntity(offerKnowledge);
                    }
                }
                await offerKnowledgeRepository.SaveChange();
            }

            SendInvoiceBeforePayment(offerDto);
            return offer.Id;
        }

        private async Task<OfferLanguage> GetByOfferIdAndLanguageIdAsync(Guid offerId, int languageId)
        {
            return await offerLanguageRepository.GetEntities()
                .FirstOrDefaultAsync(ol => ol.OfferId == offerId && ol.LanguageId == languageId);
        }
        private async Task<OfferJobCategory> GetByOfferIdAndJobCategoryIdAsync(Guid offerId)
        {

            var offer = await offerJobCategoryRepository.GetEntities()
               .FirstOrDefaultAsync(oj => oj.OfferId == offerId );
            return offer;
        }
        private async Task<OfferKnowledge> GetByOfferIdAndKnowledgeIdAsync(Guid offerId, int knowledgeId)
        {
            return await offerKnowledgeRepository.GetEntities()
                .FirstOrDefaultAsync(ok => ok.OfferId == offerId && ok.KnowledgeId == knowledgeId);
        }
        private async Task<OfferBenefits> GetByOfferIdAndBenefitsIdAsync(Guid offerId, int benefitsId)
        {
            return await offerBenefitsRepository.GetEntities()
                .FirstOrDefaultAsync(ob => ob.OfferId == offerId && ob.BenefitsId == benefitsId);
        }
        //this method come from GetoffersByFilters

        public async Task<PaginationDto<OfferMobileModel>> GetoffersFiltersAsync(List<string> jobCategories = null, List<string> EmployeeTypes = null, List<string> LanguageSkills = null, List<string> WorkAreas = null, int pageNumber = 1, int pageSize = 5, int? minSalary = null, int? maxSalary = null, string keyword = "", string location = "", string currency = "", string countryName = "", int updatedInTheLastDays = 0)
        {

            var model = GetOffersList().Where(x => x.Status == OfferStatus.ActiveJob && x.ExprationDate >= DateTime.Now && x.PaymentStatus == true).AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {

                model = model.Where(x => (x.CompanyDto.Name != null ? x.CompanyDto.Name : "").ToLower().Contains(keyword.ToLower()) ||
                (x.CompanyDto.AboutCompany != null ? x.CompanyDto.AboutCompany : "").ToLower().Contains(keyword.ToLower()) ||
                (x.StateName != null ? x.StateName : "").ToLower().Contains(keyword.ToLower()) ||
                (x.CompanyDto.WebSite != null ? x.CompanyDto.WebSite : "").ToLower().Contains(keyword.ToLower()) ||
                (x.JobDescription != null ? x.JobDescription : "").ToLower().Contains(keyword.ToLower()) ||
                (x.JobTitle != null ? x.JobTitle : "").ToLower().Contains(keyword.ToLower()) ||
                   (x.OfferKnowledgeDtos != null && x.OfferKnowledgeDtos
            .Any(ok => ok.KnowledgeDto != null && ok.KnowledgeDto.Name.ToLower().Contains(keyword.ToLower()))));

            }
            if (!string.IsNullOrWhiteSpace(location))
            {
                model = model.Where(x => x.CityName.ToLower().Trim().StartsWith(location.ToLower().Trim()));
            }
            if (jobCategories.Count() > 0)
            {
                model = model.Where(x => x.OfferJobCategoryDtos.Any(s => jobCategories.Contains(s.JobCategoryDto.Jobcategory)));
            }
            //calculate price
            if (minSalary >= 0)
            {

                model = model.Where(x => ToCurrentCurrency(ToMonthlySalary(x.BasicSalary, x.PerType.ToString()), x.CurrencyCode, currency) >= minSalary && ToCurrentCurrency(ToMonthlySalary(x.BasicSalary, x.PerType.ToString()), x.CurrencyCode, currency) <= maxSalary);
            }
            if (minSalary == null || maxSalary == null)
            {
                var minOfferPrice = model.Min(o => o.BasicSalary);
                var maxOfferPrice = model.Max(o => o.UpperLimit);

                if (minOfferPrice.HasValue && maxOfferPrice.HasValue && minOfferPrice >= 0 && maxOfferPrice >= minOfferPrice)
                {
                    minSalary = (int)minOfferPrice;
                    maxSalary = (int)maxOfferPrice;
                }
            }

            if (EmployeeTypes.Count > 0)
            {

                var IsFullTime = EmployeeTypes.Contains("IsFullTime") ? true : false;
                var IsPartTime = EmployeeTypes.Contains("IsPartTime") ? true : false;
                var IsFreelancer = EmployeeTypes.Contains("IsFreelancer") ? true : false;
                var IsInternShip = EmployeeTypes.Contains("IsInternShip") ? true : false;

                model = model.Where(x =>
                (x.IsFullTime ? IsFullTime : false)
                 || (x.IsPartTime ? IsPartTime : false)
                 || (x.IsFreelancer ? IsFreelancer : false)
                 || (x.IsInternShip ? IsInternShip : false)

                 ).AsQueryable();


            }

            if (LanguageSkills.Count() > 0)
            {
                model = model.Where(x => x.OfferLanguageDtos.Any(s => LanguageSkills.Contains(s.LanguageDto.Name) && s.LanguageLevel == (int)OfferLanguageType.Required));
            }
            if (!string.IsNullOrEmpty(countryName))
            {
                model = model.Where(x => x.CountryName == countryName);
            }
            if (updatedInTheLastDays < 0)
            {
                var filterDate = DateTime.Now.AddDays(updatedInTheLastDays);
                model = model.Where(x => x.LastUpdateDate >= filterDate);
            }
            if (WorkAreas.Count() > 0)
            {


                var workingFromHome = WorkAreas.Contains("IsOnSite") ? true : false;
                var partialWorkFromHome = WorkAreas.Contains("IsPartialRemote") ? true : false;
                var workingRegularWorkPlace = WorkAreas.Contains("IsOffSite") ? true : false;

                model = model.Where(x =>
                (x.IsOnSite ? workingFromHome : false)
                || (x.IsPartialRemote ? partialWorkFromHome : false)
                || (x.IsOffSite ? workingRegularWorkPlace : false)).AsQueryable();
            }

            List<OfferMobileModel> offerMobiles = new List<OfferMobileModel>();
            var isRemote = false;

            var modelFinal = model.ToList() ?? new List<OfferDto>();
            if (modelFinal != null && modelFinal.Count() > 0)
            {
                foreach (var item in modelFinal)
                {

                    if (item.IsOnSite || item.IsPartialRemote || item.IsOffSite)
                    {
                        isRemote = true;
                    }
                    var offerMobile = new OfferMobileModel()
                    {
                        Id = item.Id,
                        CompanyName = item.CompanyDto.Name ?? "",
                        JobTitle = item.JobTitle ?? "",
                        CompanyEmail = item.CompanyEmail ?? "",
                        Description = item.JobDescription ?? "",
                        City = item.CityName ?? "",
                        ExpireDate = item.ExprationDate,
                        IsRemote = isRemote,
                        DateOfOffer = item.CreateDate,
                        Logo = item.CompanyDto.Logo,
                        CompanyId = item.CompanyDto.Id,
                        MinSalary = item.BasicSalary,
                        MaxSalary = item.UpperLimit,
                        IsPartialRemote = item.IsPartialRemote,
                        IsOnSite = item.IsOnSite,
                        IsOffSite = item.IsOffSite,
                        knowledgeList = item.OfferKnowledgeDtos.Select(x => x.KnowledgeDto.Name).ToList(),
                        BenefitsList = item.OfferBenefitsDtos.Select(x => x.BenefitsDto.Image).ToList(),
                        JobCategoryNameList = item.OfferJobCategoryDtos.Select(x => x.JobCategoryDto.Jobcategory).ToList()
                    };
                    offerMobiles.Add(offerMobile);

                }
            }

            var count = offerMobiles.Count();
            var skip = Math.Min((pageNumber - 1) * pageSize, count - 1);
            var filterOfferMobile = offerMobiles.Skip(skip).Take(pageSize).ToList();

            var result = new PaginationDto<OfferMobileModel>
            {
                Data = filterOfferMobile,
                PageCount = (int)Math.Ceiling(((double)count / pageSize)),
                ItemsCount = count,
                Page = pageNumber
            };

            return result;
        }
        public decimal? ToMonthlySalary(decimal? basicSalary, string salaryType)
        {
            if (salaryType == SalaryPeriod.Day.ToString())
            {
                basicSalary = basicSalary * 22;
                return basicSalary;
            }
            if (salaryType == SalaryPeriod.Year.ToString())
            {
                basicSalary = basicSalary / 12;
                return basicSalary;
            }
            return basicSalary;
        }

        public decimal? ToCurrentCurrency(decimal? monthlySalary, string offerCurrency, string currentCurrency)
        {
            var offerCurrencyRate = currencyRepository.GetEntities().SingleOrDefault(x => x.Code == offerCurrency).CurrencyRate;
            var currentCurrencyRate = currencyRepository.GetEntities().SingleOrDefault(x => x.Code == currentCurrency).CurrencyRate;

            return (offerCurrencyRate * monthlySalary) / (currentCurrencyRate);
        }

        public async Task<List<CompanyDto>> GetCompanies()
        {
            var companies = await companyRepository.GetEntities().ToListAsync();
            var CompaniDtoList = mapper.Map<List<CompanyDto>>(companies);
            return CompaniDtoList;
        }
        public async Task<List<LanguageDto>> GetLanguages()
        {
            var languages = await languageRepository.GetEntities().ToListAsync();
            var languageDtoList = mapper.Map<List<LanguageDto>>(languages);
            return languageDtoList;
        }
        public async Task<List<BenefitsDto>> GetBenefits()
        {
            var benefits = await benefitsReposiotry.GetEntities().ToListAsync();
            var benefitsDtoList = mapper.Map<List<BenefitsDto>>(benefits);
            return benefitsDtoList;
        }
        public PaginationDto<OfferMobileModel> GetoffersByPagination(OfferFilter offersFilter = null)
        {
            throw new NotImplementedException();
        }



        public async Task<AppliedJobDto> GetMessageByApplierJobId(int appliedJobId)
        {
            var model = await applicantAppliedJobsRepository.GetEntities()
                .Include(x => x.AppliedJobMessages)
                .ThenInclude(x => x.AppliedJobMessageAttaches)
                .Include(x => x.AppliedJobDocuments)
                .ThenInclude(x => x.ApplicantDocument)
                .AsQueryable().FirstOrDefaultAsync(x => x.Id == appliedJobId);

            var appliedJobDtos = mapper.Map<AppliedJobDto>(model);
            appliedJobDtos.ApplicantId = applicantRepository.GetEntities().SingleOrDefault(x => x.Email.Equals(appliedJobDtos.ApplicantEmail)).Id;
            var offerDto = GetOffersList().FirstOrDefault(x => x.Id == appliedJobDtos.OfferId);

            appliedJobDtos.OfferMobileModel = GetOfferMobiles(offerDto);
            appliedJobDtos.OfferMobileModel.AppliedType = model.ApplicationStatus;
            appliedJobDtos.ApplicantDto = applicantService.GetApplicant(appliedJobDtos.ApplicantEmail);
            appliedJobDtos.AppliedType = model.ApplicationStatus;
            return appliedJobDtos;
        }
        public async Task<bool> AppliedTypeUpdate(int appliedId, ApplicationStatus status)
        {
            var applied = applicantAppliedJobsRepository.GetEntities().SingleOrDefault(x => x.Id == appliedId);
            if (applied == null) return false;

            applied.ApplicationStatus = status; 

            applicantAppliedJobsRepository.UpdateEntity(applied);
            await applicantAppliedJobsRepository.SaveChange();
            return true;
        }

        public List<OfferMobileModel> GetOfferMobilesList(List<OfferDto> offerDtos)
        {
            List<OfferMobileModel> offerMobiles = new List<OfferMobileModel>();
            var isRemote = false;

            foreach (var item in offerDtos)
            {
                if (item.IsOnSite || item.IsPartialRemote)
                {
                    isRemote = true;
                }
                var offerMobile = new OfferMobileModel()
                {
                    Id = item.Id,
                    CompanyId = item.CompanyDto.Id,
                    CompanyName = item.CompanyDto.Name ?? "",
                    JobTitle = item.JobTitle ?? "",
                    CompanyEmail = item.CompanyEmail ?? "",
                    Description = item.JobDescription ?? "",
                    City = item.CompanyDto.CityName ?? "",
                    ExpireDate = item.ExprationDate,
                    IsRemote = isRemote,
                    DateOfOffer = item.CreateDate,
                    Logo = item.CompanyDto.Logo,
                    IsPartialRemote = item.IsPartialRemote,
                    IsOnSite = item.IsOnSite,
                    IsOffSite = item.IsOffSite,
                    knowledgeList = item.OfferKnowledgeDtos.Select(x => x.KnowledgeDto.Name).ToList(),
                    BenefitsList = item.OfferBenefitsDtos.Select(x => x.BenefitsDto.Image).ToList(),
                    JobCategoryNameList = item.OfferJobCategoryDtos.Select(x => x.JobCategoryDto.Jobcategory).ToList(),
                    Applies = applicantAppliedJobsRepository.GetEntities().Where(x => x.OfferId == item.Id).Count(a => a.AppliedJobMessages.Any(m => m.AppliedJobId == a.Id && !m.IsDeleted)),
                    PaymentStatus = item.PaymentStatus,
                    Status = item.Status,
                    MinSalary = item.BasicSalary,
                    MaxSalary = item.UpperLimit
                };
                offerMobiles.Add(offerMobile);
            }
            return offerMobiles;
        }

        public OfferMobileModel GetOfferMobiles(OfferDto offerDto)
        {

            var isRemote = false;


            if (offerDto.IsOnSite || offerDto.IsPartialRemote)
            {
                isRemote = true;
            }
            var offerMobile = new OfferMobileModel()
            {
                Id = offerDto.Id,
                CompanyName = offerDto.CompanyDto.Name ?? "",
                CompanyId = offerDto.CompanyDto.Id,
                JobTitle = offerDto.JobTitle ?? "",
                CompanyEmail = offerDto.CompanyEmail ?? "",
                Description = offerDto.JobDescription ?? "",
                City = offerDto.CompanyDto.CityName ?? "",
                ExpireDate = offerDto.ExprationDate,
                IsRemote = isRemote,
                IsPartialRemote = offerDto.IsPartialRemote,
                IsOnSite = offerDto.IsOnSite,
                IsOffSite = offerDto.IsOffSite,
                DateOfOffer = offerDto.CreateDate,
                Logo = offerDto.CompanyDto.Logo,
                knowledgeList = offerDto.OfferKnowledgeDtos.Select(x => x.KnowledgeDto.Name).ToList(),
                BenefitsList = offerDto.OfferBenefitsDtos.Select(x => x.BenefitsDto.Image).ToList(),
                JobCategoryNameList = offerDto.OfferJobCategoryDtos.Select(x => x.JobCategoryDto.Jobcategory).ToList(),
                Status = offerDto.Status,
                MinSalary = offerDto.BasicSalary,
                MaxSalary = offerDto.UpperLimit
            };


            return offerMobile;
        }
        public async Task<bool> UpdateMessageIsSeenStatus(int appliedJobId)
        {
            var messages = appliedJobMessageRepository.GetEntities()
                                .Where(x => x.AppliedJobId == appliedJobId);

            if (messages.Any())
            {
                foreach (var message in messages)
                {
                    message.IsSeen = true;
                }

                await appliedJobMessageRepository.SaveChange();
                return true;
            }
            return false;
        }
        public async Task<bool> MarkAsUnRead(int id)
        {
            var lastMessage = appliedJobMessageRepository.GetEntities()
                                 .Where(x => x.AppliedJobId == id)
                                 .OrderByDescending(x => x.Id)
                                 .FirstOrDefault();

            if (lastMessage != null)
            {
                lastMessage.IsSeen = false;
                appliedJobMessageRepository.UpdateEntity(lastMessage);
                await appliedJobMessageRepository.SaveChange();
                return true;
            }
            return false;
        }
        public async Task<bool> DeleteMessage(int id)
        {

            var messagesToDelete = await appliedJobMessageRepository.GetEntities()
                .Where(x => x.AppliedJobId == id && !x.IsDeleted)
                .ToListAsync();

            if (messagesToDelete == null || messagesToDelete.Count == 0)
            {
                return false;
            }

            foreach (var message in messagesToDelete)
            {
                message.IsDeleted = true;
                appliedJobMessageRepository.UpdateEntity(message);
            }
            await appliedJobMessageRepository.SaveChange();

            return true;

        }
        public async Task<bool> ChangeJobType(Guid id, OfferStatus status)
        {
            var offer = await offerRepository.GetEntities().SingleOrDefaultAsync(x => x.Id == id);
            if (offer == null) return false;
            if (status == OfferStatus.ActiveJob)
                offer.Status = OfferStatus.InActiveJobs;
            if (status == OfferStatus.InActiveJobs)
                if (offer.ExprationDate >= DateTime.Now)
                {
                    offer.Status = OfferStatus.ActiveJob;
                }
                else
                {
                    return false;
                }
            offerRepository.UpdateEntity(offer);
            await offerRepository.SaveChange();
            return true;
        }

        public async Task<bool> ChangeThePaymentStatus(Guid offerId, bool status)
        {
            var result = await offerRepository.GetEntities().FirstOrDefaultAsync(x => x.Id == offerId);
            if (result is null)
                return false;
            result.PaymentStatus = status;
            await offerRepository.SaveChange();
            return true;
        }

        public async Task<OfferViewModel> GetPreviewItems(OfferDto offerDto)
        {
            List<OfferJobCategoryDto> offerJobCategoryDtos = new List<OfferJobCategoryDto>();
            List<OfferLanguageDto> OfferLanguageDtos = new List<OfferLanguageDto>();
            List<OfferKnowledgeDto> OfferKnowledgeDtos = new List<OfferKnowledgeDto>();
            List<OfferBenefitsDto> OfferBenefitsDto = new List<OfferBenefitsDto>();
            string baseUrl = configuration["GlobalSettings:ApiUrl"];
            var jobCategoryDtos = await Global.GetJobCategories(baseUrl);
            var knowledgeDtos = await Global.GetKnowledgs(baseUrl);
            var languageDtos = await Global.GetLanguages(baseUrl);
            var benefitsDtos = await Global.GetBenefits(baseUrl);

            //jobcategories
            if (offerDto.JobCategories is not null)
            {
                foreach (var item in offerDto.JobCategories)
                {
                    OfferJobCategoryDto offerJobCategoryDto = new OfferJobCategoryDto
                    {
                        JobCategoryDto = jobCategoryDtos.FirstOrDefault(x => x.Id == item),
                        JobCategoryId = item
                    };
                    offerJobCategoryDtos.Add(offerJobCategoryDto);
                }
            }

            //knowledgs
            if (offerDto.RequiredSeniorKnowledges is not null)
            {
                foreach (var item in offerDto.RequiredSeniorKnowledges)
                {
                    OfferKnowledgeDto offerKnowledgeDto = new OfferKnowledgeDto
                    {
                        KnowledgeDto = knowledgeDtos.FirstOrDefault(x => x.Id == item),
                        KnowledgeLevel = (int)OfferKnowledgeType.AddvantageOptionalKnowledge,
                        KnowledgeId = item

                    };
                    OfferKnowledgeDtos.Add(offerKnowledgeDto);
                }
            }
            if (offerDto.RequiredJuniorKnowledges is not null)
            {
                foreach (var item in offerDto.RequiredJuniorKnowledges)
                {
                    OfferKnowledgeDto offerKnowledgeDto = new OfferKnowledgeDto
                    {
                        KnowledgeDto = knowledgeDtos.FirstOrDefault(x => x.Id == item),
                        KnowledgeLevel = (int)OfferKnowledgeType.RequiredKnowledge,
                        KnowledgeId = item

                    };
                    OfferKnowledgeDtos.Add(offerKnowledgeDto);
                }
            }
            if (offerDto.Benefits is not null)
            {
                foreach (var item in offerDto.Benefits)
                {
                    OfferBenefitsDto offerBenefitsDto = new OfferBenefitsDto
                    {
                        BenefitsDto = benefitsDtos.FirstOrDefault(x => x.Image == item),
                        Image = item,
                    };
                    OfferBenefitsDto.Add(offerBenefitsDto);
                }
            }
            if (offerDto.OptionalKnowledges is not null)
            {
                foreach (var item in offerDto.OptionalKnowledges)
                {
                    OfferKnowledgeDto offerKnowledgeDto = new OfferKnowledgeDto
                    {
                        KnowledgeDto = knowledgeDtos.FirstOrDefault(x => x.Id == item),
                        KnowledgeLevel = (int)OfferKnowledgeType.AddvantageOptionalKnowledge,
                        KnowledgeId = item

                    };
                    OfferKnowledgeDtos.Add(offerKnowledgeDto);
                }
            }

            //languages
            if (offerDto.LanguageSkillRequireds is not null)
            {
                foreach (var item in offerDto.LanguageSkillRequireds)
                {
                    OfferLanguageDto offerLanguageDto = new OfferLanguageDto
                    {
                        LanguageDto = languageDtos.FirstOrDefault(x => x.Id == item),
                        LanguageLevel = (int)OfferLanguageType.Required,
                        LanguageId = item

                    };
                    OfferLanguageDtos.Add(offerLanguageDto);
                }
            }

            if (offerDto.LanguageSkillOptionals is not null)
            {
                foreach (var item in offerDto.LanguageSkillOptionals)
                {
                    OfferLanguageDto offerLanguageDto = new OfferLanguageDto
                    {
                        LanguageDto = languageDtos.FirstOrDefault(x => x.Id == item),
                        LanguageLevel = (int)OfferLanguageType.Optional,
                        LanguageId = item

                    };
                    OfferLanguageDtos.Add(offerLanguageDto);
                }
            }
            if (offerDto.LanguageProficiencyStatus != null)
            {
                foreach (var item in offerDto.LanguageProficiencyStatus)
                {
                    OfferLanguageDto offerLanguageDto = new OfferLanguageDto
                    {
                        LanguageDto = languageDtos.FirstOrDefault(x => x.Id == (int)item),
                        LanguageLevel = (int)item,
                        LanguageId = (int)item
                    };
                    OfferLanguageDtos.Add(offerLanguageDto);
                }
            }

            OfferViewModel offerViewModel = new OfferViewModel();

            OfferDto offerDtoNew = new OfferDto();
            offerDtoNew = offerDto;
            offerDtoNew.OfferJobCategoryDtos = offerJobCategoryDtos;
            offerDtoNew.OfferLanguageDtos = OfferLanguageDtos;
            offerDtoNew.OfferKnowledgeDtos = OfferKnowledgeDtos;
            offerDtoNew.OfferBenefitsDtos = OfferBenefitsDto;

            offerViewModel.offerDto = offerDtoNew;

            return offerViewModel;
        }

        public async Task<ResultDto<FileApplicantDocumentDto>> GetApplicantDocument(int fileId)
        {
            var documentFile = applicantDocumentRepository.GetEntities().FirstOrDefault(x => x.Id == fileId);
            var applicant = profileRepository.GetEntities().FirstOrDefault(x => x.Email == documentFile.ApplicantEmail);

            if (documentFile is null)
                return new ResultDto<FileApplicantDocumentDto>(null, MessageCodes.BadRequest);

            var byteFile = fileManagerService.ConvertFileTobyte(new List<string>
        {
            configuration["FileManager:DirectoryFileApplicant"],
            applicant.Id.ToString(),
            documentFile.Name
        });

            if (byteFile.MessageCode is not MessageCodes.Success)
                return new ResultDto<FileApplicantDocumentDto>(null, MessageCodes.BadRequest);

            return new ResultDto<FileApplicantDocumentDto>(byteFile.Data, MessageCodes.Success);
        }
        public async Task<ResultDto<FileApplicantDocumentDto>> GetCompanyDocumentFile(int fileId)
        {
            var attachFile = appliedJobMessageAttachRepository.GetEntities().FirstOrDefault(x => x.Id == fileId);
            var appliedJobMessage = appliedJobMessageRepository.GetEntities().FirstOrDefault(x => x.Id == attachFile.AppliedJobMessageId);
            var appliedJob = applicantAppliedJobsRepository.GetEntities().FirstOrDefault(x => x.Id == appliedJobMessage.AppliedJobId);
            var applicant = profileRepository.GetEntities().FirstOrDefault(x => x.Email == appliedJob.ApplicantEmail);

            if (applicant is null)
                return new ResultDto<FileApplicantDocumentDto>(null, MessageCodes.BadRequest);

            var byteFile = fileManagerService.ConvertFileTobyte(new List<string>
        {
            configuration["FileManager:DirectoryAppliedJobMessageAttach"],
            applicant.Id.ToString(),
            attachFile.Name
        });

            if (byteFile.MessageCode is not MessageCodes.Success)
                return new ResultDto<FileApplicantDocumentDto>(null, MessageCodes.BadRequest);

            return new ResultDto<FileApplicantDocumentDto>(byteFile.Data, MessageCodes.Success);
        }

        public async Task<ResultDto<List<OfferDto>>> GetOfferByCompanyEmail(string companyEmail)
        {
            var offerDtoList = GetOffersList().Where(x => x.CompanyEmail.Equals(companyEmail) && x.PaymentStatus == true).ToList();
            return new ResultDto<List<OfferDto>>(offerDtoList);
        }

        public async Task<ResultDto<int>> SaveRequestCompany(Guid id, string applicantEmail)
        {
            var existingJob = applicantAppliedJobsRepository.GetEntities()
                .FirstOrDefault(j => j.OfferId == id && j.ApplicantEmail == applicantEmail);

            if (existingJob != null)
            {

                return new ResultDto<int>(0, MessageCodes.BadRequest);

            }
            var appliedJob = new AppliedJob
            {
                OfferId = id,
                ApplicantEmail = applicantEmail,
                IsCompanyRequest = true,
                ApplicationStatus = ApplicationStatus.UnderReview,
            };

            await applicantAppliedJobsRepository.AddEntity(appliedJob);
            await applicantAppliedJobsRepository.SaveChange();

            return new ResultDto<int>(appliedJob.Id, MessageCodes.Success);
        }
        #region UpdateOffer
        public async Task<JobOfferViewModel> GetJobOfferViewModelForUpdate(Guid offerId, string email)
        {
            var offerDto = await GetOfferDtoById(offerId);
            var companyDto = await GetCompanyDtoByOfferId(email);
            var allKnowledgeDtos = await GetAllKnowledgeDtos();
            var seniorKnowledgeDtos = await GetKnowledgeDtosByOfferIdAndLevel(offerId, OfferKnowledgeType.RequiredKnowledge);
            var juniorKnowledgeDtos = await GetKnowledgeDtosByOfferIdAndLevel(offerId, OfferKnowledgeType.AddvantageRequiredKnowledge);
            var optionalKnowledgeDtos = await GetKnowledgeDtosByOfferIdAndLevel(offerId, OfferKnowledgeType.AddvantageOptionalKnowledge);
            var currencyDtos = await GetCurrencyDtos();
            var languageDtos = await GetAllLanguageDtos();
            var selectedRequiredLanguages = await GetRequiredLanguagesByOfferId(offerId);
            var selectedOptionalLanguages = await GetOptionalLanguagesByOfferId(offerId);
            var benefitsDtos = await GetBenefitsDtosByOfferId(offerId);
            var allBenefitsDtos = await GetAllBenefits();
            var packageDtos = await GetPackageDtos();
            var jobCategoryDtos = await GetJobCategoriesByOfferId(offerId);
            var viewModel = new JobOfferViewModel
            {
                OfferDto = offerDto,
                CompanyDto = companyDto,
                AllKnowledgeDtos = allKnowledgeDtos,
                RequiredSeniorKnowledges = seniorKnowledgeDtos,
                RequiredJuniorKnowledges = juniorKnowledgeDtos,
                OptionalKnowledges = optionalKnowledgeDtos,
                CurrencyDtos = currencyDtos,
                LanguageDtos = languageDtos,
                AllBenefits = allBenefitsDtos,
                SelectedBenefits = benefitsDtos,
                PackageDtos = packageDtos,
                SelectedJobCategories = jobCategoryDtos.Select(jc => jc.Id).ToList()
            };

            return viewModel;
        }
        public async Task<OfferDto> GetOfferDtoById(Guid offerId)
        {
            var offer = await offerRepository.GetEntities().Include(o => o.Company)
                                .FirstOrDefaultAsync(o => o.Id == offerId);

            return mapper.Map<OfferDto>(offer);
        }

        public async Task<List<KnowledgeDto>> GetAllKnowledgeDtos()
        {
            var allKnowledge = await knowledgeRepository.GetEntities()
                                .Select(k => mapper.Map<KnowledgeDto>(k))
                                .ToListAsync();

            return allKnowledge;
        }

        public async Task<List<KnowledgeDto>> GetKnowledgeDtosByOfferIdAndLevel(Guid offerId, OfferKnowledgeType level)
        {
            var knowledgeDtos = await offerKnowledgeRepository.GetEntities()
                                    .Where(ok => ok.OfferId == offerId && ok.KnowledgeLevel == (int)level)
                                    .Select(ok => new KnowledgeDto
                                    {
                                        Id = ok.Knowledge.Id,
                                        Name = ok.Knowledge.Name,
                                    })
                                    .ToListAsync();

            return knowledgeDtos;
        }
        public async Task<List<CurrencyDto>> GetCurrencyDtos()
        {
            var currencyDtos = await currencyRepository.GetEntities()
                                    .Select(c => mapper.Map<CurrencyDto>(c))
                                    .ToListAsync();

            return currencyDtos;
        }

        public async Task<List<LanguageDto>> GetAllLanguageDtos()
        {
            return await languageRepository.GetEntities()
                                           .Select(l => new LanguageDto { Id = l.Id, Name = l.Name })
                                           .ToListAsync();
        }

        public async Task<List<LanguageDto>> GetRequiredLanguagesByOfferId(Guid offerId)
        {
            return await offerLanguageRepository.GetEntities()
                                                .Where(ol => ol.OfferId == offerId && ol.LanguageLevel == OfferLanguageType.Required)
                                                .Select(ol => new LanguageDto { Id = ol.LanguageId, Name = ol.Language.Name })
                                                .ToListAsync();
        }

        public async Task<List<LanguageDto>> GetOptionalLanguagesByOfferId(Guid offerId)
        {
            return await offerLanguageRepository.GetEntities()
                                                .Where(ol => ol.OfferId == offerId && ol.LanguageLevel == OfferLanguageType.Optional)
                                                .Select(ol => new LanguageDto { Id = ol.LanguageId, Name = ol.Language.Name })
                                                .ToListAsync();
        }
        public async Task<List<BenefitsDto>> GetAllBenefits()
        {
            var allBenefits = await benefitsRepository.GetEntities()
                                 .Select(b => mapper.Map<BenefitsDto>(b))
                                 .ToListAsync();

            return allBenefits;
        }

        public async Task<List<BenefitsDto>> GetBenefitsDtosByOfferId(Guid offerId)
        {
            var offerBenefits = await offerBenefitsRepository.GetEntities()
                                     .Where(ob => ob.OfferId == offerId)
                                     .Include(ob => ob.Benefits)
                                     .ToListAsync();

            var benefitsDtos = offerBenefits.Select(ob => mapper.Map<BenefitsDto>(ob.Benefits)).ToList();

            return benefitsDtos;
        }
        public async Task<List<JobCategoryDto>> GetAllJobCategories()
        {
            var jobCategories = await jobCategoryRepository.GetEntities()
                                      .Select(jc => new JobCategoryDto
                                      {
                                          Id = jc.Id,
                                          Jobcategory = jc.Jobcategory
                                      })
                                      .ToListAsync();

            return jobCategories;
        }

        public async Task<List<JobCategoryDto>> GetJobCategoriesByOfferId(Guid offerId)
        {
            var offerJobCategories = await offerJobCategoryRepository.GetEntities()
                                        .Where(ojc => ojc.OfferId == offerId)
                                        .Include(ojc => ojc.JobCategory)
                                        .ToListAsync();

            var jobCategoryDtos = offerJobCategories.Select(ojc => new JobCategoryDto
            {
                Id = ojc.JobCategory.Id,
                Jobcategory = ojc.JobCategory.Jobcategory
            }).ToList();

            return jobCategoryDtos;
        }
        public async Task<List<PackageDto>> GetPackageDtos()
        {
            var packages = await packageRepository.GetEntities()
                                    .Select(p => mapper.Map<PackageDto>(p))
                                    .ToListAsync();

            return packages;
        }
        public async Task<CompanyDto> GetCompanyDtoByOfferId(string email)
        {
            var companyDto = await companyRepository.GetEntities()
                                    .Where(o => o.Email == email)
                                    .Select(o => mapper.Map<CompanyDto>(o))
                                    .FirstOrDefaultAsync();

            return companyDto;
        }
        #endregion
        #region CompanyFavourit
        private async Task<bool> IsApplicantExistToCompanyFavouriteList(CompanyApplicantFavouriteDto companyApplicantFavouriteDto)
        {
            var offerFavourite = await companyApplicantFavouriteRepository.GetEntities().SingleOrDefaultAsync(x =>
                (x.CompanyEmail == companyApplicantFavouriteDto.CompanyEmail && x.ApplicantEmail == companyApplicantFavouriteDto.ApplicantEmail));

            if (offerFavourite == null)
                return true;

            return false;
        }
        public async Task<ApplicantOffersFavouriteResult> SaveCompanyApplicnatFavourite(CompanyApplicantFavouriteDto companyApplicantFavouriteDto)
        {
            if (companyApplicantFavouriteDto.CompanyEmail == null) return ApplicantOffersFavouriteResult.Badrequest;
            if (companyApplicantFavouriteDto.ApplicantEmail == null) return ApplicantOffersFavouriteResult.Badrequest;
            if (await IsApplicantExistToCompanyFavouriteList(companyApplicantFavouriteDto) == false) return ApplicantOffersFavouriteResult.AddedBefore;
            var applicantOfferFavourite = mapper.Map<CompanyApplicantFavourite>(companyApplicantFavouriteDto);

            await companyApplicantFavouriteRepository.AddEntity(applicantOfferFavourite);
            await companyApplicantFavouriteRepository.SaveChange();
            return ApplicantOffersFavouriteResult.Success;
        }
        public async Task<bool> ReturnToConversation(Guid offerId, int applicantId)
        {
            var applicantEmail = profileRepository.GetEntities().FirstOrDefault(x => x.Id == applicantId);
            var applicants = applicantAppliedJobsRepository.GetEntities().FirstOrDefault(x => x.ApplicantEmail == applicantEmail.Email && x.OfferId == offerId);
            var returnMessage = appliedJobMessageRepository.GetEntities().Where(x => x.AppliedJobId == applicants.Id);

            if (!returnMessage.Any())
                return false;
            foreach (var applicant in returnMessage)
            {
                applicant.IsDeleted = false;
                appliedJobMessageRepository.UpdateEntity(applicant);
            }
            await appliedJobMessageRepository.SaveChange();
            return true;
        }
        public async Task<bool> DeleteFavouriteApplicant(string applicantEmail, string companyEmail)
        {
            var applicantFavoutiteOffer = await companyApplicantFavouriteRepository.GetEntities().SingleOrDefaultAsync(x => x.CompanyEmail.Equals(companyEmail) && x.ApplicantEmail == applicantEmail);
            if (applicantFavoutiteOffer == null) return false;
            try
            {
                companyApplicantFavouriteRepository.DeleteEntity(applicantFavoutiteOffer);
                await companyApplicantFavouriteRepository.SaveChange();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private IQueryable<Applicant> GetApplicantsQuery(string companyEmail)
        {
            var applicantsQuery = profileRepository.GetEntities()
                .Include(a => a.ApplicantEducations)
                .Include(a => a.ApplicantKnowledges)
                .Include(a => a.ApplicantLanguages)
                .Include(a => a.ApplicantWorkExperiences)
                .Include(a => a.AppliedJobs)
                 .ThenInclude(j => j.Offer)
                .Include(a => a.ApplicantDocuments.Where(d => !d.IsDeleted))
                .Include(a => a.CompanyApplicantFavourites)
                .Where(a => a.CompanyApplicantFavourites.Any(c => c.CompanyEmail == companyEmail) && a.AppliedJobs.Any(j => j.Offer.CompanyEmail == companyEmail))
                .AsNoTracking();

            return applicantsQuery;
        }
        public PaginationDto<ApplicantDto> CompanyFilter(ApplicantFilterDto applicantFilterDto, string companyEmail)
        {
            var model = GetApplicantsQuery(companyEmail);
            var pageSize = 5;

            if (applicantFilterDto.IsFreelancer || applicantFilterDto.IsEuropeanUnion || applicantFilterDto.IsFullTime ||
                applicantFilterDto.IsInternShip || applicantFilterDto.IsPartialworkfromhome || applicantFilterDto.IsPartTime ||
                applicantFilterDto.IsSwitzerland || applicantFilterDto.IsUnitedStatesofAmerica || applicantFilterDto.IsWorkFromHome
                || applicantFilterDto.IsWorkPlace)
            {
                model = model.Where(x =>
                    (x.IsFreelancer == applicantFilterDto.IsFreelancer) ||
                    (x.IsEuropeanUnion == applicantFilterDto.IsEuropeanUnion) ||
                    (x.IsFullTime == applicantFilterDto.IsFullTime) ||
                    (x.IsInternShip == applicantFilterDto.IsInternShip) ||
                    (x.IsPartialRemote == applicantFilterDto.IsPartialworkfromhome) ||
                    (x.IsPartTime == applicantFilterDto.IsPartTime) ||
                    (x.IsSwitzerland == applicantFilterDto.IsSwitzerland) ||
                    (x.IsUnitedStatesofAmerica == applicantFilterDto.IsUnitedStatesofAmerica) ||
                    (x.IsOffSite == applicantFilterDto.IsWorkFromHome) ||
                    (x.IsOnSite == applicantFilterDto.IsWorkPlace));
            }

            if (!string.IsNullOrWhiteSpace(applicantFilterDto.Language))
            {
                model = model.Where(x => x.ApplicantLanguages.Any(l => l.LanguageName == applicantFilterDto.Language));
            }

            if (!string.IsNullOrWhiteSpace(applicantFilterDto.Knowledge))
            {
                model = model.Where(x => x.ApplicantKnowledges.Any(k => k.KnowledgeName == applicantFilterDto.Knowledge));
            }

            var count = model.Count();
            var currentPage = int.Parse(applicantFilterDto.CurrentPage);
            var skip = Math.Max((currentPage - 1) * pageSize, 0);
            var filterModel = model.Skip(skip).Take(pageSize).ToList();

            var result = new PaginationDto<ApplicantDto>
            {
                Data = mapper.Map<List<ApplicantDto>>(filterModel),
                PageCount = (int)Math.Ceiling((double)count / pageSize),
                ItemsCount = count,
                Page = currentPage
            };

            return result;

        }
        private IQueryable<Applicant> GetApplicantsIsDelete(string companyEmail)
        {
            var applicantsQuery = profileRepository.GetEntities()
                  .Include(a => a.ApplicantEducations)
                  .Include(a => a.ApplicantKnowledges)
                  .Include(a => a.ApplicantLanguages)
                  .Include(a => a.ApplicantWorkExperiences)
                  .Include(a => a.ApplicantDocuments.Where(d => !d.IsDeleted))
                  .Include(a => a.AppliedJobs)
                    .ThenInclude(j => j.Offer)
                  .Include(a => a.AppliedJobs)
                    .ThenInclude(a => a.AppliedJobMessages).Where(a => a.AppliedJobs.Any(j => j.Offer.CompanyEmail == companyEmail && j.AppliedJobMessages.Any(m => m.IsDeleted)))
                  .AsNoTracking();

            return applicantsQuery;
        }
        public PaginationDto<ApplicantDto> ApplicantTrash(ApplicantFilterDto applicantFilterDto, string companyEmail)
        {
            var model = GetApplicantsIsDelete(companyEmail)
                            .Include(a => a.AppliedJobs)
                            .ThenInclude(aj => aj.AppliedJobMessages)
                            .AsQueryable();

            var pageSize = 5;

            if (applicantFilterDto.IsFreelancer || applicantFilterDto.IsEuropeanUnion || applicantFilterDto.IsFullTime ||
                applicantFilterDto.IsInternShip || applicantFilterDto.IsPartialworkfromhome || applicantFilterDto.IsPartTime ||
                applicantFilterDto.IsSwitzerland || applicantFilterDto.IsUnitedStatesofAmerica || applicantFilterDto.IsWorkFromHome ||
                applicantFilterDto.IsWorkPlace)
            {
                model = model.Where(x => ((bool)x.IsFreelancer ? applicantFilterDto.IsFreelancer : false) ||
                   ((bool)x.IsEuropeanUnion == true ? applicantFilterDto.IsEuropeanUnion : false) ||
                   ((bool)x.IsFullTime == true ? applicantFilterDto.IsFullTime : false) ||
                   ((bool)x.IsInternShip == true ? applicantFilterDto.IsInternShip : false) ||
                   ((bool)x.IsPartialRemote == true ? applicantFilterDto.IsPartialworkfromhome : false) ||
                   ((bool)x.IsPartTime == true ? applicantFilterDto.IsPartTime : false) ||
                   ((bool)x.IsSwitzerland == true ? applicantFilterDto.IsSwitzerland : false) ||
                   ((bool)x.IsUnitedStatesofAmerica == true ? applicantFilterDto.IsUnitedStatesofAmerica : false) ||
                   ((bool)x.IsOffSite == true ? applicantFilterDto.IsWorkFromHome : false) ||
                   ((bool)x.IsOnSite == true ? applicantFilterDto.IsWorkPlace : false)).AsQueryable();
            }

            if (!string.IsNullOrWhiteSpace(applicantFilterDto.Language))
            {
                model = model.Where(x => x.ApplicantLanguages.Any(l => l.LanguageName == applicantFilterDto.Language));
            }

            if (!string.IsNullOrWhiteSpace(applicantFilterDto.Knowledge))
            {
                model = model.Where(x => x.ApplicantKnowledges.Any(k => k.KnowledgeName == applicantFilterDto.Knowledge));
            }

            var count = model.ToList().Count();
            var skip = Math.Min((int.Parse(applicantFilterDto.CurrentPage) - 1) * pageSize, count - 1);
            var filterModel = model.ToList().Skip(skip).Take(pageSize).ToList();

            var applicantDtos = mapper.Map<List<ApplicantDto>>(filterModel);
            foreach (var applicantDto in applicantDtos)
            {
                var applicant = filterModel.FirstOrDefault(a => a.Id == applicantDto.Id);
                if (applicant != null)
                {
                    applicantDto.AppliedJobMessage = applicant.AppliedJobs
                        .Where(aj => aj.Offer.CompanyEmail == companyEmail)
                        .SelectMany(aj => aj.AppliedJobMessages ?? new List<AppliedJobMessage>())
                        .Select(message => new AppliedJobMessageDto
                        {
                            AppliedJobId = message.AppliedJobId,
                            OfferId = message.AppliedJob?.OfferId ?? Guid.Empty,
                            Message = message.Message,
                            ApplicantEmail = message.AppliedJob?.ApplicantEmail,
                            ClientType = message.ClientType,
                            IsSeen = message.IsSeen,
                            IsDeleted = message.IsDeleted,
                            CreateDate = message.CreateDate,
                        }).ToList();
                }
            }

            var result = new PaginationDto<ApplicantDto>
            {
                Data = applicantDtos,
                PageCount = (int)Math.Ceiling((double)count / pageSize),
                ItemsCount = count,
                Page = int.Parse(applicantFilterDto.CurrentPage)
            };

            return result;
        }

        #endregion
    }

}

