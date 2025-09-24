using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Lilly
{
    public static class MyLog
    {
        private static string _assemblyName;

        public static string AssemblyName
        {
            get
            {
                if (_assemblyName == null)
                {
                    _assemblyName = Assembly.GetCallingAssembly().GetName().Name;
                }
                return _assemblyName;
            }
        }

        public static string MyText(
            string text,
            string memberName,
            int lineNumber,
            string className,
            string color = null)
        {
            if (color != null)
            {
                return $"<color=#BDC3C7FF>{AssemblyName}</color>.<color=#00FF00FF>{className}</color>.<color=#FF8000FF>{memberName}</color> <color=#00FFFFFF>{lineNumber}</color> - <color=#{color}>{text}</color>";
            }
            return $"<color=#BDC3C7FF>{AssemblyName}</color>.<color=#00FF00FF>{className}</color>.<color=#FF8000FF>{memberName}</color> <color=#00FFFFFF>{lineNumber}</color> - {text}";
        }

        public static void Warning(string text,
            bool print = true, string color = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0
            )
        {
            if (!print) return;
            string className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            Log.Warning(MyText(text, memberName, lineNumber, className, color));
        }

        public static void Error(string text,
            bool print = true, string color = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!print) return;
            string className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            Log.Error(MyText(text, memberName, lineNumber, className, color));
        }

        public static void Message(string text,
            bool print = true, string color = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!print) return;
            string className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            Log.Message(MyText(text, memberName, lineNumber, className, color));
        }

    }
}
