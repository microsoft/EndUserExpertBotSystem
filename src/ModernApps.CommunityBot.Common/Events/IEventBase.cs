// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using System;

namespace ModernApps.CommunityBot.Common.Events
{
    public interface IEventBase
    {
        Guid EventId { get; }

        DateTime TimeOfEvent { get; }
    }
}