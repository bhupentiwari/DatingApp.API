using System;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DatingApp.API.Helpers
{
    public static class Extension
    {
        public static void AddApplicationError(this HttpResponse res,string message){

            res.Headers.Add("Application-Error",message);

            res.Headers.Add("Access-Control-Expose-Headers","Application-Error");
            
            res.Headers.Add("Access-Control-Allow-Origin","*");

        }

        public static int CalculateAge( this DateTime theDate){
                var age = DateTime.Today.Year - theDate.Year;
                if(theDate.AddYears(age) > DateTime.Today)
                    --age;
                return age;
        }

        public static void AddPagination(this HttpResponse res,int currentPage,int itemsPerPage,int totalItems,int totalPages){
            
            var paginationHeader = new PaginationHeader(currentPage,itemsPerPage,totalItems,totalPages);
            var camelCaseFormatter = new JsonSerializerSettings();
            camelCaseFormatter.ContractResolver = new  CamelCasePropertyNamesContractResolver();
            res.Headers.Add("Pagination",JsonConvert.SerializeObject(paginationHeader,camelCaseFormatter));
            res.Headers.Add("Access-Control-Expose-Headers", "Pagination");
        }
    }
}