using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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

    [DataContract]
    public class ListHistory
    {
        [DataMember]
        public string subscriber_id { get; set; }
        [DataMember]
        public string email { get; set; }
        [DataMember]
        public string newsletter_id { get; set; }
        [DataMember]
        public string newsletter_subject { get; set; }
        [DataMember]
        public string url { get; set; }
        [DataMember]
        public string date { get; set; }
        [DataMember]
        public string action { get; set; }
        [DataMember]
        public string timestamp { get; set; }
    }
}
