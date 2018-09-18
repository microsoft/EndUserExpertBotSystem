// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using ModernApps.CommunityBot.Common.DataProviders.QnAMaker;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ModernApps.CommunitiyBot.Common.Providers
{
    [Serializable]
    public class QnAMakerProvider : IQnAMakerProvider
    {
        private string knowledgebaseId;
        private string qnamakerSubscriptionKey;
        private string qnamakerManagmentKey;
        private double scoreThreshold;
        private Uri qnamakerUriBase;
        private Uri qnamakerManagementUriBase;

        public QnAMakerProvider(string qnaMakerRootUrl, string qnaMakerManagmentUri, string knowledgebaseId, string qnamakerSubscriptionKey, string qnamakerManagmentKey, double scoreThreshold)
        {
            this.knowledgebaseId = knowledgebaseId;
            this.qnamakerSubscriptionKey = qnamakerSubscriptionKey;
            this.qnamakerManagmentKey = qnamakerManagmentKey;
            this.scoreThreshold = scoreThreshold;
            qnamakerUriBase = new Uri(qnaMakerRootUrl);
            qnamakerManagementUriBase = new Uri(qnaMakerManagmentUri);
        }

        public async Task<IQnaResponse> GetQandAResponse(string query)
        {
            var url = $"{qnamakerUriBase}/knowledgebases/{knowledgebaseId}/generateAnswer";

            var result = await SendQnAMakerRequest(GetQueryObject(query), HttpMethod.Post, url);
            if (result.IsSuccessStatusCode && result.Content != null)
            {
                var qnaResponse = JsonConvert.DeserializeObject<QandAResponse>(await result.Content.ReadAsStringAsync(), new JsonSerializerSettings()
                {
                    Culture = CultureInfo.InvariantCulture,
                    Converters = new JsonConverter[] { new HtmlEncodingConverter() }
                });
                qnaResponse.Threshold = scoreThreshold;
                qnaResponse.Question = query;
                return qnaResponse;
            }
            return null;
        }

        private object GetQueryObject(string query)
        {
            return new
            {
                question = query,
            };
        }

        public async Task<bool> StoreNewAnswer(string question, string answer)
        {
            var inputObject = new QnAMakerUpdateKBInput();
            inputObject.Add.QnaPairs.Add(new QnAMakerUpdateKBInput.QnaDto()
            {
                questions = new List<string>() { question },
                answer = answer
            });
            return await SendUpdateKbRequest(inputObject);
        }


        public async Task<bool> ReplaceAnswer(string question,string answer, IQnaResponse qnaResponse)
        {
            var url = $"{qnamakerManagementUriBase}/knowledgebases/{knowledgebaseId}/Prod/qna ";
            var response = await SendQnAMakerManagementRequest(HttpMethod.Get, url);
            QnaDbResponse kb = null;

            if (response.IsSuccessStatusCode)
            {
                kb = JsonConvert.DeserializeObject<QnaDbResponse>(await response.Content.ReadAsStringAsync());
            }

            if (kb != null)
            {
                var qnaPair = kb.qnaDocuments.FirstOrDefault(x => x.answer == qnaResponse.Answer && x.questions.Contains(question));

                if (qnaPair != null)
                {

                    var inputObject = new QnAMakerUpdateKBInput();
                    inputObject.Update.qnaList = new List<QnAMakerUpdateKBInput.QnaUpdateDto>()
            {
                new QnAMakerUpdateKBInput.QnaUpdateDto()
                {
                id = qnaPair.id,
                answer = answer,
                }
            };
                    return await SendUpdateKbRequest(inputObject);
                }
            }
            return false;
        }

        private async Task<bool> SendUpdateKbRequest(QnAMakerUpdateKBInput inputObject)
        {
            var url = $"{qnamakerManagementUriBase}/knowledgebases/{knowledgebaseId}";
            var response = await SendQnAMakerManagementRequest(inputObject, new HttpMethod("PATCH"), url);
            if (response.IsSuccessStatusCode)
            {
                var status = JsonConvert.DeserializeObject<OperationStatus>(await response.Content.ReadAsStringAsync());

                while (status.operationState != "Succeeded")
                {
                    Thread.Sleep(500);
                    var statusResponse = await SendQnAMakerManagementRequest(HttpMethod.Get, $"{qnamakerManagementUriBase}/operations/{status.operationId}");
                    if (statusResponse.IsSuccessStatusCode)
                    {
                        var content = JsonConvert.DeserializeObject<OperationStatus>(await statusResponse.Content.ReadAsStringAsync());
                        status = content;
                    }
                    else
                    {
                        return false;
                    }
                }

                var publishResponse = await SendQnAMakerManagementRequest(string.Empty, HttpMethod.Post, url);
                return publishResponse.IsSuccessStatusCode;
            }
            else
            {
                return false;
            }
        }



        private async Task<HttpResponseMessage> SendQnAMakerRequest<TIn>(TIn inputObject, HttpMethod method, string url)
        {
            using (var client = new HttpClient())
            {
                var message = new HttpRequestMessage(method, url);
                message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("EndpointKey", qnamakerSubscriptionKey);

                message.Content = new StringContent(JsonConvert.SerializeObject(inputObject), Encoding.UTF8, "application/json");

                return await client.SendAsync(message);
            }
        }

        private async Task<HttpResponseMessage> SendQnAMakerRequest(HttpMethod method, string url)
        {
            using (var client = new HttpClient())
            {
                var message = new HttpRequestMessage(method, url);
                message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("EndpointKey", qnamakerSubscriptionKey);

                return await client.SendAsync(message);
            }
        }

        private async Task<HttpResponseMessage> SendQnAMakerManagementRequest<TIn>(TIn inputObject, HttpMethod method, string url)
        {
            using (var client = new HttpClient())
            {
                var message = new HttpRequestMessage(method, url);
                message.Headers.Add("Ocp-Apim-Subscription-Key", qnamakerManagmentKey);

                message.Content = new StringContent(JsonConvert.SerializeObject(inputObject), Encoding.UTF8, "application/json");

                return await client.SendAsync(message);
            }
        }

        private async Task<HttpResponseMessage> SendQnAMakerManagementRequest(HttpMethod method, string url)
        {
            using (var client = new HttpClient())
            {
                var message = new HttpRequestMessage(method, url);
                message.Headers.Add("Ocp-Apim-Subscription-Key", qnamakerManagmentKey);

                return await client.SendAsync(message);
            }
        }

        public async Task<IKnowledgeBase> GetKnowledgeBase()
        {
            var url = $"{qnamakerManagementUriBase}/knowledgebases/{knowledgebaseId}/Prod/qna ";
            var response = await SendQnAMakerManagementRequest(HttpMethod.Get, url);

            if (response.IsSuccessStatusCode)
            {
                var qnaResponse = JsonConvert.DeserializeObject<QnaDbResponse>(await response.Content.ReadAsStringAsync());
                if (qnaResponse != null)
                {
                    return ParseKnowledgeBase(qnaResponse);
                }
            }

            return null;
        }

        private IKnowledgeBase ParseKnowledgeBase(QnaDbResponse qandAResponse)
        {
            var knowledgeBase = new Dictionary<string, string>();
            foreach (var answer in qandAResponse.qnaDocuments)
            {
                foreach (var question in answer.questions)
                {
                    if (!knowledgeBase.ContainsKey(question))
                        knowledgeBase.Add(question, answer.answer);

                }
            }

            return new KnowledgeBaseEntity()
            {
                KnowledgeBase = knowledgeBase
            };
        }
    }

    public class HtmlEncodingConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(String);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return HttpUtility.HtmlDecode((string)reader.Value);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteRawValue(HttpUtility.HtmlEncode((string)value));
        }
    }

    public class QnaDocument
    {
        public int id { get; set; }
        public string answer { get; set; }
        public string source { get; set; }
        public List<string> questions { get; set; }
        public List<object> metadata { get; set; }
    }

    public class QnaDbResponse
    {
        public List<QnaDocument> qnaDocuments { get; set; }
    }

    public class OperationStatus
    {
        public string operationState { get; set; }
        public DateTime createdTimestamp { get; set; }
        public DateTime lastActionTimestamp { get; set; }
        public string userId { get; set; }
        public string operationId { get; set; }
    }
}
