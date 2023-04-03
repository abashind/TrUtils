using System.Linq;
using Newtonsoft.Json.Linq;
using TrUtils.Extensions;

namespace TrUtils.TestRailEntities;

public abstract class TrBaseClass
{
    protected static TrApiClient TrClient => new(Parameters.TrServer)
    {
        User = Parameters.TestrailUserName,
        Password = Parameters.TestrailApiToken
    };

    protected static JArray GetTestRailItemsWithPagination(string url, string itemsNodeName)
    {
        var result = new JArray();

        while (true)
        {
            var response = (JObject)TrClient.SendGet(url);
            switch (response)
            {
                case null when result.Any():
                    return result;
                case null:
                    return null;
            }

            var itemsNode = response[itemsNodeName];

            result.Merge(itemsNode);

            var next = response["_links"]["next"].ToString();
            if (next.IsNullOrEmpty())
                break;

            if (url.Contains('&'))
                url = url.Remove(url.IndexOf('&'));

            url += next[next.IndexOf('&')..];
        }

        return result;
    }
}