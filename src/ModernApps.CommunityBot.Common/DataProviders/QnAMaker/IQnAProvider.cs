// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using ModernApps.CommunityBot.Common.DataProviders.QnAMaker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernApps.CommunitiyBot.Common.Providers
{
    public interface IQnAMakerProvider
    {
        Task<IQnaResponse> GetQandAResponse(string query);
        Task<bool> StoreNewAnswer(string question, string answer);
        Task<bool> ReplaceAnswer(string question, string answer, IQnaResponse qnaResponse);
        Task<IKnowledgeBase> GetKnowledgeBase();
    }
}
