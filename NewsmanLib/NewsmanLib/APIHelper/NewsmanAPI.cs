﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace NewsmanLib.APIHelper
{
    public class NewsmanAPI : IDisposable
    {
        public WebClient _client;
        public string _baseUrl;

        /// <summary>
        /// rest api url - RestAPI + userid + apikey + /namespace.method.json
        /// </summary>
        public NewsmanAPI(string ak, string uid)
        {
            _client = new WebClient();
            _baseUrl = $"https://ssl.newsman.ro/api/1.2/rest/{uid}/{ak}";
        }

        public List<NewsmanList> RetrieveLists()
        {
            try
            {
                var response = _client.DownloadString($"{_baseUrl}/list.all.json");

                JavaScriptSerializer js = new JavaScriptSerializer();
                return js.Deserialize<List<NewsmanList>>(response);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"{_baseUrl}/list.all.json", ex);
            }
        }

        public string RetrieveListsJson()
        {
            try
            {
                return _client.DownloadString($"{_baseUrl}/list.all.json");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

        public List<NewsmanSegment> RetrieveSegments(string list_id)
        {
            try
            {
                var response = _client.DownloadString($"{_baseUrl}/segment.all.json?list_id={list_id}");

                JavaScriptSerializer js = new JavaScriptSerializer();
                return js.Deserialize<List<NewsmanSegment>>(response);
            }
            catch { throw; }
        }

        public string RetrieveSegmentsJson(string list_id)
        {
            try
            {
                return _client.DownloadString($"{_baseUrl}/segment.all.json?list_id={list_id}");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string CreateSegment(string list_id, string segment_name)
        {
            try
            {
                var response = _client.UploadString($"{_baseUrl}/segment.create.json?list_id={list_id}&segment_name={segment_name}", "");
                return response;
            }
            catch { throw; }
        }

        public string ImportSubscribers(string list_id, string segment, List<Subscriber> subscribers)
        {
            StringBuilder sb = new StringBuilder("email,firstname,lastname");
            sb.AppendLine();
            foreach (Subscriber s in subscribers)
            {
                sb.AppendLine($"\"{s.Email}\",\"{s.Firstname}\",\"{s.Lastname}\"");
            }

            string responsebody = string.Empty;
            var reqparm = new System.Collections.Specialized.NameValueCollection();
            reqparm.Add("list_id", list_id);
            reqparm.Add("segments", "[" + segment + "]");
            reqparm.Add("csv_data", sb.ToString());

            byte[] responsebytes = _client.UploadValues($"{_baseUrl}/import.csv.json", "POST", reqparm);
            responsebody = Encoding.UTF8.GetString(responsebytes);

            return responsebody;
        }

        public string ImportSubscribers(string list_id, string segment, Subscriber s)
        {
            List<Subscriber> subs = new List<Subscriber>();
            subs.Add(s);

            return ImportSubscribers(list_id, segment, subs);
        }

        public string ImportStatus(string import_id)
        {
            return _client.UploadString($"{_baseUrl}/import.status.json?import_id={import_id}", "");
        }

        public List<ListHistory> RetrieveListHistory(string list_id, int count, string timestamp)
        {
            StringBuilder trace = new StringBuilder();
            List<ListHistory> data = new List<ListHistory>();
            try
            {
                var response = _client.DownloadString($"{_baseUrl}/list.subscribersHistory.json?list_id={list_id}&count={count}&before_timestamp={timestamp}");
                trace.AppendLine("downloading string..");
                using (MemoryStream ms = new MemoryStream())
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<ListHistory>));
                    StreamWriter writer = new StreamWriter(ms);
                    writer.Write(response);
                    writer.Flush();

                    ms.Position = 0;
                    data = (List<ListHistory>)serializer.ReadObject(ms);
                }
                trace.AppendLine("return response");
                return data;
            }
            catch (Exception e)
            {
                throw new Exception($"URI: {_baseUrl}/list.subscribersHistory.json?list_id={list_id}&count={count}&before_timestamp={timestamp}; trace: {trace.ToString()}; message: {e.Message}", e);
            }
        }

        #region Interface
        public void Dispose()
        {
            ((IDisposable)_client).Dispose();
        }
        #endregion
    }

    public static class NewsmanDefaults
    {
        public static string NewsletterLink(string list_id, string newsletter_id)
        {
            return $"https://ssl.newsman.ro/admin/newsletter/details/list_id/{list_id}/newsletter_id/{newsletter_id}";
        }
    }
}
