using Newtonsoft.Json.MediaTypeFormatter.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Formatting;
using System.Text;

namespace Newtonsoft.Json.MediaTypeFormatter.Configurations
{
    public class MediaTypeFormatterConfig
    {
        public static void RegisterJsonNetMediaTypeFormatter(MediaTypeFormatterCollection formatters)
        {
            formatters.Remove(formatters.OfType<JsonMediaTypeFormatter>().FirstOrDefault());
            formatters.Add(new JsonNetMediaTypeFormatter());
        }
    }
}
