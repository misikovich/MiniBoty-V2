using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MiniBoty
{
    public class SetValueFeedback
    {
        public object PreviousValue { get; set; } = new object();
        public object NewValue { get; set; } = new object();
        public string FeedbackMessage { get; set; } = string.Empty;
        public bool Succesfull { get; set; } = false;
    }
    public enum ParameterType
    {
        TagParam,
        IsActiveParam,
        DefaultPasteTimeoutParam,
        LevensteinDistanceTriggParam,
        PasteTriggParam,
        OverlengthParam
    }
    public class ParameterCollection
    {
        public List<Parameter> Parameters { get; private set; }
        public int Count { get { return Parameters.Count; } }

        public ParameterCollection()
        {
            Parameters = new List<Parameter>();
        }

        public void AddParameter(Parameter parameter)
        {
            for (int i = 0; i < Parameters.Count; i++)
            {
                if (Parameters[i].Type == parameter.Type)
                {
                    Parameters[i] = parameter;
                    return;
                }
            }
            Parameters.Add(parameter);
        }
        public object GetValue(object parameter)
        {
            if (Parameters.Count == 0)
            {
                return null;
            }

            if (parameter.GetType() == typeof(ParameterType))
            {
                var tempType = (ParameterType) parameter;
                foreach (var item in Parameters)
                {
                    if (tempType == item.Type)
                    {
                        return item.Value;
                    }
                }
            }
            else if (parameter.GetType() == typeof(string))
            {
                foreach (var item in Parameters)
                {
                    if (parameter.ToString().ToLower() == item.Name)
                    {
                        return item.Value;
                    }
                }
            }
            
            return null;
        }

        public SetValueFeedback SetValue(string parameterName, object value)
        {
            ///<summary>
            /// -1 = Data is null
            /// -2 = Incorrect value
            /// -3 = Not found
            ///  1 = Success
            ///</summary>


            var feedback = new SetValueFeedback();
            if (Parameters == null || parameterName == null || value == null)
            {
                feedback.FeedbackMessage = "some data is empty";
                return feedback;
            }

            parameterName = Regex.Replace(parameterName, @"<[^>]+>|&nbsp;", "").Trim().ToLower().Replace('_', '-'); // make string look like some-parameter-name

            foreach (var item in Parameters)
            {
                if (item.Name == parameterName)
                {
                    if (item.ValueType == typeof(TimeSpan) && value.GetType() == typeof(int))
                    {
                        value = TimeSpan.FromSeconds(Convert.ToInt32(value));
                    }

                    if (item.ValueType == value.GetType())
                    {
                        var prevValue = item.Value;

                        
                        item.Value = Convert.ChangeType(value, item.ValueType);

                        feedback.Succesfull = true;
                        feedback.PreviousValue = prevValue;
                        feedback.NewValue = value;
                        return feedback;
                    }
                    else
                    {
                        feedback.FeedbackMessage = $"incorrect value '{value} {value.GetType().Name}', should be '{item.ValueType.Name}'";
                        return feedback;
                    }
                }
            }

            feedback.FeedbackMessage = $"parameter '{parameterName} not found'";
            return feedback;
        }
    }
    public class Parameter
    {
        public ParameterType Type { get; protected set; }
        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value.ToLower(); }
        }
        public string Description { get; protected set; }
        public object Value { get; set; }
        public Type ValueType { get; protected set; }
        protected static object IfNullSetDefault(object Default, object[] value)
        {
            if (value.Length > 0 && value[0] is not null && value.Length <= 1)
            {
                return value[0];
            }
            else
            {
                return Default;
            }
        }
        
        
    }
    public class TagParam : Parameter
    {
        private const string _default = "mb";

        public TagParam(params object[] value)
        {
            Type = ParameterType.TagParam;
            Value = IfNullSetDefault(_default, value);
            ValueType = typeof(string);
            Name = "tag";
            Description = "Changes bot tag command";
        }
    }
    public class IsActiveParam : Parameter
    {
        private const bool _default = false;

        public IsActiveParam(params object[] value)
        {
            Type = ParameterType.IsActiveParam;
            Value = IfNullSetDefault(_default, value);
            ValueType = typeof(bool);
            Name = "active";
            Description = "Changes bot activity, if active:'true' - bot is moderating";
        }
    }
    public class DefaultPasteTimeoutParam : Parameter
    {
        private readonly TimeSpan _default = TimeSpan.FromSeconds(5);

        public DefaultPasteTimeoutParam(params object[] value)
        {
            Type = ParameterType.DefaultPasteTimeoutParam;
            Value = IfNullSetDefault(_default, value);
            ValueType = typeof(TimeSpan);
            Name = "def-paste-timeout";
            Description = "Changes default timeout time for being paste or overlengthed";
        }
    }
    internal class LevensteinDistanceTriggParam : Parameter
    {
        private const int _default = 9;

        public LevensteinDistanceTriggParam(params object[] value)
        {
            Type = ParameterType.LevensteinDistanceTriggParam;
            Value = IfNullSetDefault(_default, value);
            ValueType = typeof(int);
            Name = "l-dist_trig";
            Description = "Influences how message should be similar to '!mb ban <message>' to be punished";
        }
    }
    internal class PasteTriggParam : Parameter
    {
        private const int _default = 65;

        public PasteTriggParam(params object[] value)
        {
            Type = ParameterType.PasteTriggParam;
            Value = IfNullSetDefault(_default, value);
            ValueType = typeof(int);
            Name = "paste-content-trig";
            Description = "Influences how much repetition percentage must be in a message to be timeouted";
        }
    }
    internal class OverlengthParam : Parameter
    {
        private const int _default = 65;

        public OverlengthParam(params object[] value)
        {
            Type = ParameterType.OverlengthParam;
            Value = IfNullSetDefault(_default, value);
            ValueType = typeof(int);
            Name = "msg-max-length-trig";
            Description = "Influences how much repetition percentage must be in a message to be timeouted";
        }
    }
}
