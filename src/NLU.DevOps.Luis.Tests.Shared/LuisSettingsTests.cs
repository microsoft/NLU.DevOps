// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis.Tests
{
    using System;
    using System.Collections.Generic;
    using FluentAssertions;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    internal static class LuisSettingsTests
    {
        [Test]
        public static void ThrowsArgumentNull()
        {
            Action nullSettings = () => LuisSettings.FromJson(null);
            nullSettings.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("settings");
        }

        [Test]
        public static void ParsesPrebuiltEntities()
        {
            var settings = LuisSettings.FromJson(new JObject
            {
                { "prebuiltEntityTypes", new JObject { { "foo", "bar" } } },
            });

            settings.AppTemplate.Name.Should().BeNull();
            settings.PrebuiltEntityTypes.Should().Contain(new KeyValuePair<string, string>("foo", "bar"));
        }

        [Test]
        public static void ParsesRoles()
        {
            var settings = LuisSettings.FromJson(new JObject
            {
                { "roles", new JObject { { "foo", "bar" } } },
            });

            settings.AppTemplate.Name.Should().BeNull();
            settings.Roles.Should().Contain(new KeyValuePair<string, string>("foo", "bar"));
        }

        [Test]
        public static void ParsesAppTemplate()
        {
            var settings = LuisSettings.FromJson(new JObject
            {
                { "appTemplate", new JObject { { "name", "foo" } } },
            });

            settings.PrebuiltEntityTypes.Should().BeEmpty();
            settings.AppTemplate.Name.Should().Be("foo");
        }

        [Test]
        public static void ParsesAsLuisApp()
        {
            var settings = LuisSettings.FromJson(new JObject
            {
                { "name", "foo" },
            });

            settings.PrebuiltEntityTypes.Should().BeEmpty();
            settings.AppTemplate.Name.Should().Be("foo");
        }
    }
}
