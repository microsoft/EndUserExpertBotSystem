// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace ModernApps.CommunitiyBot.Common.Providers
{
    public class QandAResponse : IQnaResponse
    {

        public List<AnswerObject> answers { get; set; }

        public string Question { get; set; }

        public double Score => answers.OrderByDescending(x => x.score).FirstOrDefault().score;

        public bool FoundAnswer => Score >= Threshold;
        public double Threshold { get; set; }
        public string MatchingQuestion => answers.OrderByDescending(x => x.score).FirstOrDefault().questions.FirstOrDefault();

        public string Answer => answers.OrderByDescending(x => x.score).FirstOrDefault().answer;

        public class AnswerObject
        {
            [JsonProperty("Answer")]
            public string answer { get; set; }
            [JsonProperty("Questions")]
            public List<string> questions { get; set; }
            [JsonProperty("Score")]
            public double score { get; set; }
        }

    }
}
