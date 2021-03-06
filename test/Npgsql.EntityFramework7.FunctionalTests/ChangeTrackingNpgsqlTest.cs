// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity.FunctionalTests;
using Xunit;

namespace Npgsql.EntityFramework7.FunctionalTests
{
    public class ChangeTrackingNpgsqlTest : ChangeTrackingTestBase<NorthwindQueryNpgsqlFixture>
    {
        public ChangeTrackingNpgsqlTest(NorthwindQueryNpgsqlFixture fixture)
            : base(fixture)
        {
        }

        [Fact(Skip = "Fails because of https://github.com/xunit/xunit/pull/383")]
        public override void Multiple_entities_can_revert()
        { }
    }
}
