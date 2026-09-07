using System.ComponentModel.DataAnnotations;

namespace ECommerce522.Validations
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CustomLengthAttribute : ValidationAttribute
    {
        private readonly int _minLength;
        private readonly int _maxLength;

        public CustomLengthAttribute(int minLength, int maxLength)
        {
            _minLength = minLength;
            _maxLength = maxLength;
        }

        public override bool IsValid(object? value)
        {
            if(value is string result)
            {
                return result.Length > _minLength && result.Length < _maxLength;
            }

            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The filed {name}, Length Must be {_minLength} and {_maxLength}";
        }
    }
}
