// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Authoring.Models;
    using Microsoft.Extensions.Configuration;
    using Models;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    internal static class LuisNLUTrainClientTests
    {
        /// <summary>
        /// Epsilon used to accomodate for clock accuracy.
        /// </summary>
        private static readonly TimeSpan Epsilon = TimeSpan.FromMilliseconds(100);

        [Test]
        public static void ThrowsArgumentNull()
        {
            var luisTrainClient = new Mock<ILuisTrainClient>().Object;
            var luisConfiguration = new LuisConfiguration(new ConfigurationBuilder().Build());
            Action nullLuisConfiguration = () => new LuisNLUTrainClient(null, new LuisApp(), luisTrainClient);
            Action nullLuisTemplate = () => new LuisNLUTrainClient(luisConfiguration, null, luisTrainClient);
            Action nullLuisClient = () => new LuisNLUTrainClient(luisConfiguration, new LuisApp(), null);
            nullLuisConfiguration.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("luisConfiguration");
            nullLuisTemplate.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("luisTemplate");
            nullLuisClient.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("luisClient");

            using (var luis = new LuisNLUTrainClientBuilder().Build())
            {
                Func<Task> nullUtterances = () => luis.TrainAsync(null);
                Func<Task> nullUtterance = () => luis.TrainAsync(new Models.LabeledUtterance[] { null });
                nullUtterances.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("utterances");
                nullUtterance.Should().Throw<ArgumentException>().And.ParamName.Should().Be("utterances");
            }
        }

        [Test]
        public static async Task TrainEmptyModel()
        {
            var builder = new LuisNLUTrainClientBuilder();
            builder.AppVersion = Guid.NewGuid().ToString();
            using (var luis = builder.Build())
            {
                var utterances = Array.Empty<Models.LabeledUtterance>();
                await luis.TrainAsync(utterances).ConfigureAwait(false);

                // Assert correct import request
                var importRequest = builder.MockLuisTrainClient.Invocations.FirstOrDefault(request => request.Method.Name == nameof(ILuisTrainClient.ImportVersionAsync));
                importRequest.Should().NotBeNull();
                importRequest.Arguments[2].Should().NotBeNull();
                var luisApp = importRequest.Arguments[2].As<LuisApp>();

                // Expects 3 intents
                luisApp.Intents.Count.Should().Be(1);
                luisApp.Intents.First().Name.Should().Be("None");

                // Assert train request
                var trainRequest = builder.MockLuisTrainClient.Invocations.FirstOrDefault(request => request.Method.Name == nameof(ILuisTrainClient.TrainAsync));
                trainRequest.Should().NotBeNull();

                // Assert publish request
                var publishRequest = builder.MockLuisTrainClient.Invocations.FirstOrDefault(request => request.Method.Name == nameof(ILuisTrainClient.PublishAppAsync));
                publishRequest.Should().NotBeNull();
                publishRequest.Arguments[0].Should().Be(builder.AppId);
                publishRequest.Arguments[1].Should().Be(builder.AppVersion);
            }
        }

        [Test]
        public static async Task TrainModelWithUtterances()
        {
            var builder = new LuisNLUTrainClientBuilder();
            using (var luis = builder.Build())
            {
                var utterances = new[]
                {
                    new Models.LabeledUtterance("Book me a flight.", "BookFlight", Array.Empty<Entity>()),
                    new Models.LabeledUtterance("Cancel my flight.", "CancelFlight", Array.Empty<Entity>())
                };

                await luis.TrainAsync(utterances).ConfigureAwait(false);

                // Assert correct import request
                var importRequest = builder.MockLuisTrainClient.Invocations.FirstOrDefault(request => request.Method.Name == nameof(ILuisTrainClient.ImportVersionAsync));
                importRequest.Should().NotBeNull();
                var luisApp = importRequest.Arguments[2].As<LuisApp>();

                // Expects 3 intents
                luisApp.Intents.Count.Should().Be(3);
                luisApp.Intents.Should().Contain(intent => intent.Name == "None");
                luisApp.Intents.Should().Contain(intent => intent.Name == utterances[0].Intent);
                luisApp.Intents.Should().Contain(intent => intent.Name == utterances[1].Intent);

                // Expect 2 utterances
                luisApp.Utterances.Count.Should().Be(2);

                var bookUtterance = luisApp.Utterances.FirstOrDefault(utterance => utterance.Intent == utterances[0].Intent);
                bookUtterance.Should().NotBeNull();
                bookUtterance.Text.Should().Be(utterances[0].Text);
                bookUtterance.Entities.Count.Should().Be(0);

                var cancelUtterance = luisApp.Utterances.FirstOrDefault(utterance => utterance.Intent == utterances[1].Intent);
                cancelUtterance.Should().NotBeNull();
                cancelUtterance.Text.Should().Be(utterances[1].Text);
                cancelUtterance.Entities.Count.Should().Be(0);
            }
        }

        [Test]
        public static async Task TrainModelWithUtterancesAndSimpleEntities()
        {
            var builder = new LuisNLUTrainClientBuilder();
            using (var luis = builder.Build())
            {
                var utterances = new[]
                {
                    new Models.LabeledUtterance(
                        "Book me a flight.",
                        "BookFlight",
                        new[] { new Entity("Name", null, "me", 0) }),
                    new Models.LabeledUtterance(
                        "Cancel my flight.",
                        "CancelFlight",
                        new[] { new Entity("Subject", null, "flight", 0) })
                };

                await luis.TrainAsync(utterances).ConfigureAwait(false);

                // Assert correct import request
                var importRequest = builder.MockLuisTrainClient.Invocations.FirstOrDefault(request => request.Method.Name == nameof(ILuisTrainClient.ImportVersionAsync));
                importRequest.Should().NotBeNull();
                var luisApp = importRequest.Arguments[2].As<LuisApp>();

                // Expects 3 intents
                luisApp.Intents.Count.Should().Be(3);
                luisApp.Intents.Should().Contain(intent => intent.Name == "None");
                luisApp.Intents.Should().Contain(intent => intent.Name == utterances[0].Intent);
                luisApp.Intents.Should().Contain(intent => intent.Name == utterances[1].Intent);

                // Expect 2 utterances
                luisApp.Utterances.Count.Should().Be(2);

                var bookUtterance = luisApp.Utterances.FirstOrDefault(utterance => utterance.Intent == utterances[0].Intent);
                bookUtterance.Should().NotBeNull();
                bookUtterance.Text.Should().Be(utterances[0].Text);
                bookUtterance.Entities.Count.Should().Be(1);
                bookUtterance.Entities.First().Entity.Should().Be(utterances[0].Entities[0].EntityType);
                bookUtterance.Entities.First().StartPos.Should().Be(5);
                bookUtterance.Entities.First().EndPos.Should().Be(6);

                var cancelUtterance = luisApp.Utterances.FirstOrDefault(utterance => utterance.Intent == utterances[1].Intent);
                cancelUtterance.Should().NotBeNull();
                cancelUtterance.Text.Should().Be(utterances[1].Text);
                cancelUtterance.Entities.Count.Should().Be(1);
                cancelUtterance.Entities.First().Entity.Should().Be(utterances[1].Entities[0].EntityType);
                cancelUtterance.Entities.First().StartPos.Should().Be(10);
                cancelUtterance.Entities.First().EndPos.Should().Be(15);
            }
        }

        [Test]
        public static async Task CleanupModel()
        {
            var builder = new LuisNLUTrainClientBuilder
            {
                AppCreated = true,
            };

            using (var luis = builder.Build())
            {
                await luis.CleanupAsync().ConfigureAwait(false);
                var deleteAppRequest = builder.MockLuisTrainClient.Invocations.FirstOrDefault(request => request.Method.Name == nameof(ILuisTrainClient.DeleteAppAsync));
                var deleteVersionRequest = builder.MockLuisTrainClient.Invocations.FirstOrDefault(request => request.Method.Name == nameof(ILuisTrainClient.DeleteVersionAsync));
                deleteAppRequest.Should().NotBeNull();
                deleteVersionRequest.Should().BeNull();
            }
        }

        [Test]
        public static async Task CleanupModelVersionOnly()
        {
            var builder = new LuisNLUTrainClientBuilder();
            using (var luis = builder.Build())
            {
                await luis.CleanupAsync().ConfigureAwait(false);
                var deleteAppRequest = builder.MockLuisTrainClient.Invocations.FirstOrDefault(request => request.Method.Name == nameof(ILuisTrainClient.DeleteAppAsync));
                var deleteVersionRequest = builder.MockLuisTrainClient.Invocations.FirstOrDefault(request => request.Method.Name == nameof(ILuisTrainClient.DeleteVersionAsync));
                deleteAppRequest.Should().BeNull();
                deleteVersionRequest.Should().NotBeNull();
            }
        }

        [Test]
        public static async Task TrainingStatusDelayBetweenPolling()
        {
            var count = 0;
            string[] statusArray = { "Queued", "InProgress", "Success" };

            var timestamps = new DateTimeOffset[statusArray.Length];
            var builder = new LuisNLUTrainClientBuilder();
            builder.MockLuisTrainClient
                .Setup(luis => luis.GetTrainingStatusAsync(
                    It.Is<string>(appId => appId == builder.AppId),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<IList<ModelTrainingInfo>>(new[]
                {
                    new ModelTrainingInfo
                    {
                        Details = new ModelTrainingDetails { Status = statusArray[count++] }
                    }
                }))
                .Callback(() => timestamps[count - 1] = DateTimeOffset.Now);

            using (var luis = builder.Build())
            {
                await luis.TrainAsync(Array.Empty<Models.LabeledUtterance>()).ConfigureAwait(false);

                // Ensure correct number of training status requests are made.
                builder.MockLuisTrainClient.Invocations.Where(request => request.Method.Name == nameof(ILuisTrainClient.GetTrainingStatusAsync))
                    .Count().Should().Be(statusArray.Length);

                // Ensure 2 second delay between requests
                for (var i = 1; i < statusArray.Length; ++i)
                {
                    var timeDifference = timestamps[i] - timestamps[i - 1];
                    timeDifference.Should().BeGreaterThan(TimeSpan.FromSeconds(2) - Epsilon);
                }
            }
        }

        [Test]
        public static void TrainingFailedThrowsInvalidOperation()
        {
            var failureReason = Guid.NewGuid().ToString();
            var builder = new LuisNLUTrainClientBuilder();
            builder.MockLuisTrainClient
                .Setup(luis => luis.GetTrainingStatusAsync(
                    It.Is<string>(appId => appId == builder.AppId),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<IList<ModelTrainingInfo>>(new[]
                {
                    new ModelTrainingInfo
                    {
                        Details = new ModelTrainingDetails { Status = "Fail", FailureReason = failureReason }
                    }
                }));

            using (var luis = builder.Build())
            {
                Func<Task> trainAsync = () => luis.TrainAsync(Array.Empty<Models.LabeledUtterance>());
                trainAsync.Should().Throw<InvalidOperationException>().And.Message.Should().Contain(failureReason);
            }
        }

        [Test]
        public static async Task CreatesAppIfAppIdNotProvided()
        {
            var appId = Guid.NewGuid().ToString();
            var builder = new LuisNLUTrainClientBuilder();
            builder.AppId = null;
            builder.MockLuisTrainClient
                .Setup(luis => luis.CreateAppAsync(
                    It.Is<string>(appName => appName == builder.AppName),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(appId));

            using (var luis = builder.Build())
            {
                await luis.TrainAsync(Array.Empty<Models.LabeledUtterance>()).ConfigureAwait(false);
                luis.LuisAppId.Should().Be(appId);
            }
        }

        [Test]
        public static async Task DoesNotOverwriteTemplateIntents()
        {
            var role = Guid.NewGuid().ToString();
            var intentName = Guid.NewGuid().ToString();
            var appTemplate = new LuisApp
            {
                ClosedLists = new List<ClosedList>(),
                Entities = new List<HierarchicalModel>(),
                Intents = new List<HierarchicalModel>
                {
                    new HierarchicalModel
                    {
                        Name = intentName,
                        Roles = new List<string> { role },
                    },
                },
                ModelFeatures = new List<JSONModelFeature>(),
                PrebuiltEntities = new List<PrebuiltEntity>(),
            };

            var builder = new LuisNLUTrainClientBuilder();
            builder.LuisTemplate = appTemplate;
            using (var luis = builder.Build())
            {
                var utterance = new Models.LabeledUtterance(null, intentName, null);
                await luis.TrainAsync(new[] { utterance }).ConfigureAwait(false);

                // Ensure LUIS app intent still has role
                var importRequest = builder.MockLuisTrainClient.Invocations.FirstOrDefault(request => request.Method.Name == nameof(ILuisTrainClient.ImportVersionAsync));
                importRequest.Should().NotBeNull();
                var luisApp = importRequest.Arguments[2].As<LuisApp>();
                luisApp.Intents.Should().Contain(intent => intent.Name == intentName);
                luisApp.Intents.First(intent => intent.Name == intentName).Roles.Count.Should().Be(1);
                luisApp.Intents.First(intent => intent.Name == intentName).Roles.First().Should().Be(role);
            }
        }

        [Test]
        public static async Task AddsRoleToEntities()
        {
            var text = Guid.NewGuid().ToString();

            var builder = new LuisNLUTrainClientBuilder();
            builder.LuisTemplate = new LuisApp
            {
                PrebuiltEntities = new[]
                {
                    new PrebuiltEntity
                    {
                        Name = "number",
                        Roles = new[] { "count" },
                    }
                },
            };

            using (var luis = builder.Build())
            {
                var entity1 = new Entity("number", null, text, 0);
                var entity2 = new Entity("count", null, text, 0);
                var utterance = new Models.LabeledUtterance(text, string.Empty, new[] { entity1, entity2 });
                await luis.TrainAsync(new[] { utterance }).ConfigureAwait(false);

                // Ensure LUIS app intent still has role
                var importRequest = builder.MockLuisTrainClient.Invocations.FirstOrDefault(request => request.Method.Name == nameof(ILuisTrainClient.ImportVersionAsync));
                importRequest.Should().NotBeNull();
                var luisApp = importRequest.Arguments[2].As<LuisApp>();
                luisApp.Utterances.Should().Contain(u => u.Text == text);
                luisApp.Utterances.First(u => u.Text == text).Entities.Count().Should().Be(2);
                luisApp.Utterances.First(u => u.Text == text).Entities[0].Entity.Should().Be("number");
                luisApp.Utterances.First(u => u.Text == text).Entities[1].Entity.Should().Be("number");
                luisApp.Utterances.First(u => u.Text == text).Entities.OfType<JSONEntityWithRole>().Single().Role.Should().Be("count");
            }
        }

        private class LuisNLUTrainClientBuilder
        {
            public string AppId { get; set; } = Guid.NewGuid().ToString();

            public string AppVersion { get; set; } = "0.1";

            public string AppName { get; set; } = "test";

            public bool AppCreated { get; set; } = false;

            public Mock<ILuisTrainClient> MockLuisTrainClient { get; } = new Mock<ILuisTrainClient>();

            public LuisApp LuisTemplate { get; set; } = new LuisApp();

            public LuisNLUTrainClient Build()
            {
                this.MockLuisTrainClient.SetReturnsDefault(
                    Task.FromResult<IList<ModelTrainingInfo>>(
                        Array.Empty<ModelTrainingInfo>()));

                var luisConfiguration = new LuisConfiguration(new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>
                    {
                        { "luisAppId", this.AppId },
                        { "luisAppCreated", this.AppCreated.ToString(CultureInfo.InvariantCulture) },
                        { "luisVersionId", this.AppVersion },
                        { "luisAppName", this.AppName },
                    })
                    .Build());

                return new LuisNLUTrainClient(luisConfiguration, this.LuisTemplate, this.MockLuisTrainClient.Object);
            }
        }
    }
}
