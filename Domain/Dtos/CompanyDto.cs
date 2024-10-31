using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace FindJobs.Domain.Dtos
{
    public class CompanyDto : BaseClassDto
    {
        public string Email { get; set; }
        public string Name { get; set; }

        public bool IsTop { get; set; }
        public string Logo { get; set; }
        public string CompanyRegistrationId { get; set; }
        public string VatNumber { get; set; }
        public string TaxNumber { get; set; }
        public string WebSite { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }


        #region ContactPerson
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NumberOfEmployees { get; set; }
        public string ContactPersonEmail { get; set; }
        public string ContactPersonPhone { get; set; }
        #endregion

        public string AboutCompany { get; set; }
        public IFormFile ImageLogo { get; set; }
        public byte[] ImageLogoByte { get; set; } = null;
        public string FileImageLogo { get; set; }
        public string CountryCode { get; set; }
        public string CurrencyCode { get; set; }
        public long? CityId { get; set; }

        public string CountryName { get; set; }
        public string CityName { get; set; }
        public string StateName { get; set; }
        public CityDto CityDto { get; set; }
        public List<CountryDto> CountryDtoList { get; set; }
        public string ContactCity { get; set; }
    }
    public enum SendVerificationCodeCompanyResult
    {
        Success,
    }
}
