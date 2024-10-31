using DotNek.Common.Dtos;
using FindJobs.Domain.Dtos;
using FindJobs.Domain.Enums;
using FindJobs.Domain.ViewModels;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FindJobs.Domain.Services
{
    public interface ICompanyService
    {
        Task<EmailSendResult> SendOfferEmails(string applicantEmail,Guid id);
        Task<OfferDto> GetOfferById(Guid id);
        Task<EmailSendResult> SendInvoiceBeforePayment(OfferDto offerDto);
        Task<List<OfferDto>> GetOffers();
        Task<List<CompanyDto>> GetTopEmployers();
        Task<bool> UpdateCompany(CompanyDto companyDto);
        bool GetCompanyRole(ClaimsPrincipal user);
        Task<JobOfferViewModel> GetJobOfferViewModel(string email);
        Task<JobOfferViewModel> GetJobOfferViewModelForUpdate(Guid offerId,string email);
        Task<Guid> SaveOrUpdateJobOffer(OfferDto offerDto);
        Task<CompanyDto> GetCompanyByEmail(string email);
        Task<string> GetEmailCompanyById(int id);
        Task<CompanyDto> GetCompanyById(int id);
        PaginationDto<OfferMobileModel> GetOffersByCompanyEmail(string companyEmail, int currentPage = 1, OfferStatus status = OfferStatus.All, string offerName = "");
        Task<ResultDto<List<OfferDto>>> GetOfferByCompanyEmail(string companyEmail);
        Task<PaginationDto<OfferMobileModel>> GetoffersFiltersAsync(List<string> jobCategories, List<string> EmployeeTypes, List<string> WorkAreas, List<string> LanguageSkills, int pageNumber = 1, int pageSize = 5, int? minSalary = null, int? maxSalary = null, string keyword = "", string location = "", string currency = "", string countryName = "", int updatedInTheLastDays = 0);
        Task<List<CompanyDto>> GetCompanies();
        Task<ResultDto<int>> SaveRequestCompany(Guid id, string applicantEmail);
        Task<ApplicantOffersFavouriteResult> SaveCompanyApplicnatFavourite(CompanyApplicantFavouriteDto companyApplicantFavouriteDto);
        PaginationDto<ApplicantDto> CompanyFilter(ApplicantFilterDto applicantFilterDto, string companyEmail);
        PaginationDto<ApplicantDto> ApplicantTrash(ApplicantFilterDto applicantFilterDto,string companyEmail);
        Task<bool> DeleteFavouriteApplicant(string applicantEmail, string companyEmail);
        Task<List<LanguageDto>> GetLanguages();
        Task<List<BenefitsDto>> GetBenefits();
        Task<bool> ReturnToConversation(Guid offerId,int applicantId);
        Task<AppliedJobDto> GetMessageByApplierJobId(int applierJobId);
        Task<bool> AppliedTypeUpdate(int appliedId, ApplicationStatus status);
        Task<bool> ChangeJobType(Guid id, OfferStatus type);
        Task<bool> ChangeThePaymentStatus(Guid offerId, bool status);
        Task<OfferViewModel> GetPreviewItems(OfferDto offerDto);
        Task<bool> UpdateMessageIsSeenStatus(int id);
        Task<bool> MarkAsUnRead(int id);
        Task<bool> DeleteMessage(int id);
        Task<ResultDto<FileApplicantDocumentDto>> GetApplicantDocument(int fileId);
        Task<ResultDto<FileApplicantDocumentDto>> GetCompanyDocumentFile(int fileId);
        Task<List<KnowledgeDto>> GetKnowledgeDtosByOfferIdAndLevel(Guid offerId, OfferKnowledgeType level);
        Task<List<KnowledgeDto>> GetAllKnowledgeDtos();
        Task<List<CurrencyDto>> GetCurrencyDtos();
        Task<List<LanguageDto>> GetAllLanguageDtos();
        Task<List<LanguageDto>> GetRequiredLanguagesByOfferId(Guid offerId);
        Task<List<LanguageDto>> GetOptionalLanguagesByOfferId(Guid offerId);
        Task<List<BenefitsDto>> GetBenefitsDtosByOfferId(Guid offerId);
        Task<OfferDto> GetOfferDtoById(Guid offerId);
        Task<List<JobCategoryDto>> GetJobCategoriesByOfferId(Guid offerId);
        Task<CompanyDto> GetCompanyDtoByOfferId(string email);
        Task<List<JobCategoryDto>> GetAllJobCategories();
        Task<List<BenefitsDto>> GetAllBenefits();
        Task<OfferDto> GetCurrencyByOfferId(Guid id, string currencyCodeUser);

    }
}
