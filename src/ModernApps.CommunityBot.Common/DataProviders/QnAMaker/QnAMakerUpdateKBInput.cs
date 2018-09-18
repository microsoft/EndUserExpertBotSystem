// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ModernApps.CommunitiyBot.Common.Providers
{
    public class QnAMakerUpdateKBInput
    {
        public QnAMakerUpdateKBInput()
        {
            Add = new AddObjects();
            Update = new UpdateObjects();
        }

        [JsonProperty("add")]
        public AddObjects Add { get; set; }
        [JsonProperty("update")]
        public UpdateObjects Update { get; set; }

        public class QnaDto
        {
            public int id { get; set; }
            public string answer { get; set; }
            public List<string> questions { get; set; }
        }

        public class QnaUpdateDto
        {
            public int id { get; set; }
            public string answer { get; set; }
            public List<QuestionUpdateDto> questions { get; set; }
        }

        public class AddObjects
        {
            public AddObjects()
            {
                QnaPairs = new List<QnaDto>();
            }
            [JsonProperty("qnaList")]
            public List<QnaDto> QnaPairs { get; set; }
            [JsonProperty("Urls")]
            public List<string> Urls { get; set; }
        }


        public class UpdateObjects
        {
            public UpdateObjects()
            {
                qnaList = new List<QnaUpdateDto>();
            }
            public List<QnaUpdateDto> qnaList { get; set; }
        }


    }

    public class QuestionUpdateDto
    {
        public QuestionUpdateDto()
        {
            add = new List<string>();
            delete = new List<string>();
        }

        List<string> add { get; set; }
        List<string> delete { get; set; }
    }
}