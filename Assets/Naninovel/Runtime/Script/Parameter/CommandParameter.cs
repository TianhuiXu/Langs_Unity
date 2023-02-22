// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;
using Naninovel.Parsing;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents a <see cref="Command"/> parameter.
    /// </summary>
    /// <typeparam name="TValue">Type of the parameter value; should be natively supported by the Unity serialization system.</typeparam>
    [Serializable]
    public abstract class CommandParameter<TValue> : Nullable<TValue>, ICommandParameter
    {
        private readonly NamedValueParser parser = new NamedValueParser();

        /// <summary>
        /// Whether the value contains injected script expressions and is evaluated at runtime.
        /// </summary>
        public bool DynamicValue => dynamicValue?.Expressions?.Length > 0;
        /// <summary>
        /// When <see cref="DynamicValue"/>, returns the associated value text; null otherwise.
        /// </summary>
        public string DynamicValueText => DynamicValue ? dynamicValue.ValueText : null;

        [SerializeField] private DynamicValue dynamicValue;

        public override string ToString () => HasValue ? (DynamicValue ? dynamicValue.ValueText : base.ToString() ?? string.Empty) : "Unassigned";

        public virtual void SetValue (string valueText, out string errors)
        {
            Value = ParseValueText(valueText, out var hasValue, out errors);
            HasValue = hasValue;
        }

        public virtual void SetValue (DynamicValue dynamicValue)
        {
            this.dynamicValue = dynamicValue;
            HasValue = true;
        }

        protected override TValue GetValue () => DynamicValue ? EvaluateDynamicValue() : base.GetValue();

        protected override void SetValue (TValue value)
        {
            if (DynamicValue) // When overriding a dynamic value, reset the stored data.
                dynamicValue = default;
            base.SetValue(value);
        }

        protected virtual TValue EvaluateDynamicValue ()
        {
            if (!DynamicValue) throw new Error($"Failed to evaluate dynamic value of `{GetType().Name}` command parameter: the value is not dynamic. {dynamicValue.PlaybackSpot}");
            if (!(Engine.Behaviour is RuntimeBehaviour)) throw new Error($"Attempting to evaluate dynamic value of `{GetType().Name}` command parameter while the engine is not initialized. {dynamicValue.PlaybackSpot}");
            var valueText = dynamicValue.ValueText;
            foreach (var expression in dynamicValue.Expressions)
            {
                var expressionBody = expression.GetBetween("{", "}");
                var varValue = ExpressionEvaluator.Evaluate<string>(expressionBody, LogEvaluationError);
                valueText = valueText.Replace(expression, varValue);
            }
            var value = ParseValueText(valueText, out _, out var errors);
            if (!string.IsNullOrEmpty(errors))
                Script.LogWithPosition(dynamicValue.PlaybackSpot, errors, LogType.Error);
            return value;
            void LogEvaluationError (string message) => Script.LogWithPosition(dynamicValue.PlaybackSpot, message, LogType.Error);
        }

        protected abstract TValue ParseValueText (string valueText, out string errors);

        protected static int ParseIntegerText (string intText, out string errors)
        {
            errors = ParseUtils.TryInvariantInt(intText, out var result) ? null : $"Failed to parse `{intText}` string into `{nameof(Int32)}`";
            return result;
        }

        protected static float ParseFloatText (string floatText, out string errors)
        {
            errors = ParseUtils.TryInvariantFloat(floatText, out var result) ? null : $"Failed to parse `{floatText}` string into `{nameof(Single)}`";
            return result;
        }

        protected static bool ParseBooleanText (string boolText, out string errors)
        {
            errors = bool.TryParse(boolText, out var result) ? null : $"Failed to parse `{boolText}` string into `{nameof(Boolean)}`";
            return result;
        }

        protected void ParseNamedValueText (string valueText, out string name, out string value, out string errors)
        {
            errors = null;
            (name, value) = parser.Parse(valueText);
        }

        private TValue ParseValueText (string valueText, out bool hasValue, out string errors)
        {
            if (string.IsNullOrEmpty(valueText))
            {
                hasValue = false;
                errors = null;
                return default;
            }
            else hasValue = true;
            return ParseValueText(valueText, out errors);
        }
    }
}
