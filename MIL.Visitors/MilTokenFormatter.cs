using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Roslyn.Compilers.CSharp;

namespace MIL.Visitors
{
    public class MilTokenFormatter : System.IFormatProvider, ICustomFormatter
    {
        #region Implementation of IFormatProvider

        public object GetFormat(Type formatType)
        {
            if (formatType == typeof (ICustomFormatter))
                return this;
            else
                return null;

        }

        #endregion

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            var token = arg as MilToken;
            if (token == null)
            {
                try
                {
                    return HandleOtherFormats(format, arg);
                }
                catch (FormatException e)
                {
                    throw new FormatException(String.Format("The format of '{0}' is invalid.", format), e);
                }
            }

            return token.ToString();


        }

        private string HandleOtherFormats(string format, object arg)
        {
            if (arg is IFormattable)
                return ((IFormattable) arg).ToString(format, CultureInfo.CurrentCulture);
            else if (arg != null)
                return arg.ToString();
            else
                return String.Empty;
        }
    }
}
