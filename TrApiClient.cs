using System;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using RestSharp;
using TrUtils.Extensions;

namespace TrUtils;

public class TrApiClient
{
    private readonly string mUrl;

    public TrApiClient(string baseUrl)
    {
        if (!baseUrl.EndsWith("/")) 
            baseUrl += "/";

        mUrl = baseUrl + "index.php?/api/v2/";
    }

    public string User { get; set; }

    public string Password { get; set; }

    public object SendGet(string uri, string filepath = null) => 
        SendRequest(Method.Get, uri, filepath);

    public object SendPost(string uri, object data) => 
        SendRequest(Method.Post, uri, data);

    private object? SendRequest(Method method , string uri, object data)
    {
        var url = mUrl + uri;

        var request = new RestRequest(url);
        request.AddHeader("Content-Type", "application/json");
        request.Method = method;

        var auth = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{User}:{Password}"));

        request.AddHeader("Authorization", "Basic " + auth);

        if (method == Method.Post)
        {
            if (uri.StartsWith("add_attachment"))
            {
                var filePath = (string)data;

                request.AddHeader("Content-Type", "multipart/form-data");

                request.AddFile("attachment", 
                    File.ReadAllBytes(filePath), 
                    Path.GetFileName(filePath), 
                    ContentType.Undefined);
            }
                
            else if (data != null) 
                request.AddJsonBody(data);
        }

        RestResponse? response = null;
        
        // Let it be here, we could need it in the future.
        // Console.WriteLine($"Request '{method}' - '{uri}'");

        try
        {
            for (var i = 0; i <= 2; i++)
            {
                response = new RestClient().Execute(request);
                if (!response.Content.Contains("\"error\":"))
                    break;
                if(i == 2)
                {
                    Console.WriteLine($"'{method}' '{url}' request failed. Error - '{response.Content}'");
                    return null;
                }
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            if (response.Content.IsNullOrEmpty())
            {
                Console.WriteLine($"'{method}' '{url}' returned nothing.");
                return null;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"'{method}' '{url}' request failed. Error - '{e.Message}'");
        }

        // If 'get_attachment' (but not 'get_attachments') returned valid status code, save the file
        if (response != null && (int)response.StatusCode == 200 && uri.StartsWith("get_attachment/"))
        {
            try
            {
                var path = data as string;
                var bytes = response.RawBytes;
                File.WriteAllBytes(path, bytes);
                return path;
            }
            catch (Exception ex)
            {
                throw new APIException($"Unable to save attachment - '{data as string}'", ex);
            }
        }

        JContainer result;

        if (response.Content.StartsWith("["))
                result = JArray.Parse(response.Content);
        else
            try
            {
                result = JObject.Parse(response.Content);
            }
            catch
            {
                throw new APIException($"Unable to parse response content. TestRail API returned the following: {response.Content}");
            }

        return result;
    }
}