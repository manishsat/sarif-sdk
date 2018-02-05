﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CodeAnalysis.Sarif.Driver
{
    public enum ExceptionCondition
    {
        None,
        AccessingId,
        AccessingName,
        InvokingConstructor,
        InvokingAnalyze,
        InvokingCanAnalyze,
        InvokingInitialize,
        ParsingTarget,
        LoadingPdb,
        ValidatingOptions,
        InvalidPlatform
    }
}
