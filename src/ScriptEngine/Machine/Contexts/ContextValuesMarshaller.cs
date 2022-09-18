﻿/*----------------------------------------------------------
This Source Code Form is subject to the terms of the 
Mozilla Public License, v.2.0. If a copy of the MPL 
was not distributed with this file, You can obtain one 
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/
using System;
using System.Linq;
using ScriptEngine.Machine.Values;

namespace ScriptEngine.Machine.Contexts
{
    public static class ContextValuesMarshaller
    {
        public static T ConvertParam<T>(IValue value, T defaultValue = default)
        {
            object valueObj = ConvertParam(value, typeof(T));
            return valueObj != null ? (T)valueObj : defaultValue;
        }

        public static T ConvertParamDef<T>(IValue value, object defaultValue)
        {
            object valueObj = ConvertParam(value, typeof(T));
            return valueObj != null ? (T)valueObj : (T)defaultValue;
        }

        public static object ConvertParam(IValue value, Type type)
        {
            try
            {
                return ConvertValueType(value, type);
            }
            catch (InvalidCastException)
            {
                throw RuntimeException.InvalidArgumentType();
            }
            catch (OverflowException)
            {
                throw RuntimeException.InvalidArgumentValue();
            }
        }


        private static object ConvertValueType(IValue value, Type type)
        {
            object valueObj;
            if (value == null || value.DataType == DataType.NotAValidValue)
            {
                return null;
            }

            if (Nullable.GetUnderlyingType(type) != null)
            {
                return ConvertValueType(value, Nullable.GetUnderlyingType(type));
            }

            if (type == typeof(IValue))
            {
                valueObj = value;
            }
            else if (type == typeof(IVariable))
            {
                valueObj = value;
            }
            else if (type == typeof(string))
            {
                valueObj = value.AsString();
            }
            else if (value == UndefinedValue.Instance)
            {
                // Если тип параметра не IValue и не IVariable && Неопределено -> null
                valueObj = null;
            }
            else if (type == typeof(int))
            {
                valueObj = (int)value.AsNumber();
            }
            else if (type == typeof(sbyte))
            {
                valueObj = (sbyte)value.AsNumber();
            }
            else if (type == typeof(short))
            {
                valueObj = (short)value.AsNumber();
            }
            else if (type == typeof(ushort))
            {
                valueObj = (ushort)value.AsNumber();
            }
            else if (type == typeof(uint))
            {
                valueObj = (uint)value.AsNumber();
            }
            else if (type == typeof(byte))
            {
                valueObj = (byte)value.AsNumber();
            }
            else if (type == typeof(long))
            {
                valueObj = (long)value.AsNumber();
            }
            else if (type == typeof(ulong))
            {
                valueObj = (ulong)value.AsNumber();
            }
            else if (type == typeof(double))
            {
                valueObj = (double)value.AsNumber();
            }
            else if (type == typeof(decimal))
            {
                valueObj = value.AsNumber();
            }
            else if (type == typeof(DateTime))
            {
                valueObj = value.AsDate();
            }
            else if (type == typeof(bool))
            {
                valueObj = value.AsBoolean();
            }
            else if (typeof(IRuntimeContextInstance).IsAssignableFrom(type))
            {
                valueObj = value.AsObject();
            }
            else
            {
                valueObj = CastToCLRObject(value);
            }

            return valueObj;
        }

        private static IValue ConvertReturnValue(object objParam, Type type)
        {
            if (objParam == null)
                return ValueFactory.Create();

            switch (objParam)
            {
                case IValue v: return v;

                case string s: return ValueFactory.Create(s);
                case bool b: return ValueFactory.Create(b);
                case DateTime d: return ValueFactory.Create(d);

                case int n: return ValueFactory.Create(n);
                case uint n: return ValueFactory.Create(n);
                case long n: return ValueFactory.Create(n);
                case ulong n: return ValueFactory.Create(n);
                case byte n: return ValueFactory.Create(n);
                case sbyte n: return ValueFactory.Create(n);
                case short n: return ValueFactory.Create(n);
                case ushort n: return ValueFactory.Create(n);
                case decimal n: return ValueFactory.Create(n);
                case double n: return ValueFactory.Create((decimal)n);
            }

            if (type.IsEnum)
            {
                return ConvertEnum(objParam, type);
            }
            else if (typeof(IRuntimeContextInstance).IsAssignableFrom(type))
            {
                return ValueFactory.Create((IRuntimeContextInstance)objParam);
            }
            else if (typeof(IValue).IsAssignableFrom(type))
            {
                return (IValue)objParam;
            }
            else if (Nullable.GetUnderlyingType(type) != null)
            {
                return ConvertReturnValue(objParam, Nullable.GetUnderlyingType(type));
            }
            else
            {
                throw new ValueMarshallingException(Locale.NStr(
                    $"ru='Возвращаемый тип {type} не поддерживается'; en='Return type {type} is not supported'"));
            }
        }

        private static IValue ConvertEnum(object objParam, Type type)
        {
            if (!type.IsAssignableFrom(objParam.GetType()))
                throw new ValueMarshallingException(Locale.NStr(
                    $"ru='Некорректный тип конвертируемого перечисления'; en='Invalid enum return type'"));

            var memberInfo = type.GetMember(objParam.ToString());
            var valueInfo = memberInfo.FirstOrDefault(x => x.DeclaringType == type);
            var attrs = valueInfo.GetCustomAttributes(typeof(EnumItemAttribute), false);

            if (attrs.Length == 0)
                throw new ValueMarshallingException(Locale.NStr(
                     "ru='Значение перечисления должно быть помечено атрибутом EnumItemAttribute';"
                    +"en='An enumeration value must be marked with the EnumItemAttribute attribute"));

            var itemName = ((EnumItemAttribute)attrs[0]).Name;
            var enumImpl = GlobalsManager.GetSimpleEnum(type);

            return enumImpl.GetPropValue(itemName);
        }

        public static T ConvertWrappedEnum<T>(IValue enumeration, T defValue) where T : struct
        {
            if (enumeration == null)
                return defValue;

            if (enumeration.GetRawValue() is CLREnumValueWrapper<T> wrapped)
            {
                return wrapped.UnderlyingValue;
            }

            throw RuntimeException.InvalidArgumentValue();
        }
 
        public static IValue ConvertReturnValue(object param)
        {
            return ConvertReturnValue(param, param.GetType());
        }


        public static IValue ConvertReturnValue<TRet>(TRet param)
        {
            return ConvertReturnValue(param, typeof(TRet));
        }

		public static object ConvertToCLRObject(IValue val)
		{
			object result;
			if (val == null)
				return val;
			
			switch (val.DataType)
			{
			case Machine.DataType.Boolean:
				result = val.AsBoolean();
				break;
			case Machine.DataType.Date:
				result = val.AsDate();
				break;
			case Machine.DataType.Number:
				result = val.AsNumber();
				break;
			case Machine.DataType.String:
				result = val.AsString();
				break;
			case Machine.DataType.Undefined:
				result = null;
				break;
			default:
                if (val.DataType == DataType.Object)
                    result = val.AsObject();

				result = val.GetRawValue();
				if (result is IObjectWrapper)
					result = ((IObjectWrapper)result).UnderlyingObject;
				else
				    throw new ValueMarshallingException(Locale.NStr(
                          $"ru='Тип {val.GetType()} не поддерживает преобразование в CLR-объект';"
                         +$"en='Type {val.GetType()} does not support conversion to CLR object'"));

                break;
			}
			
			return result;
		}

        public static T CastToCLRObject<T>(IValue val)
        {
            return (T)CastToCLRObject(val);
        }

        public static object CastToCLRObject(IValue val)
        {
            var rawValue = val.GetRawValue();
            object objectRef;
            if (rawValue.DataType == DataType.GenericValue)
            {
                objectRef = rawValue;
            }
            else
            {
                objectRef = ConvertToCLRObject(rawValue);
            }

            return objectRef;

        }
    }
}
