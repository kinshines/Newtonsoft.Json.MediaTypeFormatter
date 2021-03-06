# Newtonsoft.Json.MediaTypeFormatter
A MediaTypeFormatter for use with ASP.NET Web API to ensure (de)serialization of derived classes through the wire


`JsonNetMediaTypeFormatter` is meant to replace the default `JsonMediaTypeFormatter` that Web API uses out of the box to help remedy this use case.


### How to use

The assembly contains `Newtonsoft.Json.MediaTypeFormatter.Configurations.MediaTypeFormatterConfig` which you can use to register the included `JsonNetMediaTypeFormatter`. A good place to do this would be in your application's `Global.asax`.

``` csharp

protected void Application_Start() 
{
    // other prep stuff
	
	MediaTypeFormatterConfig.RegisterJsonNetMediaTypeFormatter(GlobalConfiguration.Configuration.Formatters);
}
```

This removes the default `JsonMediaTypeFormatter` and adds in the included `JsonNetMediaTypeFormatter`.

If you'd prefer, `JsonMediaTypeFormatter` is also exposed, so you can perform the swap using your own method.
