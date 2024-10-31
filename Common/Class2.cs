using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public enum MessageCodes
    {
        Success = 0,
        DbConnectionError = 1,
        UnHandleException = 2,
        CaptchaNotValid = 3,
        VerificationCodeNotValid = 4,
        BadRequest = 5,
        Unauthorized = 6,
        CannotSendEmail = 7,
        PaymentSuccess = 100,
        PaymentFailure = 101,
        AlreadyPaid = 102,
        CanNotFindOrder = 103,
        NoConnection = 104,
        NoOrder = 105,
        ThisPaymentHasAlreadyBeenProcessed = 106,
        WeAreUnableToProcessYourPayment = 107,
        CompanyNameIsRequired = 200,
        VatIdIsNotValid = 201,
        VatIdIsNotMatch = 202
    }
}
