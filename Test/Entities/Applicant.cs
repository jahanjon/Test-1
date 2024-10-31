using FindJobs.Domain.Enums;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace FindJobs.DataAccess.Entities
{
    public class Applicant : BaseEntity
    {
        public int Id { get; set; }   
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public long? CityId { get; set; }
        public string Currency { get; set; }
        public string CountryCode { get; set; }
        public string CityName { get; set; }
        public string StateName { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }
        public bool HasDrivingLicense { get; set; }
        public bool HasDrivingLicenseA { get; set; }
        public bool HasDrivingLicenseB { get; set; }
        public bool HasDrivingLicenseC { get; set; }
        public bool HasDrivingLicenseD { get; set; }

        public bool? IsEuropeanUnion { get; set; }
        public bool? IsSwitzerland { get; set; }
        public bool? IsUnitedStatesofAmerica { get; set; }
        public bool? IsHourlyRate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyAverage { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyFrom { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyUntil { get; set; }
        public bool? IsOnSite { get; set; }
        public bool? IsOffSite { get; set; }
        public bool? IsPartialRemote { get; set; }
        public bool? IsFullTime { get; set; }
        public bool? IsPartTime { get; set; }
        public bool? IsFreelancer { get; set; }
        public bool? IsInternShip { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? AvailableDate { get; set; }
        public string ImageName { get; set; }
        public string ImageFileName { get; set; }
        public string ApplicantImage { get; set; }
        public bool AllowSearchEngines { get; set; }
        public bool ShowGender { get; set; } = true;
        public bool ShowAge { get; set; } = true;
        public bool ShowAddress { get; set; }
        public bool SendEmail { get; set; } = true;
        public bool ShowPhone { get; set; } = true;
        public bool ShowCountryOrCity { get; set; } = true;
        public string JobPosition { get; set; }

        public bool VerifiedByUser { get; set; } = false;

        #region Relations
        public virtual List<ApplicantBlacklistOfCompany> ApplicantBlacklistOfCompanies { get; set; }
        public virtual List<ApplicantPreference> ApplicantPreferences { get; set; }
        public virtual List<ApplicantWorkExperience> ApplicantWorkExperiences { get; set; }
        public virtual List<ApplicantEducation> ApplicantEducations { get; set; }
        public virtual List<ApplicantKnowledge> ApplicantKnowledges { get; set; }
        public virtual List<ApplicantLanguage> ApplicantLanguages { get; set; }
        public virtual List<ApplicantDocument> ApplicantDocuments { get; set; }
        public virtual List<ApplicantOfferRequest> ApplicantOfferRequests { get; set; }
        public virtual List<AppliedJob> AppliedJobs { get; set; }
        public virtual List<ApplicantOfferFavourite> ApplicantOffersFavourites { get; set; }
        public virtual List<CompanyApplicantFavourite> CompanyApplicantFavourites { get; set; }
        public virtual Country Country { get; set; }
        public virtual City City { get; set; }

        #endregion

    }
}
