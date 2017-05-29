using NewsmanLib.APIHelper;
using System.Collections.Generic;

namespace NewsmanLibConsole
{
    class Program
    {
        private const string apikey = "demo";
        private const string userid = "demo";

        static void Main(string[] args)
        {
            using (NewsmanAPI api = new NewsmanAPI(apikey, userid))
            {
                string defaultList = "2896";

                var lists = api.RetrieveLists();
                var segments = api.RetrieveSegments(defaultList);

                //var newSegment = api.CreateSegment(defaultList, "segment din consola");

                List<Subscriber> subscribers = new List<Subscriber>();
                subscribers.Add(new Subscriber { Email = "abcd@nubiz.com", Firstname = "ab", Lastname = "c" });
                subscribers.Add(new Subscriber { Email = "bcde@nubiz.com", Firstname = "bc", Lastname = "d" });
                subscribers.Add(new Subscriber { Email = "defg@nubiz.com", Firstname = "de", Lastname = "f" });
                var resp = api.ImportSubscribers(defaultList, "65399", subscribers);

                var status = api.ImportStatus(resp.Replace("\"", ""));
            }
        }
    }
}
