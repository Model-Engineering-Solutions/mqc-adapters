using System;
using System.Collections.Generic;
using System.Linq;
using MES.MQC.DataSourceLibrary.Adapters;
using MES.MQC.DataSourceLibrary.Models.Adapters;
using MES.MQC.UtilityLibrary.Form;
using MES.MQC.UtilityLibrary.Form.Field;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using RestSharp;

// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable UnusedMember.Local
namespace MES.MQC.CustomAdapters
{
    public class ExampleApiConnector : ApiConnector
    {
        /// <summary>
        ///   Unique Name of the Adapter.
        /// </summary>
        public override string Name => "Example API";

        /// <summary>
        ///   Description of the adapter that is visible in the adapter dialog as a popover.
        ///   If the adapter is an API Connector, the description is also shown in the Add/Edit DataSource dialog,
        ///   when the API Connector is selected.
        ///   Absolute links get transformed into HTML link tags,
        ///   Linebreaks (\n) get transformed into HTML linebreaks (<![CDATA[<br>]]>),
        ///   HTML Tags are not allowed.
        /// </summary>
        public override string Description => "This is an example API adapter";

        /// <summary>
        ///   CheckAvailable must be implemented by the Api Adapter class.
        ///   This method is called to show a warning for configured but unavailable data sources in the UI.
        /// 
        ///   In this example adapter, a quote request with a limit of 1 is made to check if it is successful.
        /// </summary>
        /// <param name="context">ApiConnectorContext</param>
        /// <returns>boolean (is available/accessible?)</returns>
        public override bool CheckAvailable(ApiConnectorContext context)
        {
            var client = GetClient(context);
            if (client == null)
            {
                return false;
            }

            var quoteRequest = new RestRequest("quotes/en")
                .AddParameter("limit", 1);

            return client.Get(quoteRequest).IsSuccessful;
        }

        /// <summary>
        ///   CheckModified must be implemented by the Api Adapter class.
        ///   This method is called by the local and server side monitoring
        ///   to check if the data sources should be updated.
        ///
        ///   In this example adapter, the data source will never provide new data, so it always return false.
        /// </summary>
        /// <param name="context">ApiConnectorContext</param>
        /// <returns>boolean (are there any changes?)</returns>
        public override bool CheckModified(ApiConnectorContext context)
        {
            return false;
        }

        /// <summary>
        ///   Read can be implemented by the Api Adapter class.
        ///   At least one of Download and Read must be implemented.
        ///   The implementation of this method should access the configured API, create AdapterData,
        ///   AdapterMeasures and/or AdapterFindings and return a AdapterReadResult.
        /// 
        ///   In this example adapter, the quotes are read and filtered by author.
        ///   Data is then created for the number of quotes per author and a finding is created for each quote.
        /// </summary>
        /// <param name="context">ApiConnectorContext</param>
        /// <returns>AdapterReadResult</returns>
        protected override AdapterReadResult Read(ApiConnectorContext context)
        {
            if (!(context.Configuration is AdapterConfiguration configuration))
            {
                return null;
            }

            var client = GetClient(context);
            if (client == null)
            {
                return null;
            }

            var quoteRequest = new RestRequest("quotes/en");
            var response = client.Get(quoteRequest);

            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
            {
                return null;
            }

            var result = new AdapterReadResult();
            var quotes = JsonConvert.DeserializeObject<Quote[]>(response.Content);

            var quotesByAuthors = new Dictionary<string, int>();
            foreach (var quote in quotes)
            {
                var skip = false;
                foreach (var authorFilter in configuration.AuthorFilters)
                {
                    var match = string.IsNullOrEmpty(authorFilter.Regex) || 
                                new Regex(authorFilter.Regex, RegexOptions.IgnoreCase).Match(quote.Author.Name).Success;

                    if (authorFilter.Apply && !match)
                    {
                        skip = true;
                        break;
                    } 
                    else if (!authorFilter.Apply && match)
                    {
                        skip = true;
                        break;
                    }
                }

                if (skip)
                {
                    continue;
                }

                if (!quotesByAuthors.ContainsKey(quote.Author.Name))
                {
                    quotesByAuthors[quote.Author.Name] = 0;
                }
                quotesByAuthors[quote.Author.Name]++;
            }

            var dateTime = DateTime.Now;
            foreach (var quoteByAuthor in quotesByAuthors)
            {
                result.Data.Add(new AdapterData
                {
                    DateTime = dateTime,
                    ArtifactPath = quoteByAuthor.Key,
                    Value = quoteByAuthor.Value,
                    DataSourceName = "Fisenko",
                    MeasurementName = "English",
                    MeasureName = "Quotes",
                    VariableName = "Count",
                });
            }

            if (!context.ImportFindings)
            {
                return result;
            }
            
            foreach (var quote in quotes)
            {
                var finding = new AdapterFinding
                {
                    DateTime = dateTime,
                    ArtifactPath = quote.Author.Name,
                    DataSourceName = "Fisenko",
                    MeasurementName = "English",
                    Description = quote.Text,
                    State = "Available",
                    SubjectType = "Quote",
                    SubjectPath = new []{"Fisenko", "English"}
                    
                };
                finding.AddData(result.Data.Where(x => x.ArtifactPath == quote.Author.Name));

                result.Findings.Add(finding);
            }

            return result;
        }

        /// <summary>
        ///   ConfigureForm can be implemented by an API Connector.
        ///   It is used to modify the FormSchema of the Add/Edit DataSource dialog for the 
        ///   AdapterConfiguration depending on the values entered
        ///   (e.g. to make options or input fields visible and/or deactivate them based on a correct API request).
        ///   It is called when the FormSchema is first created with all fields as modified and when
        ///   a field that was enabled by RefreshOnModified has been changed by the user.
        ///   This method should also validate the configuration, not if the schema is valid, but if the
        ///   values are usable (e.g. if the api is accessible and the authentification is valid).
        ///
        ///   In this example adapter, there is no modification of the FormSchema.
        ///   The Url is validated to be on specific url, that of fisenko public api.
        /// </summary>
        /// <param name="form">IForm, the AdapterConfiguration</param>
        /// <param name="modifiedFields">
        ///   Array of the modified fields as string
        ///   (Form-Subfields as field.field or field.index.field)
        /// </param>
        /// <returns>Array of FormError, if a validation failed</returns>
        public override FormError[] ConfigureForm(IForm form, string[] modifiedFields)
        {
            if (!(form is AdapterConfiguration configuration))
            {
                return null;
            }

            var formErrors = new List<FormError>();

            if (configuration.Url != "https://api.fisenko.net")
            {
                formErrors.Add(new FormError
                {
                    Message = "Url has to be https://api.fisenko.net",
                    Description = "Only the Fisenko Quotes API is supported"
                });
            }

            return formErrors.ToArray();
        }

        /// <summary>
        ///   PreviewForm can be implemented by an API adapter.
        ///   It is used to load the Preview, if one was defined in the AdapterConfiguration.
        ///   Based on the focused form field, the Preview is shown in the Dialog and provides the ability to load
        ///   by clicking on a button. This button click calls this method.
        ///   Based on the configuration and the preview type, the api should load the relevant data,
        ///   limited to ~100 for better performance, and a array of FormPreview should be returned.
        /// 
        ///   In this example adapter, there are two previews, one for Quotes and one for Authors.
        ///   The Author preview is shown for the AuthorFilter (see AdapterConfiguration).
        /// </summary>
        /// <param name="context">ApiConnectorContext</param>
        /// <param name="preview">The preview type (string)</param>
        /// <param name="totalCount">Output of totalCount, if 0 shown as ? in the preview dialog</param>
        /// <returns>Array of FormPreview, to show Title and optionally Description and DateTime</returns>
        public override FormPreview[] GetPreview(ApiConnectorContext context, string preview, out int totalCount)
        {
            totalCount = 0;
            if (!(context.Configuration is AdapterConfiguration configuration))
            {
                return null;
            }

            var client = GetClient(configuration);
            if (client == null)
            {
                return null;
            }

            var previews = new List<FormPreview>();
            switch (preview)
            {
                case "Quotes":
                {
                    var quoteRequest = new RestRequest("quotes/en")
                        .AddParameter("limit", 100);
                    var response = client.Get(quoteRequest);

                    if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
                    {
                        return null;
                    }

                    var quotes = JsonConvert.DeserializeObject<Quote[]>(response.Content);
                    totalCount = quotes.Length == 100 ? 0 : quotes.Length;

                    foreach (var quote in quotes)
                    {
                        var skip = false;
                        foreach (var authorFilter in configuration.AuthorFilters)
                        {
                            var match = string.IsNullOrEmpty(authorFilter.Regex) || 
                                        new Regex(authorFilter.Regex, RegexOptions.IgnoreCase).Match(quote.Author.Name).Success;

                            if (authorFilter.Apply && !match)
                            {
                                skip = true;
                                break;
                            } 
                            else if (!authorFilter.Apply && match)
                            {
                                skip = true;
                                break;
                            }
                        }

                        if (skip)
                        {
                            continue;
                        }

                        previews.Add(new FormPreview
                        {
                            Title = quote.Author.Name,
                            Description = quote.Text
                        });
                    }
                    break;
                }
                case "Authors":
                {
                    var quoteRequest = new RestRequest("quotes/en")
                        .AddParameter("limit", 100);
                    var response = client.Get(quoteRequest);

                    if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
                    {
                        return null;
                    }

                    var quotes = JsonConvert.DeserializeObject<Quote[]>(response.Content);
                    var authors = new HashSet<string>();
                    foreach (var quote in quotes)
                    {
                        authors.Add(quote.Author.Name);
                    }

                    totalCount = authors.Count == 100 ? 0 : authors.Count;
 
                    foreach (var author in authors)
                    {
                        var skip = false;
                        foreach (var authorFilter in configuration.AuthorFilters)
                        {
                            var match = string.IsNullOrEmpty(authorFilter.Regex) || 
                                        new Regex(authorFilter.Regex, RegexOptions.IgnoreCase).Match(author).Success;

                            if (authorFilter.Apply && !match)
                            {
                                skip = true;
                                break;
                            } 
                            else if (!authorFilter.Apply && match)
                            {
                                skip = true;
                                break;
                            }
                        }

                        if (skip)
                        {
                            continue;
                        }

                        previews.Add(new FormPreview
                        {
                            Title = author
                        });
                    }
                    break;
                }
            }

            return previews.ToArray();
        }

        /// <summary>
        ///   A resuable method to create a RestClient is recommended.
        ///   This method calls the real method with just the configuration.
        /// </summary>
        /// <param name="context">ApiConnectorContext</param>
        /// <returns>RestClient</returns>
        protected RestClient GetClient(ApiConnectorContext context)
        {
            if (!(context.Configuration is AdapterConfiguration configuration))
            {
                return null;
            }

            return GetClient(configuration);
        }

        /// <summary>
        ///   A resuable method to create a RestClient is recommended.
        ///   This method only creates a client, if the necessary configuration has been supplied.
        ///   The validation of the required fields is not needed and done by the Form UI.
        ///   When creating a RestClient a Timeout should be supplied.
        /// </summary>
        /// <param name="configuration">AdapterConfiguration</param>
        /// <returns>RestClient</returns>
        protected RestClient GetClient(AdapterConfiguration configuration)
        {
            if (string.IsNullOrEmpty(configuration.Url) ||
                !Uri.TryCreate(configuration.Url, UriKind.Absolute, out var uriResult)
                || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                return null;
            }

            try
            {
                // https://restsharp.dev/docs/category/using-restsharp
                return new RestClient(new RestClientOptions(configuration.Url + "/v1/")
                {
                    Timeout = new TimeSpan(0, 0, 10) // max 10 sec for a request
                });
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///   Private class for use inside this adapter.
        /// </summary>
        private class Quote
        {
            public string Id { get; set; }
            
            public string Text { get; set; }
            
            public QuoteAuthor Author { get; set; }
        }

        /// <summary>
        ///   Private class for use inside this adapter.
        /// </summary>
        private class QuoteAuthor
        {
            public string Id { get; set; }
            
            public string Name { get; set; }
        }

        /// <summary>
        ///   Adapter Configuration with Form UI attributes for the class and properties.
        ///   This class defines how the data source of this API Connector can be configured.
        /// </summary>
        [FormClass(Preview = "Quotes")]
        public class AdapterConfiguration : ApiConnectorConfiguration
        {
            [JsonProperty(Required = Required.Always)]
            [Input(InputType = InputType.Url, Help = "Example API Url (Fisenko Quotes)", RefreshOnModified = true)]
            public string Url { get; set; }

            [Forms(DisplayMode = FormsDisplayMode.Compact, TitleFields = new[] { "Regex" },
                MinItems = 1, Label = "Author Filters", AddLabel = "Add Filter", Preview = "Authors")]
            public AuthorFilter[] AuthorFilters { get; set; } = { new AuthorFilter() };
        }

        /// <summary>
        ///   Class used inside the Adapter Configuration.
        /// </summary>
        [FormClass(Preview = "Authors")]
        public class AuthorFilter
        {
            [Switch(SuccessDangerColoring = true, CheckedIcon = "plus", UncheckedIcon = "minus",
                CheckedTooltip = "Include", UncheckedTooltip = "Exclude")]
            public bool Apply { get; set; } = true;

            [Input(InputType = InputType.Regex, Label = "RegEx",
                Help = "Regular Expression, leave empty if everything should be matched.")]
            public string Regex { get; set; } = "";
        }

        /// <summary>
        ///   Adapter Options for this Adapter.
        ///   Configuration for the Adapter independent of the data source.
        /// </summary>
        public class ExampleApiConnectorOptions : ApiConnectorOptions
        {
        }
    }
}