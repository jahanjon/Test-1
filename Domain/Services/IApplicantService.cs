using DotNek.Common.Dtos;
using FindJobs.Domain.Dtos;
using FindJobs.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FindJobs.Domain.Services
{
    public interface IApplicantService
    {
        #region Claims
        bool GetApplicantUserClaims(ClaimsPrincipal user);
        #endregion
        #region Applicants
        ApplicantDto GetApplicant(string applicantEmail);
        ApplicantDto GetApplicant(int id);
        PaginationDto<ApplicantDto> GetApplicants(int currentPage, string key, string location);

        PaginationDto<ApplicantDto> ApplicantsFilter(ApplicantFilterDto applicantFilterDto);
        int GetApplicantsCount(string key, string location);

        #endregion
        #region Applicant Privacy
        Task<bool> UpdatePrivacy(ApplicantPrivacyDto model);
        #endregion
        #region Applicant Image
        Task<bool> UpdateApplicantImage(string imageFileName, string imageName, string applicantEmail);
        Task<bool> IsApplicantExist(string email);
        Task<bool> DeleteApplicantImage(string applicantEmail);
        #endregion
        #region ApplicantDocuments
        Task<ApplicantDocumentDto> GetApplicantDocument(int id);
        Task<ApplicantDocumentDto> GetApplicantDocument(int fileId, string email);
        List<ApplicantDocumentDto> GetApplicantDocumentsByEmail(string email);
        Task<List<ApplicantDocumentDto>> GetRemovedApplicantDocumentsByEmail(string email);
        Task<ResultDto<FileApplicantDocumentDto>> GetApplicantDocumentFile(int fileId);
        Task<bool> InsertApplicantDocument(ApplicantDocumentDto applicantDocumentsDto);
        Task<bool> UpdateApplicantDocument(ApplicantDocumentDto applicantDocumentsDto);
        Task<bool> RestoreApplicantDocument(int id);
        Task<bool> RemoveApplicantDocument(int id);
        Task<bool> DeleteApplicantDocument(int id);
        Task<bool> SetActiveDocument(int documentId, string applicantEmail);
        List<ApplicantDocumentDto> GetApplicantDocumentsByEmailAndType(string email, UploadDocumentType type);
        #endregion
        #region WorkExperience
        Task<bool> InsertOrUpdateWorkExperiance(ApplicantWorkExperienceDto workExperienceDto);
        List<ApplicantWorkExperienceDto> GetAllApplicantWorkExperienceByEmail(string email);
        Task<ApplicantWorkExperienceDto> GetWorkExperienceById(int id);
        Task<bool> DeleteApplicantWorkExperience(int id);
        #endregion
        #region ApplicantEducation
        Task<bool> CreateOrUpdateEducation(ApplicantEducationDto educationDto);
        List<ApplicantEducationDto> GetAllApplicantEducation(string email);
        Task<ApplicantEducationDto> GetApplicantEducationById(int id);
        Task<bool> DeleteApplicantEducation(int id);
        #endregion
        #region knowledge
        Task<bool> CreateOrUpdateKhnowledge(ApplicantKnowledgeDto knowledgeDto);
        List<ApplicantKnowledgeDto> GetAllApplicantKnowledge(string email);
        Task<ApplicantKnowledgeDto> GetApplicantKnowledgeById(int id);
        Task<bool> DeleteApplicantKnowledge(int id);
        List<KnowledgeDto> GetKnowledgeList();
        #endregion
        #region Language
        Task<bool> CreateOrUpdateLanguage(ApplicantLanguageDto languageDto);
        List<ApplicantLanguageDto> GetAllApplicantLanguage(string email);
        Task<ApplicantLanguageDto> GetApplicantLanguageById(int id);
        Task<bool> DeleteApplicantLanguage(int id);
        List<LanguageDto> GetLanguageList();

        #endregion
        #region Personal Information
        ApplicantDto GetPersonalInformation(string email);
        Task<bool> SavePersonalInformation(ApplicantProfileDto personalInformation);
        #endregion
        #region ContactDetail
        ApplicantDto GetApplicantContactDetail(string email);
        Task<bool> SaveApplicantContactDetail(ApplicantContactDetailsDto applicantContactDetailsDto, string email);
        #endregion
        #region Additional Section
        ApplicantDto GetApplicantAdditionalSection(string email);
        Task<bool> SaveApplicantAdditionalSection(ApplicantAddtionalSectionDto addtionalSectionDto);
        #endregion
        #region Email Preferences
        Task<ApplicantPrivacyDto> GetApplicantSettingsAsync(string email);
        bool UnsubscribeEmailPreferences(string email);
        List<ApplicantPreferenceDto> GetEmailPreferences(string email);
        #endregion
        #region Offers
        List<OfferDto> GetOffers();
        PaginationDto<OfferDto> SearchOffersAjax(string location, int currentPage, string language, string workPlace,
            string jobOffer, string employer, string position, string company, List<string> jobCategories = null);
        PaginationDto<OfferDto> SearchOffersAjax();
        Task<ApplicantOffersFavouriteResult> SaveApplicantOfferFavourite(ApplicantOfferFavouriteDto applicantOffersFavouriteDto);
        List<OfferDto> GetAllFavourteApplicantOffers(string applicantEmail);
        Task<bool> DeleteFavouriteOffer(Guid id, string applicantEmail);
        int GetCountFavouritApplicant(string applicantEmail);
        List<ApplicantOfferFavouriteDto> applicantOffersFavouriteDtos(string applicantEmail);
        Task<ApplicantOfferFavouriteDto> GetApplicantOfferFavouriteDtos(string applcantEmail, Guid id);
        Task<PaginationDto<OfferMobileModel>> GetAllFavourteApplicantOffersWithPageing(string applicantEmail, int currentPage, string company);
        Task<PaginationDto<OfferMobileModel>> GetAllDeleteMessage(string applicantEmail, int currentPage, string company);
        #endregion

        #region ApplicantAppliedJobs
        Task<bool> Apply(AppliedJobDto applicantAppliedJobsDto);
        Task<AppliedJobsViewModelDto> GetAllAppliedJobsAsync(string applicantEmail, int currentPage);
        AppliedJobDto GetAppliedJob(string applicantEmail, Guid offerId);
        Task<ResultDto<bool>> IsOfferExistFavouriteList(ApplicantOfferFavouriteDto applicantOffersFavouriteDto);
        Task<PaginationDto<ApplicantAppliedJobDto>> GetApplicantAppliers(Guid offerId, string companyEmail, ApplicationStatus appliedType, int currentPage);

        Task<bool> SaveMessage(AppliedJobMessageDto appliedJobMessageDto);
        Task<bool> UpdateMessageIsSeenStatus(int id);
        Task<bool> MarkAsUnRead(int id);
        Task<ResultDto<MessageCodes>> DeleteMessage(int id);
        Task<bool> ReturnToConverstion(int appliedJobId);

        #endregion

        #region prefrences
        Task<List<ApplicantPreferenceDto>> GetApplicantPreferences(string applicantEmail);

        #endregion

        Task<int> CheckCVParser(string emailApplicant);

        Task<bool> UpdateVerifyStatusApplicant(VerifyStatusApplicantProfileDto status);

        #region ApplicantBlacklistOfCompany
        Task<ResultDto<bool>> UpdateApplicantBlacklistOfCompany(List<string> companyEmails, string emailApplicant);
        Task<PaginationDto<OfferMobileModel>> GetAllBlackListOfCompanyWithPageingAsync(string applicantEmail, int currentPage, string company);
        Task<ApplicantBlacklistOfCompanyDto> GetApplicantBlacklistOfCompanyDtos(string applcantEmail, Guid id);
        List<ApplicantBlacklistOfCompanyDto> applicantBlacklistOfCompanyDtos(string applicantEmail);
        int GetCountBlackLisOfCompany(string applicantEmail);
        List<OfferDto> GetAllBlackListOfCompany(string applicantEmail);
        Task<ApplicantBlacklistOfCompanyResult> SaveApplicantBlacklistOfCompany(ApplicantBlacklistOfCompanyDto applicantBlacklistOfCompanyDto);
        Task<bool> DeleteBlackLisCompany(Guid offerId, string applicantEmail);
        List<CompanyDto> GetAllCompanies();
        #endregion
    }
}
