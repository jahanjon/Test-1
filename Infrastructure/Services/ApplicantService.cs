using AutoMapper;
using DotNek.Common.Dtos;
using FindJobs.DataAccess.Entities;
using FindJobs.Domain.Dtos;
using FindJobs.Domain.Enums;
using FindJobs.Domain.Repositories;
using FindJobs.Domain.Services;
using iText.Layout.Element;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FindJobs.Infrastructure.Services
{
    public class ApplicantService : IApplicantService
    {

        private readonly IGenericRepository<ApplicantDocument> applicantDocumentRepository;
        private readonly IGenericRepository<ApplicantWorkExperience> workExperienceRepository;
        private readonly IGenericRepository<ApplicantEducation> educationRepository;
        private readonly IGenericRepository<ApplicantKnowledge> applicantknowledgeRepository;
        private readonly IGenericRepository<ApplicantLanguage> applicantlanguageRepository;
        private readonly IGenericRepository<Applicant> profileRepository;
        private readonly IGenericRepository<Language> languageRepository;
        private readonly IGenericRepository<Knowledge> knowledgeRepository;
        private readonly IGenericRepository<Offer> offerRepository;
        private readonly IGenericRepository<ApplicantPreference> preferenceRepository;
        private readonly IGenericRepository<ApplicantOfferFavourite> applicantOfferFavouritRepository;
        private readonly IJobCategoriService jobCategoriService;
        private readonly IGenericRepository<AppliedJob> applicantAppliedJobsRepository;
        private readonly IGenericRepository<AppliedJobDocument> applicantAppliedJobDocumentsRepository;
        private readonly IGenericRepository<AppliedJobMessage> appliedJobMessageRepository;
        private readonly IGenericRepository<ApplicantBlacklistOfCompany> applicantBlacklistOfCompanyRepository;
        private readonly IGenericRepository<CompanyApplicantFavourite> companyApplicantFavouriteRepository;
        private readonly IGenericRepository<Company> companyRepository;
        private readonly IMapper mapper;
        private readonly IConfiguration configuration;
        private readonly IGenericRepository<AppliedJobMessageAttach> appliedJobMessageAttachRepository;
        private readonly IFileManagerService fileManagerService;

        public ApplicantService(IMapper mapper,
            IGenericRepository<ApplicantDocument> applicantDocumentRepository,
            IGenericRepository<ApplicantWorkExperience> workExperienceRepository,
            IGenericRepository<ApplicantEducation> educationRepository,
            IGenericRepository<ApplicantKnowledge> ApplicantknowledgeRepository,
            IGenericRepository<ApplicantLanguage> ApplicantlanguageRepository,
            IGenericRepository<Applicant> ProfileRepository,
            IGenericRepository<Language> LanguageRepository,
            IGenericRepository<Knowledge> KnowledgeRepository,
            IGenericRepository<Offer> offerRepository,
            IGenericRepository<ApplicantPreference> preferenceRepository,
            IGenericRepository<ApplicantOfferFavourite> ApplicantOfferFavouritRepository,
            IJobCategoriService jobCategoriService,
            IGenericRepository<AppliedJob> applicantAppliedJobsRepository,
            IGenericRepository<AppliedJobDocument> applicantAppliedJobDocumentsRepository,
            IGenericRepository<AppliedJobMessage> appliedJobMessageRepository,
            IGenericRepository<ApplicantBlacklistOfCompany> applicantBlacklistOfCompanyRepository,
            IGenericRepository<Company> companyRepository,
            IGenericRepository<CompanyApplicantFavourite> companyApplicantFavouriteRepository,
            IGenericRepository<AppliedJobMessageAttach> appliedJobMessageAttachRepository,
            IFileManagerService fileManagerService,
            IConfiguration configuration)

        {
            this.applicantDocumentRepository = applicantDocumentRepository;
            this.workExperienceRepository = workExperienceRepository;
            this.educationRepository = educationRepository;
            applicantknowledgeRepository = ApplicantknowledgeRepository;
            applicantlanguageRepository = ApplicantlanguageRepository;
            this.profileRepository = ProfileRepository;
            languageRepository = LanguageRepository;
            knowledgeRepository = KnowledgeRepository;
            this.offerRepository = offerRepository;
            this.mapper = mapper;
            this.preferenceRepository = preferenceRepository;
            this.applicantOfferFavouritRepository = ApplicantOfferFavouritRepository;
            this.jobCategoriService = jobCategoriService;
            this.applicantAppliedJobsRepository = applicantAppliedJobsRepository;
            this.applicantAppliedJobDocumentsRepository = applicantAppliedJobDocumentsRepository;
            this.appliedJobMessageRepository = appliedJobMessageRepository;
            this.applicantBlacklistOfCompanyRepository = applicantBlacklistOfCompanyRepository;
            this.companyRepository = companyRepository;
            this.companyApplicantFavouriteRepository = companyApplicantFavouriteRepository;
            this.appliedJobMessageAttachRepository = appliedJobMessageAttachRepository;
            this.fileManagerService = fileManagerService;
            this.configuration = configuration;
        }

        #region Claims
        public bool GetApplicantUserClaims(ClaimsPrincipal user)
                => user.Claims.Where(a => a.Type.Equals(ClaimTypes.Role)).ToList().Select(x => x.Value == ((int)RoleType.Applicant).ToString()).FirstOrDefault();

        #endregion
        #region Applicant
        public ApplicantDto GetApplicant(string email)
        {
            var applicant = GetApplicantsQuery().FirstOrDefault(a => a.Email.Equals(email));
            return mapper.Map<ApplicantDto>(applicant);
        }
        public async Task<bool> IsApplicantExist(string email)
        {
            var applicant = await profileRepository.GetEntities().SingleOrDefaultAsync(x => x.Email.Equals(email));
            if (applicant is null)
                return false;
            return true;
        }

        public ApplicantDto GetApplicant(int id)
        {
            var applicant = profileRepository.GetEntities().AsNoTracking().FirstOrDefault(a => a.Id == id);
            return mapper.Map<ApplicantDto>(applicant);
        }

        public List<ApplicantDto> GetApplicants(int skip, int take)
        {
            var applicants = GetApplicantsQuery().Skip(skip).Take(take);
            return mapper.Map<List<ApplicantDto>>(applicants);
        }
        public PaginationDto<ApplicantDto> GetApplicants(int currentPage, string
            ? key, string? location)
        {
            var itemPerPage = 5;

            var query = GetApplicantsQuery();
            if (!string.IsNullOrWhiteSpace(key))
                query = query.Where(x => x.JobPosition.Contains(key));
            if (!string.IsNullOrWhiteSpace(location))
                query = query.Where(x => x.Country.Name.Contains(location) || x.CityName.Contains(location));
            var count = query.Count();
            var skip = Math.Min((currentPage - 1) * itemPerPage, count - 1);
            query = query.Skip(skip).Take(itemPerPage);
            try
            {
                var data = mapper.Map<List<ApplicantDto>>(query);
                var result = new PaginationDto<ApplicantDto>
                {
                    Data = data,
                    PageCount = (int)Math.Ceiling(((double)count / itemPerPage)),
                    ItemsCount = count
                };
                return result;
            }
            catch
            {
                return new PaginationDto<ApplicantDto>();
            }
        }


        public int GetApplicantsCount(string key, string location)
        {
            var query = profileRepository.GetEntities().Include(x => x.ApplicantKnowledges).AsQueryable();
            if (!string.IsNullOrWhiteSpace(key))
                query = query.Where(x => x.JobPosition.Contains(key));
            if (!string.IsNullOrWhiteSpace(location))
                query = query.Where(x => x.Country.Name.Contains(location) || x.CityName.Contains(location));
            return query.Count();
        }

        private IQueryable<Applicant> GetApplicantsQuery()
        {
            var applicantsQuery = profileRepository.GetEntities()

                  .Include(a => a.ApplicantEducations)
                  .Include(a => a.ApplicantKnowledges)
                  .Include(a => a.ApplicantLanguages)
                  .Include(a => a.ApplicantWorkExperiences)
                  .Include(a => a.ApplicantDocuments.Where(d => d.IsDeleted == false))
                  .AsNoTracking().AsQueryable();
            return applicantsQuery;
        }

        public PaginationDto<ApplicantDto> ApplicantsFilter(ApplicantFilterDto applicantFilterDto)
        {
            var model = GetApplicantsQuery().Where(a => !string.IsNullOrEmpty(a.FirstName) && !string.IsNullOrEmpty(a.LastName));
            var pageSize = 5;


            if (applicantFilterDto.IsFreelancer)
                model = model.Where(x => (bool)x.IsFreelancer);
            if (applicantFilterDto.IsEuropeanUnion)
                model = model.Where(x => (bool)x.IsEuropeanUnion);
            if (applicantFilterDto.IsFullTime)
                model = model.Where(x => (bool)x.IsFullTime);
            if (applicantFilterDto.IsInternShip)
                model = model.Where(x => (bool)x.IsInternShip);
            if (applicantFilterDto.IsPartialworkfromhome)
                model = model.Where(x => (bool)x.IsPartialRemote);
            if (applicantFilterDto.IsPartTime)
                model = model.Where(x => (bool)x.IsPartTime);
            if (applicantFilterDto.IsSwitzerland)
                model = model.Where(x => (bool)x.IsSwitzerland);
            if (applicantFilterDto.IsUnitedStatesofAmerica)
                model = model.Where(x => (bool)x.IsUnitedStatesofAmerica);
            if (applicantFilterDto.IsWorkFromHome)
                model = model.Where(x => (bool)x.IsOffSite);
            if (applicantFilterDto.IsWorkPlace)
                model = model.Where(x => (bool)x.IsOnSite);

            if (!string.IsNullOrWhiteSpace(applicantFilterDto.Language))
            {
                model = model.Where(x => x.ApplicantLanguages.Any(l => l.LanguageName == applicantFilterDto.Language));
            }
            if (!string.IsNullOrWhiteSpace(applicantFilterDto.Knowledge))
            {
                model = model.Where(x => x.ApplicantKnowledges.Any(k => k.KnowledgeName == applicantFilterDto.Knowledge));
            }

            var count = model.Count();
            var skip = Math.Max(0, (int.Parse(applicantFilterDto.CurrentPage) - 1) * pageSize);
            var filterModel = model.Skip(skip).Take(pageSize).ToList();

            var result = new PaginationDto<ApplicantDto>
            {
                Data = mapper.Map<List<ApplicantDto>>(filterModel),
                PageCount = (int)Math.Ceiling((double)count / pageSize),
                ItemsCount = count,
                Page = int.Parse(applicantFilterDto.CurrentPage)
            };

            return result;
        }
        #endregion
        #region Applicant Image
        public async Task<bool> UpdateApplicantImage(string imageFileName, string imageName, string applicantEmail)
        {
            var applicant = await profileRepository.GetEntities().SingleOrDefaultAsync(a => a.Email.Equals(applicantEmail));
            applicant.ImageFileName = imageFileName;
            applicant.ImageName = imageName;
            try
            {
                profileRepository.UpdateEntity(applicant);
                await profileRepository.SaveChange();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion
        #region ApplicantDocuments

        public async Task<ApplicantDocumentDto> GetApplicantDocument(int id)
        {
            var applicantDocument = await applicantDocumentRepository.GetEntities()
                .AsNoTracking().FirstOrDefaultAsync(x => x.Id.Equals(id));
            if (applicantDocument is null)
                return null;
            var applicantDocumentDto = mapper.Map<ApplicantDocumentDto>(applicantDocument);
            return applicantDocumentDto;
        }

        public async Task<ApplicantDocumentDto> GetApplicantDocument(int fileId, string email)
        {
            var applicantDocument = await applicantDocumentRepository.GetEntities().AsNoTracking().FirstOrDefaultAsync(x => x.ApplicantEmail.Equals(email) && x.Id == fileId);
            if (applicantDocument is null)
                return null;
            var applicantDocumentDto = mapper.Map<ApplicantDocumentDto>(applicantDocument);
            return applicantDocumentDto;
        }
        public async Task<ResultDto<FileApplicantDocumentDto>> GetApplicantDocumentFile(int fileId)
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
        public List<ApplicantDocumentDto> GetApplicantDocumentsByEmail(string email)
        {
            var ApplicantDocumentsList = applicantDocumentRepository.GetEntities().AsNoTracking().Where(x => x.ApplicantEmail.Equals(email) && x.IsDeleted == false);
            if (ApplicantDocumentsList == null)
                return null;
            var ApplicantDocumentsListDtoList = mapper.Map<List<ApplicantDocumentDto>>(ApplicantDocumentsList);
            return ApplicantDocumentsListDtoList;
        }
        public async Task<List<ApplicantDocumentDto>> GetRemovedApplicantDocumentsByEmail(string email)
        {
            var ApplicantDocumentsList = await applicantDocumentRepository.GetEntities().AsNoTracking().Where(x => x.ApplicantEmail.Equals(email) && x.IsDeleted == true).ToListAsync();
            if (ApplicantDocumentsList == null)
                return null;
            var ApplicantDocumentsListDtoList = mapper.Map<List<ApplicantDocumentDto>>(ApplicantDocumentsList);
            return ApplicantDocumentsListDtoList;
        }
        public async Task<bool> RemoveApplicantDocument(int id)
        {
            var applicant = await applicantDocumentRepository.GetEntities().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (applicant == null)
                return false;
            try
            {
                applicantDocumentRepository.RemoveEntity(applicant);
                await applicantDocumentRepository.SaveChange();
                return true;
            }
            catch (Exception)
            {

                return false;
            }

        }
        public async Task<bool> DeleteApplicantDocument(int id)
        {
            var applicant = await applicantDocumentRepository.GetEntities().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (applicant == null)
                return false;
            try
            {
                applicantDocumentRepository.DeleteEntity(applicant);
                await applicantDocumentRepository.SaveChange();
                return true;
            }
            catch (Exception)
            {

                return false;
            }

        }

        public async Task<bool> InsertApplicantDocument(ApplicantDocumentDto applicantDocumentsDto)
        {
            try
            {

                int MaxDocumentsPerSection = 4;

                var documents = applicantDocumentRepository.GetEntities()
                    .Where(x => x.ApplicantEmail.Equals(applicantDocumentsDto.ApplicantEmail) && !x.IsDeleted);
                int documentsInCurrentSection = documents.Count(x => x.Type == applicantDocumentsDto.Type);

                if ((documents.Count() >= 0 && documents.Count() < 16) || applicantDocumentsDto.Type == UploadDocumentType.Site_Generated)
                {

                    if (documentsInCurrentSection < MaxDocumentsPerSection)
                    {
                        var applicantDocument = mapper.Map<ApplicantDocument>(applicantDocumentsDto);
                        if (documents.Count() == 0)
                        {
                            applicantDocument.IsDefault = true;
                        }
                        await applicantDocumentRepository.AddEntity(applicantDocument);
                        await applicantDocumentRepository.SaveChange();
                    }
                    else
                    {

                        return false;
                    }
                }
                else
                {
                    return false;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateApplicantDocument(ApplicantDocumentDto applicantDocumentsDto)
        {
            try
            {
                var applicantDocument = applicantDocumentRepository.GetEntities().AsNoTracking().SingleOrDefault(x => x.Id == applicantDocumentsDto.Id);
                if (applicantDocument == null)
                    return false;
                var ApplicantDocument = mapper.Map<ApplicantDocument>(applicantDocumentsDto);
                applicantDocumentRepository.UpdateEntity(applicantDocument);
                await applicantDocumentRepository.SaveChange();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public async Task<bool> RestoreApplicantDocument(int id)
        {
            try
            {
                var applicantDocument = applicantDocumentRepository.GetEntities().AsNoTracking().SingleOrDefault(x => x.Id == id);
                if (applicantDocument == null)
                    return false;
                var ApplicantDocument = mapper.Map<ApplicantDocument>(applicantDocument);
                ApplicantDocument.IsDeleted = false;
                applicantDocumentRepository.UpdateEntity(applicantDocument);
                await applicantDocumentRepository.SaveChange();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public async Task<bool> SetActiveDocument(int selectedDocumentId, string applicantEmail)
        {
            var currentActiveDocument = applicantDocumentRepository.GetEntities().Where(x => x.ApplicantEmail == applicantEmail).FirstOrDefault(x => x.IsDefault);

            if (currentActiveDocument != null)
            {
                currentActiveDocument.IsDefault = false;
            }
            var selectedDocument = applicantDocumentRepository.GetEntities().FirstOrDefault(doc => doc.Id == selectedDocumentId);
            if (selectedDocument != null)
            {
                selectedDocument.IsDefault = true;
                await applicantDocumentRepository.SaveChange();
                return true;
            }
            return false;
        }

        public List<ApplicantDocumentDto> GetApplicantDocumentsByEmailAndType(string email, UploadDocumentType type)
        {
            var ApplicantDocumentsList = applicantDocumentRepository.GetEntities()
                .AsNoTracking()
                .Where(x => x.ApplicantEmail.Equals(email) && x.IsDeleted == false && x.Type == type);

            if (ApplicantDocumentsList == null)
                return null;

            var ApplicantDocumentsListDtoList = mapper.Map<List<ApplicantDocumentDto>>(ApplicantDocumentsList);
            return ApplicantDocumentsListDtoList;
        }
        #endregion
        #region WorkExperience
        public List<ApplicantWorkExperienceDto> GetAllApplicantWorkExperienceByEmail(string email)
        {
            var workExpreienceList = workExperienceRepository.GetEntities().Where(x => x.ApplicantEmail.Equals(email));
            if (workExpreienceList == null)
                return null;
            var workExperienceListDto = mapper.Map<List<ApplicantWorkExperienceDto>>(workExpreienceList);
            return workExperienceListDto;
        }
        public async Task<bool> InsertOrUpdateWorkExperiance(ApplicantWorkExperienceDto workExperienceDto)
        {
            try
            {
                if (workExperienceDto.Id != 0)
                {
                    var workExperience = await workExperienceRepository.GetEntities().AsNoTracking().FirstOrDefaultAsync(x => x.Id == workExperienceDto.Id);
                    if (workExperience == null)
                        return false;
                    var workExperienceNew = mapper.Map<ApplicantWorkExperience>(workExperienceDto);
                    workExperienceRepository.UpdateEntity(workExperienceNew);
                    await workExperienceRepository.SaveChange();
                    return true;
                }
                else
                {
                    var workExperiencNew = mapper.Map<ApplicantWorkExperience>(workExperienceDto);
                    await workExperienceRepository.AddEntity(workExperiencNew);
                    await workExperienceRepository.SaveChange();
                    return true;
                }

            }
            catch (Exception)
            {

                return false;
            }
        }
        public async Task<ApplicantWorkExperienceDto> GetWorkExperienceById(int id)
        {
            var workExperience = await workExperienceRepository.GetEntities().SingleOrDefaultAsync(i => i.Id == id);
            if (workExperience == null)
                return null;
            return mapper.Map<ApplicantWorkExperienceDto>(workExperience);
        }

        public async Task<bool> DeleteApplicantWorkExperience(int id)
        {
            var applicant = await workExperienceRepository.GetEntities().FirstOrDefaultAsync(x => x.Id == id);
            if (applicant == null)
                return false;
            try
            {
                workExperienceRepository.DeleteEntity(applicant);
                await workExperienceRepository.SaveChange();
                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }


        #endregion
        #region Applicant Education
        public async Task<bool> CreateOrUpdateEducation(ApplicantEducationDto educationDto)
        {
            try
            {
                if (educationDto.Id != 0)
                {
                    var ApplicantEducation = await educationRepository.GetEntities().AsNoTracking().SingleOrDefaultAsync(x => x.Id == educationDto.Id);
                    if (ApplicantEducation == null)
                        return false;
                    var ApplicantEducationNew = mapper.Map<ApplicantEducation>(educationDto);
                    educationRepository.UpdateEntity(ApplicantEducationNew);
                    await educationRepository.SaveChange();
                    return true;
                }
                else
                {
                    var ApplicantEducationNew = mapper.Map<ApplicantEducation>(educationDto);
                    await educationRepository.AddEntity(ApplicantEducationNew);
                    await educationRepository.SaveChange();
                    return true;
                }

            }
            catch (Exception)
            {

                return false;
            }
        }

        public List<ApplicantEducationDto> GetAllApplicantEducation(string email)
        {
            var ApplicantEducationList = educationRepository.GetEntities().Where(x => x.ApplicantEmail.Equals(email));
            if (ApplicantEducationList == null)
                return null;
            var ApplicantEducationListDto = mapper.Map<List<ApplicantEducationDto>>(ApplicantEducationList);
            return ApplicantEducationListDto;
        }

        public async Task<ApplicantEducationDto> GetApplicantEducationById(int id)
        {
            var applicantEducation = await educationRepository.GetEntities().AsNoTracking().SingleOrDefaultAsync(i => i.Id == id);
            if (applicantEducation == null)
                return null;
            return mapper.Map<ApplicantEducationDto>(applicantEducation);
        }

        public async Task<bool> DeleteApplicantEducation(int id)
        {
            var applicantEducation = await educationRepository.GetEntities().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (applicantEducation == null)
                return false;
            try
            {
                educationRepository.DeleteEntity(applicantEducation);
                await educationRepository.SaveChange();
                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }
        #endregion
        #region Applicant Knowledge

        public async Task<bool> CreateOrUpdateKhnowledge(ApplicantKnowledgeDto knowledgeDto)
        {
            try
            {
                if (knowledgeDto.Id != 0)
                {
                    var ApplicantKnowledge = await applicantknowledgeRepository.GetEntities().AsNoTracking().SingleOrDefaultAsync(x => x.Id == knowledgeDto.Id);
                    if (ApplicantKnowledge == null)
                        return false;
                    var ApplicantKnowledgeNew = mapper.Map<ApplicantKnowledge>(knowledgeDto);
                    applicantknowledgeRepository.UpdateEntity(ApplicantKnowledgeNew);
                    await applicantknowledgeRepository.SaveChange();
                    return true;
                }
                else
                {
                    var ApplicantKnowledgeNew = mapper.Map<ApplicantKnowledge>(knowledgeDto);
                    await applicantknowledgeRepository.AddEntity(ApplicantKnowledgeNew);
                    await applicantknowledgeRepository.SaveChange();
                    return true;
                }

            }
            catch (Exception)
            {

                return false;
            }
        }

        public List<ApplicantKnowledgeDto> GetAllApplicantKnowledge(string email)
        {
            var ApplicantKnowledgeList = applicantknowledgeRepository.GetEntities().Where(x => x.ApplicantEmail.Equals(email)).AsNoTracking();
            if (ApplicantKnowledgeList == null)
                return null;
            var ApplicantKnowledgeListDto = mapper.Map<List<ApplicantKnowledgeDto>>(ApplicantKnowledgeList);
            return ApplicantKnowledgeListDto;
        }
        public List<KnowledgeDto> GetKnowledgeList()
        {
            var knowledgeList = knowledgeRepository.GetEntities().ToList();
            var knowledgeDtoList = mapper.Map<List<KnowledgeDto>>(knowledgeList);
            return knowledgeDtoList;
        }

        public async Task<ApplicantKnowledgeDto> GetApplicantKnowledgeById(int id)
        {
            var applicantKnowledge = await applicantknowledgeRepository.GetEntities().AsNoTracking().SingleOrDefaultAsync(i => i.Id == id);
            if (applicantKnowledge == null)
                return null;
            return mapper.Map<ApplicantKnowledgeDto>(applicantKnowledge);
        }

        public async Task<bool> DeleteApplicantKnowledge(int id)
        {
            var applicantKnowledge = await applicantknowledgeRepository.GetEntities().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (applicantKnowledge == null)
                return false;
            try
            {
                applicantknowledgeRepository.DeleteEntity(applicantKnowledge);
                await applicantknowledgeRepository.SaveChange();
                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }
        #endregion
        #region Applicant Language
        public async Task<bool> CreateOrUpdateLanguage(ApplicantLanguageDto languageDto)
        {
            try
            {
                if (languageDto.Id != 0)
                {
                    var applicantLanguage = await applicantlanguageRepository.GetEntities().AsNoTracking().SingleOrDefaultAsync(x => x.Id == languageDto.Id);
                    if (applicantLanguage == null)
                        return false;
                    var applicantLanguageNew = mapper.Map<ApplicantLanguage>(languageDto);
                    applicantlanguageRepository.UpdateEntity(applicantLanguageNew);
                    await applicantlanguageRepository.SaveChange();
                    return true;
                }
                else
                {
                    var applicantLanguageNew = mapper.Map<ApplicantLanguage>(languageDto);
                    await applicantlanguageRepository.AddEntity(applicantLanguageNew);
                    await applicantlanguageRepository.SaveChange();
                    return true;
                }

            }
            catch (Exception)
            {

                return false;
            }
        }

        public List<ApplicantLanguageDto> GetAllApplicantLanguage(string email)
        {
            var ApplicantLanguageList = applicantlanguageRepository.GetEntities().Where(x => x.ApplicantEmail.Equals(email));
            if (ApplicantLanguageList == null)
                return null;
            var ApplicantLanguageListDto = mapper.Map<List<ApplicantLanguageDto>>(ApplicantLanguageList);
            return ApplicantLanguageListDto;
        }

        public async Task<ApplicantLanguageDto> GetApplicantLanguageById(int id)
        {
            var applicantLanguage = await applicantlanguageRepository.GetEntities().SingleOrDefaultAsync(i => i.Id == id);
            if (applicantLanguage == null)
                return null;
            return mapper.Map<ApplicantLanguageDto>(applicantLanguage);
        }

        public async Task<bool> DeleteApplicantLanguage(int id)
        {
            var applicantLanguage = await applicantlanguageRepository.GetEntities().FirstOrDefaultAsync(x => x.Id == id);
            if (applicantLanguage == null)
                return false;
            try
            {
                applicantlanguageRepository.DeleteEntity(applicantLanguage);
                await applicantlanguageRepository.SaveChange();
                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }
        public List<LanguageDto> GetLanguageList()
        {
            var languages = languageRepository.GetEntities().ToList();
            var knowledgeDtos = mapper.Map<List<LanguageDto>>(languages);
            return knowledgeDtos;
        }


        #endregion
        #region Personal Information
        public ApplicantDto GetPersonalInformation(string email)
        {
            var applicant = profileRepository.GetEntities().SingleOrDefault(x => x.Email.Equals(email));
            var PersonalInformationDto = mapper.Map<ApplicantDto>(applicant);
            return PersonalInformationDto;
        }

        public async Task<bool> SavePersonalInformation(ApplicantProfileDto personalInformation)
        {
            var applicant = await profileRepository.GetEntities().FirstOrDefaultAsync(x => x.Email == personalInformation.Email);
            if (applicant == null)
                return false;
            try
            {

                applicant.FirstName = personalInformation.FirstName;
                applicant.LastName = personalInformation.LastName;
                applicant.Gender = personalInformation.Gender;
                applicant.DateOfBirth = personalInformation.DateOfBirth;
                applicant.AvailableDate = personalInformation.AvailableDate;
                applicant.ReadyToWorkStatus = personalInformation.ReadyToWorkStatus;
                applicant.JobPosition = personalInformation.JobPosition;
                profileRepository.UpdateEntity(applicant);
                await profileRepository.SaveChange();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion
        #region Contact Detail
        public ApplicantDto GetApplicantContactDetail(string email)
        {
            var applicant = profileRepository.GetEntities().AsNoTracking().FirstOrDefault(x => x.Email == email);
            var PersonalContactDetailDto = mapper.Map<ApplicantDto>(applicant);
            return PersonalContactDetailDto;
        }



        public async Task<bool> SaveApplicantContactDetail(ApplicantContactDetailsDto contactDetailsDto, string email)
        {

            var applicant = await profileRepository.GetEntities().FirstOrDefaultAsync(x => x.Email == email);



            if (applicant == null)
                return false;
            try
            {
                applicant.CountryCode = contactDetailsDto.CountryCode;
                if (contactDetailsDto.CityId != null)
                {
                    applicant.CityId = contactDetailsDto.CityId;
                }
                else
                {
                    applicant.CityId = null;
                }
                applicant.Phone = contactDetailsDto.Phone;
                applicant.Address = contactDetailsDto.Address;
                applicant.PostalCode = contactDetailsDto.PostalCode;
                applicant.CityName = contactDetailsDto.CityName;
                applicant.StateName = contactDetailsDto.StateName;

                profileRepository.UpdateEntity(applicant);
                await profileRepository.SaveChange();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion
        #region Applicant Additional Section
        public ApplicantDto GetApplicantAdditionalSection(string email)
        {
            var applicant = profileRepository.GetEntities().FirstOrDefault(x => x.Email == email);

            var applicantAdditionalSection = mapper.Map<ApplicantDto>(applicant);
            applicantAdditionalSection.Email = email;
            return applicantAdditionalSection;
        }

        public async Task<bool> SaveApplicantAdditionalSection(ApplicantAddtionalSectionDto addtionalSectionDto)
        {
            var applicant = await profileRepository.GetEntities().FirstOrDefaultAsync(x => x.Email == addtionalSectionDto.Email);
            if (applicant == null)
                return false;
            try
            {

                applicant = mapper.Map(addtionalSectionDto, applicant);

                profileRepository.UpdateEntity(applicant);
                await profileRepository.SaveChange();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        #endregion
        #region Application Privacy
        public Task<ApplicantPrivacyDto> GetPrivacyByEmail(string email)
        {
            var privacy = profileRepository.GetEntities().FirstOrDefault(x => x.Email.Equals(email));
            return Task.FromResult(mapper.Map<ApplicantPrivacyDto>(privacy));
        }

        public async Task<bool> UpdatePrivacy(ApplicantPrivacyDto applicantPrivacyDto)
        {
            var userPrivacy = GetApplicantsQuery().FirstOrDefault(a => a.Email.Equals(applicantPrivacyDto.Email));
            if (userPrivacy != null)
            {
                userPrivacy.ShowGender = applicantPrivacyDto.ShowGender;
                userPrivacy.ShowAddress = applicantPrivacyDto.ShowAddress;
                userPrivacy.ShowAge = applicantPrivacyDto.ShowAge;
                userPrivacy.ShowCountryOrCity = applicantPrivacyDto.ShowCountryOrCity;
                userPrivacy.AllowSearchEngines = applicantPrivacyDto.AllowSearchEngines;
                userPrivacy.ShowPhone = applicantPrivacyDto.ShowPhone;
                userPrivacy.SendEmail = applicantPrivacyDto.SendEmail;
                profileRepository.UpdateEntity(userPrivacy);
            }

            var records = preferenceRepository.GetEntities()
                .Where(x => x.ApplicantEmail.Equals(applicantPrivacyDto.Email)).ToList();

            foreach (var record in records)
            {
                if (record.Category == "JobNotification")
                    record.IsSubscribed = applicantPrivacyDto.JobNotification;
                if (record.Category == "NewsLetter")
                    record.IsSubscribed = applicantPrivacyDto.NewsLetter;
            }


            var jobNotificationPreference = new ApplicantPreferenceDto
            {
                ApplicantEmail = applicantPrivacyDto.Email,
                Category = "JobNotification",
                IsSubscribed = applicantPrivacyDto.JobNotification
            };

            var newsLetterPreference = new ApplicantPreferenceDto
            {
                ApplicantEmail = applicantPrivacyDto.Email,
                Category = "NewsLetter",
                IsSubscribed = applicantPrivacyDto.NewsLetter
            };

            await preferenceRepository.AddEntity(mapper.Map<ApplicantPreference>(jobNotificationPreference));
            await preferenceRepository.AddEntity(mapper.Map<ApplicantPreference>(newsLetterPreference));


            await profileRepository.SaveChange();
            await preferenceRepository.SaveChange();

            return true;
        }
        #endregion
        #region Applicant EmailPreferences

        public List<ApplicantPreferenceDto> GetEmailPreferences(string email)
        {
            var result = preferenceRepository.GetEntities().Where(x => x.ApplicantEmail.Equals(email)).ToList();
            return mapper.Map<List<ApplicantPreferenceDto>>(result);

        }
        public bool UnsubscribeEmailPreferences(string email)
        {
            var records = preferenceRepository.GetEntities().Where(x =>
                x.ApplicantEmail.Equals(email)).ToList();
            if (records.Count > 0)
            {
                foreach (var applicantPreference in records)
                {
                    applicantPreference.IsSubscribed = false;
                    preferenceRepository.UpdateEntity(applicantPreference);
                    preferenceRepository.SaveChange();
                }
            }
            return true;
        }

        #endregion

        #region offers
        public List<OfferDto> GetOffers()
        {
            var offers = mapper.Map<List<OfferDto>>(offerRepository.GetEntities()
                 .Include(x => x.Company)
                 .ThenInclude(x => x.City)
                 .Include(x => x.OfferLanguages)
                 .ThenInclude(x => x.Language)
                 .Include(x => x.OfferKnowledges)
                 .ThenInclude(x => x.Knowledge)
                .Include(x => x.OfferJobCategories)
                .ThenInclude(x => x.JobCategory));

            return offers;
        }

        public PaginationDto<OfferDto> SearchOffersAjax(string location, int currentPage, string language,
            string workPlace, string jobOffer, string employer, string position, string company,
            List<string> jobCategories)
        {
            var itemPerPage = 5;
            var offers = GetOffers();
            List<OfferDto> newOffer = new List<OfferDto>();
            if (jobCategories.First() != null && jobCategories.First().Contains(","))
                jobCategories = jobCategories.First().Split(",").ToList();
            if (jobCategories.Count >= 1 && jobCategories.First() != null)
            {
                foreach (var item in jobCategories)
                    //{
                    //    if (item != null)
                    //    {
                    //        List<int> offerIds = offerJobCategoryRepository.GetEntities()
                    //            .Where(x => x.JobCategoryId == Int32.Parse(item)).Select(x => x.OfferId).ToList();

                    //        foreach (var offerId in offerIds)
                    //        {
                    //            var IsOfferExist = newOffer.Any(x => x.Id == offerId);
                    //            if (!IsOfferExist)
                    //            {
                    //                newOffer.AddRange(offers.Where(x => x.Id == offerId).ToList());
                    //            }
                    //        }
                    //    }
                    //}
                    offers = newOffer;

            }
            if (!string.IsNullOrWhiteSpace(location))
            {

                offers = offers.Where(x => x.CompanyDto.CityDto.Name.Contains(location) || x.CompanyDto.CityDto.StateName == location).ToList();
            }

            if (!string.IsNullOrWhiteSpace(company))
            {
                newOffer.Clear();
                newOffer.AddRange(offers.Where(x => x.CompanyDto.Name == company).ToList());
                offers = newOffer;
            }
            if (!string.IsNullOrWhiteSpace(language))
            {
                var requestedLanguage = languageRepository.GetEntities().AsNoTracking().Single(x => x.Name == language);
                foreach (var item in offers)
                {
                    if (item.OfferLanguageDtos.Any(x => x.LanguageDto.Id == requestedLanguage.Id))
                    {
                        newOffer.AddRange(offers.Where(x => x.Id == item.Id));
                    }
                }

                offers = newOffer;
            }
            if (!string.IsNullOrWhiteSpace(jobOffer))
            {
                offers = offers.Where(x => x.PackageName.Equals(jobOffer)).ToList();
            }
            if (!string.IsNullOrWhiteSpace(employer))
            {
                offers = SearchEmployerStatus(employer, offers);
            }
            if (!string.IsNullOrWhiteSpace(workPlace))
            {
                offers = SearchWorkPlace(workPlace, offers);
            }
            if (!string.IsNullOrWhiteSpace(position))
            {
                offers = SearchForJobPostions(position, offers, newOffer);
            }
            var count = offers.Count();
            var skip = Math.Min((currentPage - 1) * itemPerPage, count - 1);
            offers = offers.Skip(skip).Take(itemPerPage).ToList();
            try
            {
                var data = mapper.Map<List<OfferDto>>(offers);
                var result = new PaginationDto<OfferDto>
                {
                    Data = data,
                    PageCount = (int)Math.Ceiling(((double)count / itemPerPage)),
                    ItemsCount = count
                };
                return result;
            }
            catch
            {
                return new PaginationDto<OfferDto>();
            }

        }

        private List<OfferDto> SearchForJobPostions(string position, List<OfferDto> offers, List<OfferDto> newOffer)
        {
            newOffer.Clear();
            var allJobs = jobCategoriService.GetJobCategories().Result.FirstOrDefault(x => x.Jobcategory == position);
            foreach (var item in offers)
            {
                if (item.OfferJobCategoryDtos.Any(x => x.JobCategoryDto.Jobcategory == position))
                {
                    newOffer.AddRange(offers.Where(x => x.Id == item.Id));
                }

            }
            offers = newOffer;
            return offers;
        }

        private static List<OfferDto> SearchWorkPlace(string workPlace, List<OfferDto> offers)
        {
            if (Res.CompanyOffer.WorkOnlyFromHome == workPlace)
            {
                offers = offers.Where(x => x.IsOnSite).ToList();
            }
            if (Res.CompanyOffer.MaybePartially == workPlace)
            {
                offers = offers.Where(x => x.IsPartialRemote).ToList();
            }
            if (Res.CompanyOffer.WorkAtTheRegularWorkPlace == workPlace)
            {
                offers = offers.Where(x => x.IsOffSite).ToList();
            }

            return offers;
        }

        private static List<OfferDto> SearchEmployerStatus(string employer, List<OfferDto> offers)
        {
            if (employer.Equals(Res.CompanyOffer.FullTime))
            {
                offers = offers.Where(x => x.IsFullTime).ToList();
            }
            if (employer.Equals(Res.CompanyOffer.PartTime))
            {
                offers = offers.Where(x => x.IsPartTime).ToList();
            }

            if (employer.Equals(Res.CompanyOffer.Freelancer))
            {
                offers = offers.Where(x => x.IsFreelancer).ToList();
            }
            if (employer.Equals(Res.CompanyOffer.InternShip))
            {
                offers = offers.Where(x => x.IsInternShip).ToList();
            }

            return offers;
        }

        public PaginationDto<OfferDto> SearchOffersAjax()
        {
            var itemPerPage = 5;
            var offers = GetOffers();
            var count = offers.Count();
            offers = offers.ToList();

            try
            {
                var data = mapper.Map<List<OfferDto>>(offers);
                var result = new PaginationDto<OfferDto>
                {
                    Data = data,
                    PageCount = (int)Math.Ceiling(((double)count / itemPerPage)),
                    ItemsCount = count
                };
                return result;
            }
            catch
            {
                return new PaginationDto<OfferDto>();
            }

        }
        public async Task<ApplicantOffersFavouriteResult> SaveApplicantOfferFavourite(ApplicantOfferFavouriteDto applicantOffersFavouriteDto)
        {

            if (applicantOffersFavouriteDto.OfferId == Guid.Empty) return ApplicantOffersFavouriteResult.Badrequest;
            if (applicantOffersFavouriteDto.ApplicantEmail == null) return ApplicantOffersFavouriteResult.Badrequest;
            if (await IsOfferExistToOfferFavouriteList(applicantOffersFavouriteDto) == false) return ApplicantOffersFavouriteResult.AddedBefore;
            var applicantOfferFavourite = mapper.Map<ApplicantOfferFavourite>(applicantOffersFavouriteDto);

            await applicantOfferFavouritRepository.AddEntity(applicantOfferFavourite);
            await applicantOfferFavouritRepository.SaveChange();
            return ApplicantOffersFavouriteResult.Success;
        }

        private async Task<bool> IsOfferExistToOfferFavouriteList(ApplicantOfferFavouriteDto applicantOffersFavouriteDto)
        {
            var offerFavourite = await applicantOfferFavouritRepository.GetEntities().SingleOrDefaultAsync(x => x.OfferId
            == applicantOffersFavouriteDto.OfferId && x.ApplicantEmail == applicantOffersFavouriteDto.ApplicantEmail);
            if (offerFavourite == null) return true;

            return false;
        }
        private async Task<bool> IsCompanyExistToBlackList(ApplicantBlacklistOfCompanyDto applicantBlacklistOfCompanyDto)
        {
            var offerFavourite = await applicantBlacklistOfCompanyRepository.GetEntities().SingleOrDefaultAsync(x => x.OfferId
            == applicantBlacklistOfCompanyDto.OfferId && x.ApplicantEmail == applicantBlacklistOfCompanyDto.ApplicantEmail);
            if (offerFavourite == null) return true;

            return false;
        }

        public List<OfferDto> GetAllFavourteApplicantOffers(string applicantEmail)
        {
            var offers = mapper.Map<List<OfferDto>>(offerRepository.GetEntities()
               .Include(x => x.Company)
               .ThenInclude(x => x.City)
               .Include(x => x.OfferLanguages)
               .ThenInclude(x => x.Language)
               .Include(x => x.OfferKnowledges)
               .ThenInclude(x => x.Knowledge)
              .Include(x => x.OfferJobCategories)
              .ThenInclude(x => x.JobCategory)
              .Include(x => x.ApplicantOffersFavourites)
              .ThenInclude(x => x.Applicant).Where(x => x.ApplicantOffersFavourites.Any(s => s.ApplicantEmail.Equals(applicantEmail)))).ToList();
            return offers;
        }
        private List<OfferDto> GetAllMessagesApplicantOfferses(string applicantEmail)
        {
            var offers = offerRepository.GetEntities()
                .Include(x => x.Company)
                    .ThenInclude(x => x.City)
                .Include(x => x.OfferLanguages)
                    .ThenInclude(x => x.Language)
                .Include(x => x.OfferKnowledges)
                    .ThenInclude(x => x.Knowledge)
                .Include(x => x.OfferJobCategories)
                    .ThenInclude(x => x.JobCategory)
                .Include(x => x.AppliedJobs)
                    .ThenInclude(x => x.AppliedJobMessages)
                .Where(x => x.AppliedJobs.Any(s => s.ApplicantEmail.Equals(applicantEmail) && s.IsDeleted == true))
                .ToList();

            return mapper.Map<List<OfferDto>>(offers);
        }
        public List<ApplicantOfferFavouriteDto> applicantOffersFavouriteDtos(string applicantEmail)
        {
            var applicantOffersFavouriteDtos = mapper.Map<List<ApplicantOfferFavouriteDto>>(applicantOfferFavouritRepository.GetEntities());
            return applicantOffersFavouriteDtos;
        }
        public async Task<bool> DeleteFavouriteOffer(Guid id, string applicantEmail)
        {
            if (id == Guid.Empty) return false;
            var applicantFavoutiteOffer = await applicantOfferFavouritRepository.GetEntities().SingleOrDefaultAsync(x => x.OfferId == id && x.ApplicantEmail.Equals(applicantEmail));
            if (applicantFavoutiteOffer == null) return false;
            try
            {
                applicantOfferFavouritRepository.DeleteEntity(applicantFavoutiteOffer);
                await applicantOfferFavouritRepository.SaveChange();

                return true;
            }
            catch (Exception)
            {

                return false;
            }

        }
        public int GetCountFavouritApplicant(string applicantEmail)
        {
            var count = applicantOfferFavouritRepository.GetEntities().Where(x => x.ApplicantEmail.Equals(applicantEmail)).Count();
            return count;
        }

        public async Task<ApplicantOfferFavouriteDto> GetApplicantOfferFavouriteDtos(string applcantEmail, Guid id)
        {
            var favouriteDto = await applicantOfferFavouritRepository.GetEntities().FirstOrDefaultAsync(x => x.ApplicantEmail == applcantEmail && x.OfferId == id);
            if (favouriteDto is not null)
            {
                return mapper.Map<ApplicantOfferFavouriteDto>(favouriteDto);
            }
            else
                return new ApplicantOfferFavouriteDto();


        }

        #endregion


        #region AppliedJobs
        public async Task<bool> Apply(AppliedJobDto applicantAppliedJobsDto)
        {
            var applicant = GetApplicant(applicantAppliedJobsDto.ApplicantEmail);

            var currentappliedjobs = applicantAppliedJobsRepository.GetEntities()
                .AsNoTracking()
                .Where(x => x.ApplicantEmail == applicantAppliedJobsDto.ApplicantEmail && x.OfferId == applicantAppliedJobsDto.OfferId);

            if (currentappliedjobs != null && currentappliedjobs.Any())
            {
                return false;
            }
            else
            {
                var mappedjobs = mapper.Map<AppliedJob>(applicantAppliedJobsDto);

                await applicantAppliedJobsRepository.AddEntity(mappedjobs);
                await applicantAppliedJobsRepository.SaveChange();
                applicantAppliedJobsDto.ApplicantJobId = mappedjobs.Id;
                return await ApplyDocuments(applicantAppliedJobsDto);
            }
        }

        public async Task<Boolean> ApplyDocuments(AppliedJobDto applicantAppliedJobsDto)
        {

            if (applicantAppliedJobsDto.Message != null)
            {

                AppliedJobMessage appliedMessage = new AppliedJobMessage
                {
                    Message = applicantAppliedJobsDto.Message,
                    AppliedJobId = applicantAppliedJobsDto.ApplicantJobId,
                    ClientType = RoleType.Applicant
                };
                await appliedJobMessageRepository.AddEntity(appliedMessage);
                await appliedJobMessageRepository.SaveChange();
                List<ApplicantDocument> applicantDocuments = new List<ApplicantDocument>();
                foreach (var item in applicantAppliedJobsDto.Files)
                {
                    ApplicantDocument applicantDocument = new ApplicantDocument
                    {
                        Name = applicantAppliedJobsDto.Name,
                        FileName = item.FileName,
                        ApplicantEmail = applicantAppliedJobsDto.ApplicantEmail,
                        Type = UploadDocumentType.OtherDocument
                    };
                    applicantDocuments.Add(applicantDocument);
                }

                await applicantDocumentRepository.AddEntityRange(applicantDocuments);
                await applicantDocumentRepository.SaveChange();
            }

            var appliedDocument = applicantDocumentRepository.GetEntities().FirstOrDefault(x => x.ApplicantEmail == applicantAppliedJobsDto.ApplicantEmail);
            if (applicantAppliedJobsDto.Files != null)
            {
                var applicantdocDto = new List<AppliedJobDocumentDto>();
                foreach (var item in applicantAppliedJobsDto.Files)
                {
                    applicantdocDto.Add(new AppliedJobDocumentDto
                    {
                        ApplicantDocumentId = appliedDocument.Id,
                        AppliedJobId = applicantAppliedJobsDto.ApplicantJobId
                    });
                }
                var mappedjobs = mapper.Map<List<AppliedJobDocument>>(applicantdocDto);
                await applicantAppliedJobDocumentsRepository.AddEntityRange(mappedjobs);
                await applicantAppliedJobDocumentsRepository.SaveChange();
            }
            if (applicantAppliedJobsDto.ResumeList != null)
            {
                var applicantdocDto = new List<AppliedJobDocumentDto>();
                foreach (var item in applicantAppliedJobsDto.ResumeList)
                {
                    applicantdocDto.Add(new AppliedJobDocumentDto
                    {
                        ApplicantDocumentId = Int32.Parse(item),
                        AppliedJobId = applicantAppliedJobsDto.ApplicantJobId,


                    });
                }
                var mappedjobs = mapper.Map<List<AppliedJobDocument>>(applicantdocDto);
                await applicantAppliedJobDocumentsRepository.AddEntityRange(mappedjobs);
                await applicantAppliedJobDocumentsRepository.SaveChange();
            }
            if (!string.IsNullOrWhiteSpace(applicantAppliedJobsDto.Message) &&
                applicantAppliedJobsDto.Files == null)
            {
                AppliedJobMessage appliedMessage = new AppliedJobMessage
                {
                    Message = applicantAppliedJobsDto.Message,
                    AppliedJobId = applicantAppliedJobsDto.ApplicantJobId,
                    ClientType = RoleType.Applicant
                };
                await appliedJobMessageRepository.AddEntity(appliedMessage);
                await appliedJobMessageRepository.SaveChange();
            }
            return true;
        }


        public async Task<bool> SaveMessage(AppliedJobMessageDto appliedJobMessage)
        {
            var appliedJob = applicantAppliedJobsRepository.GetEntities()
                .SingleOrDefault(x => x.Id == appliedJobMessage.AppliedJobId);

            applicantAppliedJobsRepository.UpdateEntity(appliedJob);
            await applicantAppliedJobsRepository.SaveChange();

            AppliedJobMessage appliedMessage = new AppliedJobMessage
            {
                Message = appliedJobMessage.Message,
                AppliedJobId = appliedJobMessage.AppliedJobId,
                ClientType = appliedJobMessage.ClientType
            };

            await appliedJobMessageRepository.AddEntity(appliedMessage);
            await appliedJobMessageRepository.SaveChange();

            var appliedMessageAttacheList = new List<AppliedJobMessageAttach>();

            foreach (var item in appliedJobMessage.AppliedJobMessageAttachDtos)
            {
                AppliedJobMessageAttach appliedAttachment = new AppliedJobMessageAttach
                {
                    Name = item.Name,
                    FileName = item.FileName,
                    AppliedJobMessageId = appliedMessage.Id
                };
                appliedMessageAttacheList.Add(appliedAttachment);
            }

            await appliedJobMessageAttachRepository.AddEntityRange(appliedMessageAttacheList);
            await appliedJobMessageAttachRepository.SaveChange();

            return true;
        }
        public async Task<bool> UpdateMessageIsSeenStatus(int id)
        {

            var messages = appliedJobMessageRepository.GetEntities()
                                     .Where(x => x.AppliedJobId == id)
                                     .ToList();


            if (messages.Any())
            {

                foreach (var message in messages)
                {
                    message.IsSeen = true;
                }

                appliedJobMessageRepository.UpdateEntityRange(messages);
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

        public async Task<AppliedJobsViewModelDto> GetAllAppliedJobsAsync(string applicantEmail, int currentPage = 1)
        {
             var itemPerPage = 5;

                var currentAppliedJobs = applicantAppliedJobsRepository.GetEntities()
                    .AsNoTracking()
                    .Where(x => x.ApplicantEmail.Equals(applicantEmail))
                    .Include(x => x.AppliedJobMessages)
                    .ToList();

                if (!currentAppliedJobs.Any())
                    return null;

                var allApplicantApplyJobs = new List<OfferMobileModel>();
                var filteredApplicantApplyJobs = new List<OfferMobileModel>();

                foreach (var item in currentAppliedJobs)
                {
                    var offerDto = GetOffers().SingleOrDefault(x => x.Id == item.OfferId);

                    if (offerDto == null)
                        continue; 

                    var isRemote = offerDto.IsOnSite || offerDto.IsPartialRemote || offerDto.IsOffSite;

                    var isFavourite = await IsOfferExistToOfferFavouriteList(new ApplicantOfferFavouriteDto
                    {
                        ApplicantEmail = applicantEmail,
                        OfferId = offerDto.Id
                    });

                    var unseenMessagesCount = item.AppliedJobMessages.Count(m => !m.IsSeen && m.ClientType == RoleType.Company);

                    var offers = new OfferMobileModel()
                    {
                        Id = offerDto.Id,
                        CompanyId = offerDto.CompanyDto.Id,
                        CompanyName = offerDto.CompanyDto.Name ?? "",
                        JobTitle = offerDto.JobTitle ?? "",
                        CompanyEmail = offerDto.CompanyEmail ?? "",
                        Description = offerDto.JobDescription ?? "",
                        City = offerDto.CompanyDto.CityName ?? "",
                        ExpireDate = offerDto.ExprationDate,
                        IsRemote = isRemote,
                        DateOfOffer = offerDto.CreateDate,
                        Logo = offerDto.CompanyDto.Logo,
                        knowledgeList = offerDto.OfferKnowledgeDtos.Select(x => x.KnowledgeDto.Name).ToList(),
                        JobCategoryNameList = offerDto.OfferJobCategoryDtos.Select(x => x.JobCategoryDto.Jobcategory).ToList(),
                        AppliedType = item.ApplicationStatus,
                        ApplyJobCreateDate = item.CreateDate,
                        PaymentStatus = offerDto.PaymentStatus,
                        AppliedJobId = item.Id,
                        FavouriteIcon = !isFavourite,
                        IsDeleted = item.IsDeleted,
                        UnseenMessagesCount = unseenMessagesCount,
                        AppliedMessageDtos = item.AppliedJobMessages.Select(m => new AppliedJobMessageDto
                        {
                            Id = m.Id,
                            AppliedJobId = m.AppliedJobId,
                            Message = m.Message,
                            IsSeen = m.IsSeen,
                            ApplicantEmail = m.AppliedJob.ApplicantEmail,
                            ClientType = m.ClientType
                        }).ToList()
                    };

                    allApplicantApplyJobs.Add(offers);

                    if (!item.IsDeleted)
                    {
                        filteredApplicantApplyJobs.Add(offers);
                    }
                }

                var favouriteOffers = GetOffers()
                    .Where(o => applicantOfferFavouritRepository.GetEntities()
                        .Any(fav => fav.ApplicantEmail == applicantEmail && fav.OfferId == o.Id))
                    .Select(o => new OfferMobileModel
                    {
                        Id = o.Id,
                        CompanyId = o.CompanyDto.Id,
                        CompanyName = o.CompanyDto.Name ?? "",
                        JobTitle = o.JobTitle ?? "",
                        CompanyEmail = o.CompanyEmail ?? "",
                        Description = o.JobDescription ?? "",
                        City = o.CompanyDto.CityName ?? "",
                        ExpireDate = o.ExprationDate,
                        IsRemote = o.IsOnSite || o.IsPartialRemote || o.IsOffSite,
                        DateOfOffer = o.CreateDate,
                        Logo = o.CompanyDto.Logo,
                        knowledgeList = o.OfferKnowledgeDtos.Select(x => x.KnowledgeDto.Name).ToList(),
                        JobCategoryNameList = o.OfferJobCategoryDtos.Select(x => x.JobCategoryDto.Jobcategory).ToList(),
                        FavouriteIcon = true,
                        IsDeleted = false,

                    })
                    .ToList();


                var deletedOffers = currentAppliedJobs.Where(x => x.IsDeleted).Select(x => x.OfferId).ToList();


                favouriteOffers.RemoveAll(f => deletedOffers.Contains(f.Id));


                foreach (var offer in allApplicantApplyJobs)
                {
                    if (deletedOffers.Contains(offer.Id))
                    {
                        offer.FavouriteIcon = false; 
                    }
                }


                var count = filteredApplicantApplyJobs.Count;
                var skip = Math.Min((currentPage - 1) * itemPerPage, count - 1);
                var filterOffers = filteredApplicantApplyJobs.Skip(skip).Take(itemPerPage).ToList();

                var result = new AppliedJobsViewModelDto
                {
                    PaginationData = new PaginationDto<OfferMobileModel>
                    {
                        Data = filterOffers,
                        PageCount = (int)Math.Ceiling(((double)count / itemPerPage)),
                        ItemsCount = count,
                        Page = currentPage,
                    },
                    AllData = allApplicantApplyJobs,
                    FavouriteOffers = favouriteOffers
                };

                return result;
            
           
        }


        public AppliedJobDto GetAppliedJob(string applicantEmail, Guid offerId)
        {
            var currentAppliedJob = applicantAppliedJobsRepository.GetEntities()
                .FirstOrDefault(x => x.ApplicantEmail == applicantEmail && x.OfferId == offerId);

            return mapper.Map<AppliedJobDto>(currentAppliedJob);
        }

        public async Task<PaginationDto<OfferMobileModel>> GetAllFavourteApplicantOffersWithPageing(string applicantEmail, int currentPage, string company)
        {
            var itemPerPage = 5;
            var offers = GetAllFavourteApplicantOffers(applicantEmail);

            offers = offers.ToList();
            if (!string.IsNullOrWhiteSpace(company))
                offers = offers.Where(x => x.CompanyDto.Name == company).ToList();

            try
            {
                var data = mapper.Map<List<OfferDto>>(offers);

                var offerMobiles = await GetOfferMobilesList(data, applicantEmail);
                var count = offerMobiles.Count();
                var skip = Math.Min((currentPage - 1) * itemPerPage, count - 1);
                var filterOfferMobile = offerMobiles.Skip(skip).Take(itemPerPage).ToList();

                var result = new PaginationDto<OfferMobileModel>
                {
                    Data = filterOfferMobile,
                    PageCount = (int)Math.Ceiling(((double)count / itemPerPage)),
                    ItemsCount = count,
                    Page = currentPage
                };
                return result;
            }
            catch
            {
                return new PaginationDto<OfferMobileModel>();
            }
        }
        public async Task<PaginationDto<OfferMobileModel>> GetAllDeleteMessage(string applicantEmail, int currentPage, string company)
        {
            var itemPerPage = 5;
            var offers = GetAllMessagesApplicantOfferses(applicantEmail);

            offers = offers.ToList();
            if (!string.IsNullOrWhiteSpace(company))
                offers = offers.Where(x => x.CompanyDto.Name == company).ToList();

            try
            {
                var data = mapper.Map<List<OfferDto>>(offers);

                var offerMobiles = await GetOfferMobilesList(data, applicantEmail);
                var count = offerMobiles.Count();
                var skip = Math.Min((currentPage - 1) * itemPerPage, count - 1);
                var filterOfferMobile = offerMobiles.Skip(skip).Take(itemPerPage).ToList();

                var result = new PaginationDto<OfferMobileModel>
                {
                    Data = filterOfferMobile,
                    PageCount = (int)Math.Ceiling(((double)count / itemPerPage)),
                    ItemsCount = count,
                    Page = currentPage
                };
                return result;
            }
            catch
            {
                return new PaginationDto<OfferMobileModel>();
            }
        }

        public async Task<List<OfferMobileModel>> GetOfferMobilesList(List<OfferDto> offerDtos, string applicantEmail)
        {
            List<OfferMobileModel> offerMobiles = new List<OfferMobileModel>();

            foreach (var item in offerDtos)
            {
                try
                {
                    var isFavourite = await IsOfferExistToOfferFavouriteList(new ApplicantOfferFavouriteDto
                    {
                        ApplicantEmail = applicantEmail,
                        OfferId = item.Id
                    });

                    var appliedjob = applicantAppliedJobsRepository.GetEntities()
                        .FirstOrDefault(x => x.ApplicantEmail == applicantEmail && x.OfferId == item.Id);

                    var isRemote = item.IsOnSite || item.IsPartialRemote;

                    var offerMobile = new OfferMobileModel()
                    {
                        Id = item.Id,
                        CompanyId = item.CompanyDto?.Id ?? 0,
                        CompanyName = item.CompanyDto?.Name ?? "",
                        JobTitle = item.JobTitle ?? "",
                        CompanyEmail = item.CompanyEmail ?? "",
                        Description = item.JobDescription ?? "",
                        City = item.CompanyDto?.CityName ?? "",
                        ExpireDate = item.ExprationDate,
                        IsRemote = isRemote,
                        DateOfOffer = item.CreateDate,
                        Logo = item.CompanyDto?.Logo ?? "",
                        PaymentStatus = item.PaymentStatus,
                        MinSalary = item.BasicSalary,
                        MaxSalary = item.UpperLimit,
                        AppliedJobId = appliedjob?.Id ?? 0,
                        knowledgeList = item.OfferKnowledgeDtos?.Select(x => x.KnowledgeDto.Name).ToList() ?? new List<string>(),
                        JobCategoryNameList = item.OfferJobCategoryDtos?.Select(x => x.JobCategoryDto.Jobcategory).ToList() ?? new List<string>(),
                        FavouriteIcon = !isFavourite
                    };

                    offerMobiles.Add(offerMobile);
                }
                catch (Exception ex)
                {

                    Console.WriteLine($"Error processing Offer ID {item.Id}: {ex.Message}");
                }
            }

            return offerMobiles;
        }
        public async Task<List<OfferMobileModel>> GetOfferMobilesListes(List<OfferDto> offerDtos, string applicantEmail)
        {
            List<OfferMobileModel> offerMobiles = new List<OfferMobileModel>();
            var isRemote = false;
            foreach (var item in offerDtos)
            {
                if (item.IsOnSite || item.IsPartialRemote)
                {
                    isRemote = true;
                }

                var isBlacklisted = await IsCompanyExistToBlackList(new ApplicantBlacklistOfCompanyDto
                {
                    ApplicantEmail = applicantEmail,
                    OfferId = item.Id
                });

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
                    PaymentStatus = item.PaymentStatus,
                    knowledgeList = item.OfferKnowledgeDtos.Select(x => x.KnowledgeDto.Name).ToList(),
                    JobCategoryNameList = item.OfferJobCategoryDtos.Select(x => x.JobCategoryDto.Jobcategory).ToList(),
                    MaxSalary = item.UpperLimit,
                    MinSalary = item.BasicSalary,
                    IsBlacklisted = !isBlacklisted
                };
                offerMobiles.Add(offerMobile);
            }
            return offerMobiles;
        }

        private async Task<bool> IsApplicantExistToCompanyFavouriteList(CompanyApplicantFavouriteDto companyApplicantFavouriteDto)
        {
            var offerExists = await companyApplicantFavouriteRepository.GetEntities().AnyAsync(x =>
                x.CompanyEmail == companyApplicantFavouriteDto.CompanyEmail && x.ApplicantEmail == companyApplicantFavouriteDto.ApplicantEmail);

            return !offerExists;
        }
        public async Task<PaginationDto<ApplicantAppliedJobDto>> GetApplicantAppliers(Guid offerId, string companyEmail, ApplicationStatus appliedType, int currentPage = 1)
        {
            var pageSize = 5;
            var offersAppliers = new List<AppliedJob>();

            if (appliedType == ApplicationStatus.AllApplications)
            {
                offersAppliers = applicantAppliedJobsRepository.GetEntities()
                    .Where(x => x.OfferId == offerId && !x.AppliedJobMessages.Any(m => m.IsDeleted))
                    .ToList();
            }
            else if (appliedType == ApplicationStatus.UnderReview)
            {
                offersAppliers = applicantAppliedJobsRepository.GetEntities()
                    .Where(x => x.OfferId == offerId && x.ApplicationStatus == ApplicationStatus.UnderReview && !x.AppliedJobMessages.Any(m => m.IsDeleted))
                    .ToList();
            }
            else if (appliedType == ApplicationStatus.Closed)
            {
                offersAppliers = applicantAppliedJobsRepository.GetEntities()
                    .Where(x => x.OfferId == offerId && x.ApplicationStatus == ApplicationStatus.Closed && !x.AppliedJobMessages.Any(m => m.IsDeleted))
                    .ToList();
            }
            else if (appliedType == ApplicationStatus.BanApplications)
            {
                offersAppliers = applicantAppliedJobsRepository.GetEntities()
                    .Where(x => x.OfferId == offerId && x.ApplicationStatus == ApplicationStatus.BanApplications && !x.AppliedJobMessages.Any(m => m.IsDeleted))
                    .ToList();
            }
            else if (appliedType == ApplicationStatus.AcceptApplications)
            {
                offersAppliers = applicantAppliedJobsRepository.GetEntities()
                    .Where(x => x.OfferId == offerId && x.ApplicationStatus == ApplicationStatus.AcceptApplications && !x.AppliedJobMessages.Any(m => m.IsDeleted))
                    .ToList();
            }

            List<ApplicantAppliedJobDto> applicantAppliedJobDtoList = new List<ApplicantAppliedJobDto>();

            foreach (var item in offersAppliers)
            {
                var isFavourite = await IsApplicantExistToCompanyFavouriteList(new CompanyApplicantFavouriteDto
                {
                    CompanyEmail = companyEmail,
                    ApplicantEmail = item.ApplicantEmail
                });
                var applicant = await GetApplicantsQuery().FirstOrDefaultAsync(x => x.Email == item.ApplicantEmail);
                var applicantDto = mapper.Map<ApplicantDto>(applicant);
                var appliedJobMessages = await appliedJobMessageRepository.GetEntities()
                    .Where(m => m.AppliedJobId == item.Id)
                    .Select(m => new AppliedJobMessageDto
                    {
                        Id = m.Id,
                        AppliedJobId = m.AppliedJobId,
                        Message = m.Message,
                        IsSeen = m.IsSeen,
                        IsDeleted = m.IsDeleted,
                        ApplicantEmail = m.AppliedJob.ApplicantEmail,
                        ClientType = m.ClientType,
                        CreateDate = m.CreateDate
                    })
                    .ToListAsync();

                applicantAppliedJobDtoList.Add(new ApplicantAppliedJobDto
                {
                    ApplicantDto = applicantDto,
                    AppliedJobId = item.Id,
                    AppliedType = item.ApplicationStatus,
                    OfferId = item.OfferId,
                    DateTime = item.CreateDate,
                    AppliedMessageDtos = appliedJobMessages,
                    FavouriteIcon = !isFavourite
                });
            }


            var count = applicantAppliedJobDtoList.Count();
            var skip = Math.Min((currentPage - 1) * pageSize, count - 1);
            var list = applicantAppliedJobDtoList.Skip(skip).Take(pageSize).ToList();

            var result = new PaginationDto<ApplicantAppliedJobDto>
            {
                Data = list,
                PageCount = (int)Math.Ceiling(((double)count / pageSize)),
                ItemsCount = count,
                Page = currentPage
            };
            return result;
        }
        #endregion

        #region prefrences 
        public async Task<List<ApplicantPreferenceDto>> GetApplicantPreferences(string applicantEmail) => await preferenceRepository.GetEntities().Select(x => new ApplicantPreferenceDto
        {
            ApplicantEmail = x.ApplicantEmail,
            Category = x.Category,
            IsSubscribed = x.IsSubscribed,
            Id = x.Id


        }).Where(c => c.ApplicantEmail == applicantEmail).ToListAsync();




        #endregion


        public async Task<int> CheckCVParser(string emailApplicant)
        {
            var listDocument = await applicantDocumentRepository.GetEntities()
                .Where(c => c.CreateDate.Date == DateTime.Today && c.ApplicantEmail == emailApplicant).ToListAsync();
            return listDocument.Count();
        }

        public async Task<bool> UpdateVerifyStatusApplicant(VerifyStatusApplicantProfileDto status)
        {
            var applicant = await profileRepository.GetEntities().FirstOrDefaultAsync(e => e.Email == status.EmailApplicant);

            if (applicant is null)
                return false;

            applicant.VerifiedByUser = status.StatusVerifyApplicant;

            profileRepository.UpdateEntity(applicant);
            await profileRepository.SaveChange();
            return true;
        }
        #region ApplicantBlacklistOfCompany
        public async Task<ApplicantBlacklistOfCompanyResult> SaveApplicantBlacklistOfCompany(ApplicantBlacklistOfCompanyDto applicantBlacklistOfCompanyDto)
        {
            if (applicantBlacklistOfCompanyDto.OfferId == Guid.Empty) return ApplicantBlacklistOfCompanyResult.Badrequest;
            if (applicantBlacklistOfCompanyDto.ApplicantEmail == null) return ApplicantBlacklistOfCompanyResult.Badrequest;
            if (await IsCompanyExistToBlackList(applicantBlacklistOfCompanyDto) == false) return ApplicantBlacklistOfCompanyResult.AddedBefore;
            var applicantBlacklistOfCompany = mapper.Map<ApplicantBlacklistOfCompany>(applicantBlacklistOfCompanyDto);

            await applicantBlacklistOfCompanyRepository.AddEntity(applicantBlacklistOfCompany);
            await applicantBlacklistOfCompanyRepository.SaveChange();
            return ApplicantBlacklistOfCompanyResult.Success;
        }

        public List<OfferDto> GetAllBlackListOfCompany(string applicantEmail)
        {
            var offers = mapper.Map<List<OfferDto>>(offerRepository.GetEntities()
                .Include(x => x.Company)
                .ThenInclude(x => x.City)
                .Include(x => x.OfferLanguages)
                .ThenInclude(x => x.Language)
                .Include(x => x.OfferKnowledges)
                .ThenInclude(x => x.Knowledge)
               .Include(x => x.OfferJobCategories)
               .ThenInclude(x => x.JobCategory)
               .Include(x => x.ApplicantBlacklistOfCompanies)
               .ThenInclude(x => x.Applicant).Where(x => x.ApplicantBlacklistOfCompanies.Any(s => s.ApplicantEmail.Equals(applicantEmail)))).ToList();
            return offers;
        }

        public async Task<bool> DeleteBlackLisCompany(Guid offerId, string applicantEmail)
        {
            if (offerId == Guid.Empty) return false;
            var applicantBlacklistOfCompany = await applicantBlacklistOfCompanyRepository.GetEntities().SingleOrDefaultAsync(x => x.OfferId == offerId && x.ApplicantEmail.Equals(applicantEmail));
            if (applicantBlacklistOfCompany == null) return false;

            applicantBlacklistOfCompanyRepository.DeleteEntity(applicantBlacklistOfCompany);
            await applicantBlacklistOfCompanyRepository.SaveChange();

            return true;

        }

        public int GetCountBlackLisOfCompany(string applicantEmail)
        {
            var count = applicantBlacklistOfCompanyRepository.GetEntities().Where(x => x.ApplicantEmail.Equals(applicantEmail)).Count();
            return count;
        }

        public List<ApplicantBlacklistOfCompanyDto> applicantBlacklistOfCompanyDtos(string applicantEmail)
        {
            var applicantBlacklistOfCompanyDtos = mapper.Map<List<ApplicantBlacklistOfCompanyDto>>(applicantBlacklistOfCompanyRepository.GetEntities());
            return applicantBlacklistOfCompanyDtos;
        }

        public async Task<ApplicantBlacklistOfCompanyDto> GetApplicantBlacklistOfCompanyDtos(string applcantEmail, Guid id)
        {
            var favouriteDto = await applicantBlacklistOfCompanyRepository.GetEntities().FirstOrDefaultAsync(x => x.ApplicantEmail == applcantEmail && x.OfferId == id);
            if (favouriteDto is not null)
            {
                return mapper.Map<ApplicantBlacklistOfCompanyDto>(favouriteDto);
            }
            else
                return new ApplicantBlacklistOfCompanyDto();


        }
        public List<CompanyDto> GetAllCompanies()
        {
            var companies = companyRepository.GetEntities().ToList();
            return mapper.Map<List<CompanyDto>>(companies);
        }

        public async Task<PaginationDto<OfferMobileModel>> GetAllBlackListOfCompanyWithPageingAsync(string applicantEmail, int currentPage, string company)
        {
            var itemPerPage = 5;
            var offers = GetAllBlackListOfCompany(applicantEmail).ToList();

            if (!string.IsNullOrWhiteSpace(company))
                offers = offers.Where(x => x.CompanyDto.Name == company).ToList();

            try
            {
                var data = mapper.Map<List<OfferDto>>(offers);
                var offerMobiles = await GetOfferMobilesListes(data, applicantEmail);
                var count = offerMobiles.Count();
                var skip = Math.Min((currentPage - 1) * itemPerPage, count - 1);
                var filterOfferMobile = offerMobiles.Skip(skip).Take(itemPerPage).ToList();

                var result = new PaginationDto<OfferMobileModel>
                {
                    Data = filterOfferMobile,
                    PageCount = (int)Math.Ceiling(((double)count / itemPerPage)),
                    ItemsCount = count,
                    Page = currentPage
                };
                return result;
            }
            catch
            {
                return new PaginationDto<OfferMobileModel>();
            }
        }
        public async Task<ResultDto<bool>> UpdateApplicantBlacklistOfCompany(List<string> companyEmails, string emailApplicant)
        {
            if (companyEmails.Equals(emailApplicant))
            {
                return new ResultDto<bool>(false, MessageCodes.Unauthorized);
            }

            var filteredCompanyEmails = companyEmails.Where(email => email != emailApplicant).ToList();

            if (filteredCompanyEmails.Count == 0)
            {
                return new ResultDto<bool>(false, MessageCodes.BadRequest);
            }


            var offers = await offerRepository.GetEntities()
                .Where(o => filteredCompanyEmails.Contains(o.CompanyEmail) && o.PaymentStatus)
                .ToListAsync();

            if (offers.Count > 0)
            {
                foreach (var offer in offers)
                {
                    offer.IsDeleted = true;
                }

                offerRepository.UpdateEntityRange(offers);
                await offerRepository.SaveChange();

                var accessCompanies = new List<ApplicantBlacklistOfCompany>();

                foreach (var offer in offers)
                {
                    var dto = new ApplicantBlacklistOfCompanyDto
                    {
                        ApplicantEmail = emailApplicant,
                        OfferId = offer.Id
                    };

                    if (await IsCompanyExistToBlackList(dto))
                    {
                        accessCompanies.Add(new ApplicantBlacklistOfCompany
                        {
                            ApplicantEmail = emailApplicant,
                            OfferId = offer.Id
                        });
                    }
                    else
                    {
                        return new ResultDto<bool>(false, MessageCodes.BadRequest);
                    }
                }

                if (accessCompanies.Count > 0)
                {
                    await applicantBlacklistOfCompanyRepository.AddEntityRange(accessCompanies);
                    await applicantBlacklistOfCompanyRepository.SaveChange();
                }
            }
            return new ResultDto<bool>(true, MessageCodes.Success);
        }
        #endregion
        public async Task<ResultDto<MessageCodes>> DeleteMessage(int id)
        {
            var applicant = await applicantAppliedJobsRepository.GetEntities().FirstOrDefaultAsync(x => x.Id == id);
            if (applicant == null)
                return new ResultDto<MessageCodes>(MessageCodes.BadRequest);

            try
            {
                applicantAppliedJobsRepository.RemoveEntity(applicant);
                await applicantAppliedJobsRepository.SaveChange();

                var favouriteOffer = await applicantOfferFavouritRepository.GetEntities()
                    .FirstOrDefaultAsync(fav => fav.OfferId == applicant.OfferId && fav.ApplicantEmail == applicant.ApplicantEmail);

                if (favouriteOffer != null)
                {
                    applicantOfferFavouritRepository.RemoveEntity(favouriteOffer);
                    await applicantOfferFavouritRepository.SaveChange();
                }

                return new ResultDto<MessageCodes>(MessageCodes.Success);
            }
            catch (Exception)
            {
                return new ResultDto<MessageCodes>(MessageCodes.UnHandleException);
            }
        }

        public async Task<bool> DeleteApplicantImage(string applicantEmail)
        {
            var applicant = await profileRepository.GetEntities().SingleOrDefaultAsync(a => a.Email.Equals(applicantEmail));
            if (applicant == null)
                return false;

            applicant.ImageFileName = null;
            applicant.ImageName = null;


            profileRepository.UpdateEntity(applicant);
            await profileRepository.SaveChange();
            return true;


        }

        public async Task<bool> ReturnToConverstion(int appliedJobId)
        {
            var applicants = await applicantAppliedJobsRepository.GetEntities()
                .Where(x => x.Id == appliedJobId)
                .ToListAsync();

            if (!applicants.Any())
                return false;
            foreach (var applicant in applicants)
            {
                applicant.IsDeleted = false;
                applicantAppliedJobsRepository.UpdateEntity(applicant);
            }
            await applicantAppliedJobsRepository.SaveChange();
            return true;
        }
        public async Task<ApplicantPrivacyDto> GetApplicantSettingsAsync(string email)
        {
            var applicantPrivacy = GetApplicantsQuery().FirstOrDefault(a => a.Email == email);

            var emailPreferences = preferenceRepository.GetEntities()
                .Where(x => x.ApplicantEmail.Equals(email)).ToList();

            var applicantSettings = new ApplicantPrivacyDto
            {
                Email = applicantPrivacy.Email,
                ProfileVisible = ProfileVisible.ForAllUsers,
                AllowSearchEngines = applicantPrivacy.AllowSearchEngines,
                ShowGender = applicantPrivacy.ShowGender,
                ShowAge = applicantPrivacy.ShowAge,
                ShowAddress = applicantPrivacy.ShowAddress,
                ShowCountryOrCity = applicantPrivacy.ShowCountryOrCity,
                ShowPhone = applicantPrivacy.ShowPhone,
                SendEmail = applicantPrivacy.SendEmail,
                NewsLetter = emailPreferences.Any(p => p.Category == "NewsLetter" && p.IsSubscribed),
                JobNotification = emailPreferences.Any(p => p.Category == "JobNotification" && p.IsSubscribed)
            };

            return applicantSettings;
        }
        public async Task<ResultDto<bool>> IsOfferExistFavouriteList(ApplicantOfferFavouriteDto applicantOffersFavouriteDto)
        {
            var offerFavourite = await applicantOfferFavouritRepository.GetEntities()
                .SingleOrDefaultAsync(x => x.OfferId == applicantOffersFavouriteDto.OfferId
                                           && x.ApplicantEmail == applicantOffersFavouriteDto.ApplicantEmail);

            if (offerFavourite != null)
                return new ResultDto<bool>(true, MessageCodes.Success);

            return new ResultDto<bool>(false, MessageCodes.Success);
        }
    }
}
