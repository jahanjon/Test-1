function saveCompany(updateSuccessfully, notUpdate) {
	validateInput.init('company-profile');
	let Name = document.getElementById("name").value;
	let id = document.getElementById("IdCompany").value;
	let CompanyRegistrationId = document.getElementById("company-registration-id").value;
	let VatNumber = document.getElementById("vat-number").value;
	let TaxNumber = document.getElementById("tax-number").value;
	let Street = document.getElementById("address").value;
	let PostalCode = document.getElementById("postal-code").value;
	var Country = "";
	if (document.getElementById("country") != null)
		Country = document.getElementById("country").value;

	var CityName = "";
	if (document.getElementById("city") != null)
		CityName = document.getElementById("city").value;

	var StateName = "";
	if (document.getElementById("state") != null)
		StateName = document.getElementById("state").value;

	let WebSite = document.getElementById("web-site").value;

	var inputImageElement = document.getElementById("image-logo");
	var imageFile = inputImageElement.files[0];

	let Logo = document.getElementById("img-logo").src;
	let FirstName = document.getElementById("first-name").value;
	let LastName = document.getElementById("last-name").value;
	let ContactPersonEmail = document.getElementById("contact-person-email").value;
	let ContactPersonPhone = document.getElementById("contact-phone").value;
	let NumberOfEmployees = document.getElementById("number-of-employees").value;
	let AboutCompany = document.getElementById("about-company").value;

	/* ********************* validators*************************/

	if (page === 'profile') {
		if (validateInput.init('company-profile') == false) {
			return;
		}
	}

	///********************** validators *************************/

	let formData = new FormData();
	formData.append("Id", id);
	formData.append("Name", Name);
	formData.append("CompanyRegistrationId", CompanyRegistrationId);
	formData.append("VatNumber", VatNumber);
	formData.append("TaxNumber", TaxNumber);
	formData.append("Address", Street);
	formData.append("PostalCode", PostalCode);


	formData.append("WebSite", WebSite);
	formData.append("Logo", Logo);
	formData.append("ImageLogo", imageFile);
	formData.append("FirstName", FirstName);
	formData.append("LastName", LastName);
	formData.append("ContactPersonEmail", ContactPersonEmail);
	formData.append("ContactPersonPhone", ContactPersonPhone);
	formData.append("NumberOfEmployees", NumberOfEmployees);
	formData.append("AboutCompany", AboutCompany);
	formData.append("CountryCode", Country);
	formData.append("CityName", CityName);
	formData.append("StateName", StateName);
	fetch("SaveCompany", {
		method: "post",
		body: formData
	}).then((data) => { return data.text() })
		.then((data) => {

			var result = JSON.parse(data);

			if (result.messageCode == 2) {
				MessageShow("Error", notUpdate, "error");
				return false;
			}
			if (result.messageCode == 0) {
				if (Logo != null) {
					var imageLogo = document.getElementById("image-user-id");
					imageLogo.src = Logo;
				}
				MessageShow("Success", updateSuccessfully, "success");
				window.location.href = redirectUrlAddJobOffer;
			}
		});
	return false;
};
