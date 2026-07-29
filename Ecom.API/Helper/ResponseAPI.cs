
namespace Ecom.API.Helper
{
    public class ResponseAPI
    {
        public int statuscode { get; set; }
        public string? Message { get; set; }

        public ResponseAPI(int statuscode, string? Message = null)
        {
            this.statuscode = statuscode;
            this.Message = Message ?? GetMessageFormStatusCode(statuscode);
        }

        private string GetMessageFormStatusCode(int statuscode)
        {

            return statuscode switch
            {
                200 => "Done",
                201 => "Created",
                400 => "Bad Request",
                401 => "Un Authorized",
                404 => "resource not found",
                500 => "server Error",
                _ => null,
            };
        }

    }
}
