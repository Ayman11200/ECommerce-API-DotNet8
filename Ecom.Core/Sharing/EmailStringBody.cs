
using System;

namespace Ecom.Core.Sharing
{
    public class EmailStringBody
    {
        public static string send(
            string email,
            string token,
            string component,
            string message)
        {
            string encodedEmail = Uri.EscapeDataString(email);
            string encodedToken = Uri.EscapeDataString(token);


            var isPasswordReset = string.Equals(
    component,
    "ResetPassword",
    StringComparison.OrdinalIgnoreCase
);

            var path = isPasswordReset ? "reset-password" : "active";

            var buttonText = isPasswordReset
                ? "Reset Password"
                : "Activate Account";

            string frontendUrl =
                $"http://localhost:4200/{path}?email={encodedEmail}&token={encodedToken}";

            return $@"
                <!DOCTYPE html>
                <html lang=""en"">
                <head>
                    <meta charset=""UTF-8"">
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                    <title>{System.Net.WebUtility.HtmlEncode(message)}</title>
                    <style>
                        .button {{
                            border: none;
                            border-radius: 10px;
                            padding: 15px 30px;
                            color: #fff;
                            display: inline-block;
                            background: linear-gradient(45deg, #ff7e5f, #feb47b);
                            text-decoration: none;
                            font-size: 16px;
                            font-weight: bold;
                            font-family: Arial, sans-serif;
                        }}
                    </style>
                </head>
                <body>
                    <h1>{System.Net.WebUtility.HtmlEncode(message)}</h1>
                    <hr>
                    <p>Click the button below to continue:</p>
                    <a class=""button"" href=""{frontendUrl}"">
    {buttonText}
</a>
                </body>
                </html>";
        }
    }
}
