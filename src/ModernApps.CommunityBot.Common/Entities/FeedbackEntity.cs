// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernApps.CommunityBot.Common.Entities
{
    public class FeedbackEntity : TableEntity
    {

        public FeedbackEntity()
        {
            RowKey = Guid.NewGuid().ToString();
        }

        private FeedbackType feedbackType;

        public string Answer { get; set; }

        public string Question
        {
            get;
            set;
        }

        public FeedbackType FeedbackType
        {
            get { return feedbackType; }
            set
            {
                feedbackType = value;
                PartitionKey = value.ToString();
            }
        }

        public int FeedbackCount { get; set; }





    }
}
