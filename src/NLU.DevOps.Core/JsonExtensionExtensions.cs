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
    public static class JsonExtensionExtensions
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
            return instance.WithProperty(ScorePropertyName, score);
        }

        /// <summary>
        /// Adds a confidence score for the text transcription to the labeled utterance.
        /// </summary>
        /// <param name="instance">Labeled utterance instance.</param>
        /// <param name="textScore">Confidence score.</param>
        /// <returns>Labeled utterance with transcription confidence score.</returns>
        public static ILabeledUtterance WithTextScore(this ILabeledUtterance instance, double? textScore)
        {
            return instance.WithProperty(TextScorePropertyName, textScore);
        }

        /// <summary>
        /// Adds a timestamp to the labeled utterance.
        /// </summary>
        /// <param name="instance">Labeled utterance instance.</param>
        /// <param name="timestamp">Timestamp.</param>
        /// <returns>Labeled utterance with timestamp.</returns>
        public static ILabeledUtterance WithTimestamp(this ILabeledUtterance instance, DateTimeOffset? timestamp)
        {
            return instance.WithProperty(TimestampPropertyName, timestamp);
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
            return instance.WithProperty(propertyName, propertyValue, ToJsonExtension);
        }

        /// <summary>
        /// Adds a confidence score to the entity.
        /// </summary>
        /// <param name="instance">Entity instance.</param>
        /// <param name="score">Confidence score.</param>
        /// <returns>Entity with confidence score.</returns>
        public static IEntity WithScore(this IEntity instance, double? score)
        {
            return instance.WithProperty(ScorePropertyName, score, ToJsonExtension);
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

        /// <summary>
        /// Checks if a property is present in the JSON object.
        /// </summary>
        /// <param name="labeledUtterance">Labeled utterance.</param>
        /// <param name="propertyName">Property name.</param>
        /// <returns>
        /// <code>true</code> if the property is present, otherwise <code>false</code>.
        /// </returns>
        public static bool HasProperty(this ILabeledUtterance labeledUtterance, string propertyName)
        {
            if (labeledUtterance is IJsonExtension jsonExtension)
            {
                return jsonExtension.AdditionalProperties.TryGetValue(propertyName, out var unused);
            }

            throw new InvalidOperationException("Property existence cannot be checked.");
        }

        private static T WithProperty<T>(
                this T instance,
                string propertyName,
                object value,
                Func<T, T> extensionSelector)
            where T : class
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
            ((IJsonExtension)extension).AdditionalProperties[propertyName] = value;
            return extension;
        }

        private static ILabeledUtterance ToJsonExtension(this ILabeledUtterance utterance)
        {
            return utterance is IJsonExtension
                ? utterance
                : new LabeledUtterance(utterance.Text, utterance.Intent, utterance.Entities);
        }

        private static IEntity ToJsonExtension(this IEntity entity)
        {
            return entity is IJsonExtension
                ? entity
                : new Entity(entity.EntityType, entity.EntityValue, entity.MatchText, entity.MatchIndex);
        }

        private static T GetPropertyCore<T>(this object instance, string propertyName)
        {
            if (instance is IJsonExtension jsonExtension && jsonExtension.AdditionalProperties.TryGetValue(propertyName, out var value))
            {
                return value is JToken jsonValue
                    ? jsonValue.Value<T>()
                    : (T)value;
            }

            return default;
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
