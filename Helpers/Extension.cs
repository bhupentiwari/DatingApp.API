using System;
using Microsoft.AspNetCore.Http;

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
    }
}