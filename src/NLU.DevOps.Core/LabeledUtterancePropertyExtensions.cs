// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System;
    using Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Enable standard data extensions for <see cref="LabeledUtterance"/>.
    /// </summary>
    public static class LabeledUtterancePropertyExtensions
    {
        private const string ScorePropertyName = "score";
        private const string TextScorePropertyName = "textScore";
        private const string TimestampPropertyName = "timestamp";

        /// <summary>
        /// Adds a confidence score for the intent label to the labeled utterance.
        /// </summary>
        /// <param name="instance">Labeled utterance instance.</param>
        /// <param name="score">Confidence score.</param>
        /// <returns>Labeled utterance with intent confidence score.</returns>
        public static ILabeledUtterance WithScore(this ILabeledUtterance instance, double? score)
        {
            return instance.WithProperty(ScorePropertyName, score, ToJsonLabeledUtterance);
        }

        /// <summary>
        /// Adds a confidence score for the text transcription to the labeled utterance.
        /// </summary>
        /// <param name="instance">Labeled utterance instance.</param>
        /// <param name="textScore">Confidence score.</param>
        /// <returns>Labeled utterance with transcription confidence score.</returns>
        public static ILabeledUtterance WithTextScore(this ILabeledUtterance instance, double? textScore)
        {
            return instance.WithProperty(TextScorePropertyName, textScore, ToJsonLabeledUtterance);
        }

        /// <summary>
        /// Adds a timestamp to the labeled utterance.
        /// </summary>
        /// <param name="instance">Labeled utterance instance.</param>
        /// <param name="timestamp">Timestamp.</param>
        /// <returns>Labeled utterance with timestamp.</returns>
        public static ILabeledUtterance WithTimestamp(this ILabeledUtterance instance, DateTimeOffset? timestamp)
        {
            return instance.WithProperty(TimestampPropertyName, timestamp, ToJsonLabeledUtterance);
        }

        /// <summary>
        /// Adds a property to the labeled utterance.
        /// </summary>
        /// <param name="instance">Labeled utterance instance.</param>
        /// <param name="propertyName">Property name.</param>
        /// <param name="propertyValue">Property value.</param>
        /// <returns>Labeled utterance with additional property.</returns>
        public static ILabeledUtterance WithProperty(this ILabeledUtterance instance, string propertyName, object propertyValue)
        {
            return instance.WithProperty(propertyName, propertyValue, ToJsonLabeledUtterance);
        }

        /// <summary>
        /// Adds a confidence score to the entity.
        /// </summary>
        /// <param name="instance">Entity instance.</param>
        /// <param name="score">Confidence score.</param>
        /// <returns>Entity with confidence score.</returns>
        public static IEntity WithScore(this IEntity instance, double? score)
        {
            return instance.WithProperty(ScorePropertyName, score, ToJsonEntity);
        }

        /// <summary>
        /// Gets the intent confidence score for the labeled utterance.
        /// </summary>
        /// <param name="instance">Labeled utterance instance.</param>
        /// <returns>
        /// Intent confidence score, or <code>null</code> if property is not set.
        /// </returns>
        public static double? GetScore(this ILabeledUtterance instance)
        {
            return instance.GetNumericProperty(ScorePropertyName);
        }

        /// <summary>
        /// Gets the text transcription confidence score for the labeled utterance.
        /// </summary>
        /// <param name="instance">Labeled utterance instance.</param>
        /// <returns>
        /// Transcription confidence score, or <code>null</code> if property is not set.
        /// </returns>
        public static double? GetTextScore(this ILabeledUtterance instance)
        {
            return instance.GetNumericProperty(TextScorePropertyName);
        }

        /// <summary>
        /// Gets the timestamp for the labeled utterance.
        /// </summary>
        /// <param name="instance">Labeled utterance instance.</param>
        /// <returns>
        /// Timestamp, or <code>null</code> if property is not set.
        /// </returns>
        public static DateTimeOffset? GetTimestamp(this ILabeledUtterance instance)
        {
            return instance.GetPropertyCore<DateTimeOffset?>(TimestampPropertyName);
        }

        /// <summary>
        /// Gets the property for the labeled utterance.
        /// </summary>
        /// <typeparam name="T">Property value type.</typeparam>
        /// <param name="instance">Labeled utterance instance.</param>
        /// <param name="propertyName">Property name.</param>
        /// <returns>
        /// Property value, or default if property is not set.
        /// </returns>
        public static T GetProperty<T>(this ILabeledUtterance instance, string propertyName)
        {
            return instance.GetPropertyCore<T>(propertyName);
        }

        /// <summary>
        /// Gets the confidence score for the entity.
        /// </summary>
        /// <param name="instance">Entity instance.</param>
        /// <returns>
        /// Entity confidence score, or <code>null</code> if property is not set.
        /// </returns>
        public static double? GetScore(this IEntity instance)
        {
            return instance.GetPropertyCore<double?>(ScorePropertyName);
        }

        private static T WithProperty<T, TResult>(
                this T instance,
                string propertyName,
                object value,
                Func<T, TResult> extensionSelector)
            where T : class
            where TResult : T, IJsonExtension
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (value == null)
            {
                return instance;
            }

            var extension = extensionSelector(instance);
            extension.AdditionalProperties[propertyName] = value;
            return extension;
        }

        private static JsonLabeledUtterance ToJsonLabeledUtterance(this ILabeledUtterance utterance)
        {
            return utterance is JsonLabeledUtterance jsonUtterance
                ? jsonUtterance
                : new JsonLabeledUtterance(utterance.Text, utterance.Intent, utterance.Entities);
        }

        private static JsonEntity ToJsonEntity(this IEntity entity)
        {
            return entity is JsonEntity jsonEntity
                ? jsonEntity
                : new JsonEntity(entity.EntityType, entity.EntityValue, entity.MatchText, entity.MatchIndex);
        }

        private static T GetPropertyCore<T>(this object instance, string propertyName)
        {
            if (instance is IJsonExtension jsonExtension && jsonExtension.AdditionalProperties.TryGetValue(propertyName, out var value))
            {
                return value is JToken jsonValue
                    ? jsonValue.Value<T>()
                    : (T)value;
            }

            return default(T);
        }

        private static double? GetNumericProperty(this object instance, string propertyName)
        {
            if (instance is IJsonExtension jsonExtension && jsonExtension.AdditionalProperties.TryGetValue(propertyName, out var value))
            {
                return value is long integerValue
                    ? integerValue
                    : (double)value;
            }

            return null;
        }
    }
}
