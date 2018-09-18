// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernApps.CommunitiyBot.Common.Providers
{
    public interface IQnaResponse
    {
        string Question { get; }
        string Answer { get; }
        double Score { get; }
        bool FoundAnswer { get; }
        string MatchingQuestion { get; }
    }
}
