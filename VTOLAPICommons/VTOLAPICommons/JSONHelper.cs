using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace VTOLAPICommons
{
    public static class JSONHelper
    {
        public static string ToJSON(this object obj)
        {
            var options = new JsonSerializerSettings();
            options.Formatting = Formatting.Indented;
            // options.Converters.Add(new Vector3Converter());

            return JsonConvert.SerializeObject(obj, options);
        }

        public static T FromJSON<T>(string jsonString)
        {
            var options = new JsonSerializerSettings();
            // options.Converters.Add(new Vector3Converter());

            return JsonConvert.DeserializeObject<T>(jsonString, options);
        }
    }
}
