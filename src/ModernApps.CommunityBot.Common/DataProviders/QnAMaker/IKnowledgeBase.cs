// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.
using System.Collections.Generic;

namespace ModernApps.CommunityBot.Common.DataProviders.QnAMaker
{
    public interface IKnowledgeBase
    {
        Dictionary<string,string> KnowledgeBase { get; set; }
    }
}
