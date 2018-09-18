// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ModernApps.CommunityBot.Common.Helpers
{
    public static class MessageHelper
    {

        private static Regex normalizeRegex;

        static MessageHelper()
        {
            normalizeRegex = new Regex(@"([\?\s])*([\w\s]*)([\?\s])*");
        }

        public static string NormalizeString(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var normalizedInput = normalizeRegex.Replace(input, "$2");
            var capitalCase = char.ToUpper(normalizedInput.First()) + normalizedInput.Substring(1).ToLowerInvariant();
            return capitalCase.Trim();
        }
    }
}
