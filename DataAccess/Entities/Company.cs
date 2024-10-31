using System.Collections.Generic;

namespace FindJobs.DataAccess.Entities
{
    public class Company : BaseEntity
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Logo { get; set; }
        public string FileImageLogo { get; set; }
        public bool IsTop { get; set; }
        public string CompanyRegistrationId { get; set; }
        public string VatNumber { get; set; }
        public string TaxNumber { get; set; }
        public string WebSite { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }
        public string CountryCode { get; set; }
        public string CityName { get; set; }
        public string StateName { get; set; }
        public long? CityId { get; set; }

        #region ContactPerson
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NumberOfEmployees { get; set; }
        public string ContactPersonEmail { get; set; }
        public string ContactPersonPhone { get; set; }
        #endregion

        public string AboutCompany { get; set; }

        #region relations
        public Country Country { get; set; }
        public City City { get; set; }
        public List<CompanyApplicantFavourite> CompanyApplicantFavourites { get; set; }
        #endregion

    }
}
