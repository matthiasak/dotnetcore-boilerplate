using System;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;
using System.Linq;

internal static class Utils {

    public static string ToJSON(this Object me) => JsonConvert.SerializeObject(me);

    public static void Log(this Object me) => Console.WriteLine(me.ToJSON());

    public static Object ToErrorObject(this ModelStateDictionary d){
        return new {
            Errors = d.Values.Aggregate(
                new List<string>(),
                (acc, o) => {
                    foreach(var error in o.Errors)
                        acc.Add(error.ErrorMessage);

                    return acc;
                } 
            )
        };
    }
}