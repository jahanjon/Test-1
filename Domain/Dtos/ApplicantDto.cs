using FindJobs.Domain.Enums;
using FindJobs.Domain.Constants;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using FindJobs.Domain.ViewModels;

namespace FindJobs.Domain.Dtos
{
    public class ApplicantDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public long? CityId { get; set; }
        public string CityName { get; set; }
        public string Currency { get; set; }
        public string CountryCode { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }
        public bool HasDrivingLicense { get; set; }
        public bool HasDrivingLicenseA { get; set; }
        public bool HasDrivingLicenseB { get; set; }
        public bool HasDrivingLicenseC { get; set; }
        public bool HasDrivingLicenseD { get; set; }
        public bool IsEuropeanUnion { get; set; } = false;
        public bool IsSwitzerland { get; set; } = false;
        public bool IsUnitedStatesofAmerica { get; set; } = false;
        public bool IsHourlyRate { get; set; } = false;
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyAverage { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyFrom { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyUntil { get; set; }
        public bool IsOnSite { get; set; } = false;
        public bool IsOffSite { get; set; } = false;
        public bool IsPartialRemote { get; set; } = false;
        public bool IsFullTime { get; set; } = false;
        public bool IsPartTime { get; set; } = false;
        public bool IsFreelancer { get; set; } = false;
        public bool IsInternShip { get; set; } = false;
        public RateType RateType { get; set; }
        public Gender? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? AvailableDate { get; set; }
        public ReadyToWork ReadyToWorkStatus { get; set; }
        public string ApplicantImageValue { get; set; }
        public string CountryName { get; set; }
        public string StateName { get; set; }
        public string CityMainName { get; set; }
        public string ImageName { get; set; }
        public string ImageFileName { get; set; }
        public string ApplicantImage
        {
            get
            {
                if (ApplicantImageValue == null)
                {
                    return Gender != null && Gender.Value == Enums.Gender.Female ? Images.FemaleImage : Images.MaleImage;
                }
                else
                {
                    return "data:image/jpeg;base64," + ApplicantImageValue;
                }
            }
        }
        public ProfileVisible ProfileVisible { get; set; }
        public bool AllowSearchEngines { get; set; }
        public bool ShowGender { get; set; }
        public bool ShowAge { get; set; }
        public bool ShowAddress { get; set; }
        public bool ShowPhone { get; set; }
        public bool ShowCountryOrCity { get; set; }
        public bool SendEmail { get; set; }
        public string JobPosition { get; set; }

        public bool VerifiedByUser { get; set; }

        public int LimiteCVParser { get; set; }

        public virtual List<ApplicantPreferenceDto> ApplicantPreferences { get; set; }
        public virtual List<ApplicantWorkExperienceDto> ApplicantWorkExperiences { get; set; }
        public virtual List<ApplicantEducationDto> ApplicantEducations { get; set; }
        public virtual List<ApplicantKnowledgeDto> ApplicantKnowledges { get; set; }
        public virtual List<ApplicantLanguageDto> ApplicantLanguages { get; set; }
        public virtual List<ApplicantDocumentDto> ApplicantDocuments { get; set; }
        public virtual List<AppliedJobMessageDto> AppliedJobMessage { get; set; }

        public CountryDto Country { get; set; }
        public CityDto City { get; set; }
    }
}
