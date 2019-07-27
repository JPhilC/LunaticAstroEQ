using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ASCOM.LunaticAstroEQ.Core
{
   public class EnumBindingHelper
   {
      public List<KeyValuePair<Enum, string>> GetKVPs(System.Type type)
      {
         List<KeyValuePair<Enum, string>> list = new List<KeyValuePair<Enum, string>>();
         Array enumValues = Enum.GetValues(type);

         foreach (Enum value in enumValues)
         {
            list.Add(new KeyValuePair<Enum, string>(value, EnumHelper.GetDescription(value)));
         }

         return list;
      }

   }

   /// <summary>
   /// A class to help binding Comboboxes etc to an enums and display the description attribute value
   /// </summary>
   public static class EnumHelper
   {
      public static string GetDescription(this System.Enum value)
      {
         if (value == null)
         {
            throw new ArgumentNullException("value");
         }

         string description = value.ToString();
         FieldInfo fieldInfo = value.GetType().GetField(description);
         DescriptionAttribute[] attributes =
            (DescriptionAttribute[])
          fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

         if (attributes != null && attributes.Length > 0)
         {
            description = attributes[0].Description;
         }
         return description;
      }

      public static int GetIntValue(this System.Enum value)
      {
         if (value == null)
         {
            throw new ArgumentNullException("value");
         }

         string description = value.ToString();
         FieldInfo fieldInfo = value.GetType().GetField(description);
         return (int)fieldInfo.GetRawConstantValue();
      }


      public static System.Enum GetEnumValue(this Type type, string description)
      {
         System.Enum matchingValue = null;
         Array enumValues = Enum.GetValues(type);
         string normalizedDescription = Regex.Replace(description, @"\s", "");
         foreach (Enum value in enumValues)
         {
            string normalizedValue = Regex.Replace(GetDescription(value), @"\s", "");
            if (String.Equals(normalizedValue, normalizedDescription, StringComparison.OrdinalIgnoreCase))
            {
               matchingValue = value;
               break;
            }
         }
         return matchingValue;
      }

      public static IList ToIList(this Type type, bool useInt = false)
      {
         if (type == null)
         {
            throw new ArgumentNullException("type");
         }

         ArrayList list = new ArrayList();
         Array enumValues = Enum.GetValues(type);

         foreach (Enum value in enumValues)
         {
            if (useInt)
            {
               list.Add(new KeyValuePair<int, string>(GetIntValue(value), GetDescription(value)));
            }
            else
            {
               list.Add(new KeyValuePair<Enum, string>(value, GetDescription(value)));
            }
         }

         return list;
      }

      public static IList ToIList(this Type type, List<int> ignoreList, bool useInt = false)
      {
         if (type == null)
         {
            throw new ArgumentNullException("type");
         }

         ArrayList list = new ArrayList();
         Array enumValues = Enum.GetValues(type);

         foreach (Enum value in enumValues)
         {
            if (!ignoreList.Contains(Convert.ToInt32(value)))
            {
               if (useInt)
               {
                  list.Add(new KeyValuePair<int, string>(GetIntValue(value), GetDescription(value)));
               }
               else
               {
                  list.Add(new KeyValuePair<Enum, string>(value, GetDescription(value)));
               }
            }
         }

         return list;
      }

      public static List<string> GetDescriptions(System.Enum exampleValue)
      {
         if (exampleValue == null)
         {
            throw new ArgumentNullException("exampleValue");
         }

         Type type = exampleValue.GetType();

         List<string> list = new List<string>();
         Array enumValues = Enum.GetValues(type);

         foreach (Enum value in enumValues)
         {
            list.Add(GetDescription(value));
         }

         return list;
      }

      public static List<string> GetDescriptions(this Type type)
      {
         if (type == null)
         {
            throw new ArgumentNullException("type");
         }

         List<string> list = new List<string>();
         Array enumValues = Enum.GetValues(type);

         foreach (Enum value in enumValues)
         {
            list.Add(GetDescription(value));
         }

         return list;
      }

      public static List<KeyValuePair<Enum, string>> ToList(this Type type)
      {
         if (type == null)
         {
            throw new ArgumentNullException("type");
         }

         List<KeyValuePair<Enum, string>> list = new List<KeyValuePair<Enum, string>>();
         Array enumValues = Enum.GetValues(type);

         foreach (Enum value in enumValues)
         {
            list.Add(new KeyValuePair<Enum, string>(value, GetDescription(value)));
         }

         return list;
      }

      public static List<KeyValuePair<Enum, string>> ToList(this Type type, List<int> ignoreList)
      {
         if (type == null)
         {
            throw new ArgumentNullException("type");
         }

         List<KeyValuePair<Enum, string>> list = new List<KeyValuePair<Enum, string>>();
         Array enumValues = Enum.GetValues(type);

         foreach (Enum value in enumValues)
         {
            if (!ignoreList.Contains(Convert.ToInt32(value)))
            {
               list.Add(new KeyValuePair<Enum, string>(value, GetDescription(value)));
            }
         }
         return list;
      }

   }
}
