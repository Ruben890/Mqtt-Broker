using Shared.Request;
using System.Net;

namespace Shared.Response
{
    public class BaseResponse
    {
        public string? Message { get; set; } = null!;
        public HttpStatusCode StatusCode { get; set; }
        public object? Details { get; set; } = null!;
        public Pagination? Pagination { get; set; } = null!;

        public void SetPagination(Pagination pagination)
        {
            Pagination = new Pagination
            {
                CurrentPage = pagination.CurrentPage,
                TotalPages = pagination.TotalPages,
                PreviousPage = pagination.PreviousPage,
                NextPage = pagination.NextPage,
                TotalCount = pagination.TotalCount,
                PageSize = pagination.PageSize
            };
        }

        public static BaseResponse HandleCustomResponse(
            string? message,
            HttpStatusCode? statusCode = HttpStatusCode.BadRequest,
            object? details = null,
            Pagination? pagination = null)
        {

            var response = new BaseResponse
            {
                Message = message != null ? SupportedLanguages.GetMessage(message) : null,
                StatusCode = statusCode ?? HttpStatusCode.BadRequest,
                Details = details
            };

            if (pagination != null)
                response.SetPagination(pagination);

            return response;
        }

    }
}
