// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModernApps.CommunitiyBot.Common.Providers;

namespace ModernApps.CommunityBot.Tests
{
    [TestClass]
    public class QnaMakerProviderTests
    {
        IQnAMakerProvider qnAMakerProvider;

        [TestInitialize]
        public void Setup()
        {
            qnAMakerProvider = new QnAMakerProvider("https://eaiazstackqnamaker.azurewebsites.net/qnamaker", "https://westus.api.cognitive.microsoft.com/qnamaker/v4.0", "84fe5154-29ff-4c1e-8720-22d6c25535ff", "3e3a74ab-5d5b-47d3-9ce8-802a9e7e275b", "a00a10b0613c44238918acbdc54eab0e", 0.65);
        }

        [TestMethod]
        public void TestGetKnowledgeBase()
        {
            var kb = qnAMakerProvider.GetKnowledgeBase().GetAwaiter().GetResult();
        }
    }
}
