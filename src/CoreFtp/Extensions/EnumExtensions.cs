namespace CoreFtp.Extensions
{
    using System;
#if NETSTANDARD
    using System.Reflection;
#endif
    using Attributes;
    using Enum;
    using System.Linq;

    public static class EnumExtensions
    {
        public static TEnum? ToNullableEnum< TEnum >( this string operand ) where TEnum : struct, IComparable, IFormattable, IConvertible
        {
            TEnum enumOut;
            if ( Enum.TryParse( operand, true, out enumOut ) )
            {
                return enumOut;
            }

            return null;
        }

        public static TEnum? ToNullableEnum< TEnum >( this int operand ) where TEnum : struct, IComparable, IFormattable, IConvertible
        {
            if ( Enum.IsDefined( typeof( TEnum ), operand ) )
            {
                return (TEnum) (object) operand;
            }

            return null;
        }

        public static string ToCommandString( this FtpCommand operand )
        {
            string name = operand.ToString();
            var ftpCommandValue = operand.GetType()
                                         .GetField( name )
                                         .GetCustomAttributes( typeof( FtpCommandValueAttribute ), false )
                                         .FirstOrDefault() as FtpCommandValueAttribute;

            return ftpCommandValue?.Command;
        }
    }
}