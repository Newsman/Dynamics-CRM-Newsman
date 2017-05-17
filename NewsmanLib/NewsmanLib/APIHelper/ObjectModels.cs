using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewsmanLib.APIHelper
{
    public class Subscriber
    {
        public string Email
        { get; set; }

        public string Firstname
        { get; set; }

        public string Lastname
        { get; set; }

    }

    public class MarketingListInfo
    {
        public string ListName
        { get; set; }
        public string NewsmanSegmentId
        { get; set; }

        public string ListTargetType
        { get; set; }
    }

    public class NewsmanSegment
    {
        public string segment_name { get; set; }
        public string segment_id { get; set; }
        public string count { get; set; }
        public string status { get; set; }
    }

    public class NewsmanList
    {
        public string list_name { get; set; }
        public string list_id { get; set; }
    }
}
