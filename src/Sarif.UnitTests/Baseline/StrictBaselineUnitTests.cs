﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using System.Linq;

namespace Microsoft.CodeAnalysis.Sarif.Baseline
{
    public class StrictBaselineUnitTests
    {
        private readonly ITestOutputHelper output;

        public StrictBaselineUnitTests(ITestOutputHelper outputHelper)
        {
            output = outputHelper;
        }

        private ISarifLogBaseliner strictBaseliner = SarifLogBaselinerFactory.CreateSarifLogBaseliner(SarifBaselineType.Strict);

        [Fact]
        public void StrictBaseline_SameResults_AllExisting()
        {
            Random random = RandomSarifLogGenerator.GenerateRandomAndLog(this.output);
            Run baseline = RandomSarifLogGenerator.GenerateRandomRunWithoutDuplicateIssues(random, Result.ValueComparer, random.Next(100)+5);
            Run next = baseline.DeepClone();

            Run result = strictBaseliner.CreateBaselinedRun(baseline, next);

            result.Results.Should().OnlyContain(r => r.BaselineState == BaselineState.Existing);
            result.Results.Should().HaveCount(baseline.Results.Count());
        }

        [Fact]
        public void StrictBaseline_NewResultAdded_New()
        {
            Random random = RandomSarifLogGenerator.GenerateRandomAndLog(this.output);
            Run baseline = RandomSarifLogGenerator.GenerateRandomRunWithoutDuplicateIssues(random, Result.ValueComparer, random.Next(100) + 5);
            Run next = baseline.DeepClone();
            next.Results.Add(RandomSarifLogGenerator.GenerateFakeResults(random, new List<string>() { "NEWTESTRESULT" }, new List<Uri>() { new Uri(@"c:\test\testfile") }, 1).First());

            Run result = strictBaseliner.CreateBaselinedRun(baseline, next);

            result.Results.Where(r => r.BaselineState == BaselineState.New).Should().ContainSingle();

            result.Results.Should().HaveCount(baseline.Results.Count()+1);
        }

        [Fact]
        public void StrictBaseline_RemovedResult_Absent()
        {
            Random random = RandomSarifLogGenerator.GenerateRandomAndLog(this.output);
            Run baseline = RandomSarifLogGenerator.GenerateRandomRunWithoutDuplicateIssues(random, Result.ValueComparer, random.Next(100) + 5);
            Run next = baseline.DeepClone();
            next.Results.RemoveAt(0);

            Run result = strictBaseliner.CreateBaselinedRun(baseline, next);

            int dupes = baseline.Results.Where(r => baseline.Results.Where(s => Result.ValueComparer.Equals(r, s)).Count() != 1).Count();
            if (dupes == 0)
            {
                result.Results.Where(r => r.BaselineState == BaselineState.Absent).Should().ContainSingle();
            }
            result.Results.Should().HaveCount(baseline.Results.Count());
        }
    

        [Fact]
        public void StrictBaseline_ChangedResult_AbsentAndNew()
        {
            Random random = RandomSarifLogGenerator.GenerateRandomAndLog(this.output);
            random = new Random(181968016);
            Run baseline = RandomSarifLogGenerator.GenerateRandomRunWithoutDuplicateIssues(random, Result.ValueComparer, random.Next(100) + 5);
            Run next = baseline.DeepClone();
            next.Results[0].RuleId += "V2";
            
            Run result = strictBaseliner.CreateBaselinedRun(baseline, next);

            result.Results.Where(r => r.BaselineState == BaselineState.New).Should().ContainSingle();

            result.Results.Where(r => r.BaselineState == BaselineState.Absent).Should().ContainSingle();
            
            result.Results.Should().HaveCount(baseline.Results.Count()+1);
        }
    }
}
