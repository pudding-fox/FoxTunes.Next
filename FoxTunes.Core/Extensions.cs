using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace FoxTunes
{
    public static partial class Extensions
    {
        private static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        public static bool HasCustomAttribute<T>(this Type type, out T attribute) where T : Attribute
        {
            return type.HasCustomAttribute<T>(false, out attribute);
        }

        public static bool HasCustomAttribute<T>(this Type type, bool inherit, out T attribute) where T : Attribute
        {
            if (type.Assembly.ReflectionOnly)
            {
                if (!type.HasCustomAttributeData<T>(inherit))
                {
                    attribute = default(T);
                    return false;
                }
                type = AssemblyRegistry.Instance.GetExecutableType(type);
            }
            return (attribute = type.GetCustomAttribute<T>(inherit)) != null;
        }

        public static bool HasCustomAttributes<T>(this Type type, out IEnumerable<T> attributes) where T : Attribute
        {
            return type.HasCustomAttributes<T>(false, out attributes);
        }

        public static bool HasCustomAttributes<T>(this Type type, bool inherit, out IEnumerable<T> attributes) where T : Attribute
        {
            if (type.Assembly.ReflectionOnly)
            {
                if (!type.HasCustomAttributeData<T>(inherit))
                {
                    attributes = default(IEnumerable<T>);
                    return false;
                }
                type = AssemblyRegistry.Instance.GetExecutableType(type);
            }
            return (attributes = type.GetCustomAttributes<T>(inherit)).Any();
        }

        public static bool HasCustomAttributeData<T>(this Type type, bool inherit)
        {
            if (inherit)
            {
                throw new NotImplementedException();
            }
            var typeNames = type.GetCustomAttributesData().GetTypeNames();
            return typeNames.Any(typeName => string.Equals(typeName, typeof(T).FullName, StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<string> GetTypeNames(this IEnumerable<CustomAttributeData> attributes)
        {
            var typeNames = new List<string>();
            var attributeTypeName = typeof(Attribute).FullName;
            foreach (var attribute in attributes)
            {
                var type = attribute.Constructor.DeclaringType;
                do
                {
                    typeNames.Add(type.FullName);
                    type = type.BaseType;
                } while (type != null && !string.Equals(type.FullName, attributeTypeName, StringComparison.OrdinalIgnoreCase));
            }
            return typeNames;
        }

        public static T GetCustomAttribute<T>(this Type type, bool inherit = false) where T : Attribute
        {
            return type.GetCustomAttributes<T>(inherit).FirstOrDefault();
        }

        public static IEnumerable<T> GetCustomAttributes<T>(this Type type, bool inherit = false) where T : Attribute
        {
            return type.GetCustomAttributes(typeof(T), inherit).OfType<T>();
        }

        public static T Do<T>(this T value, Action<T> action)
        {
            action(value);
            return value;
        }

        public static IEnumerable<T> Do<T>(this IEnumerable<T> sequence, Action action)
        {
            foreach (var element in sequence)
            {
                action();
                yield return element;
            }
        }

        public static IEnumerable<T> Do<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            foreach (var element in sequence)
            {
                action(element);
                yield return element;
            }
        }

        public static IEnumerable<T> Enumerate<T>(this IEnumerable<T> sequence)
        {
            foreach (var element in sequence) ;
            return sequence;
        }

        public static bool Contains(this string subject, string value, bool ignoreCase)
        {
            if (!ignoreCase)
            {
                return subject.Contains(value);
            }
            return subject.IndexOf(value, StringComparison.OrdinalIgnoreCase) != -1;
        }

        public static string IfNullOrEmpty(this string value, string alternative)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
            return alternative;
        }

        public static int IndexOf<T>(this IEnumerable<T> sequence, T element, IEqualityComparer<T> comparer = null)
        {
            var index = 0;
            var enumerator = sequence.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (comparer != null)
                {
                    if (comparer.Equals(enumerator.Current, element))
                    {
                        return index;
                    }
                }
                else if (enumerator.Current != null && enumerator.Current.Equals(element))
                {
                    return index;
                }
                index++;
            }
            return -1;
        }

        public static string GetName(this string fileName)
        {
            var absolutePath = default(string);
            if (!fileName.GetAbsolutePath(out absolutePath))
            {
                return fileName;
            }
            var name = Path.GetFileName(absolutePath);
            return name;
        }

        public static string GetExtension(this string fileName)
        {
            var extension = Path.GetExtension(fileName.GetAbsolutePath());
            if (string.IsNullOrEmpty(extension) || extension.Length <= 1)
            {
                return string.Empty;
            }
            return extension.Substring(1).ToLower(CultureInfo.InvariantCulture);
        }

        public static string GetAbsolutePath(this string fileName)
        {
            var absolutePath = default(string);
            if (fileName.GetAbsolutePath(out absolutePath))
            {
                return absolutePath;
            }
            return fileName;
        }

        public static bool GetAbsolutePath(this string fileName, out string absolutePath)
        {
            if (!fileName.HasDirectory() || !string.IsNullOrEmpty(Path.GetPathRoot(fileName)))
            {
                absolutePath = fileName;
                return true;
            }
            try
            {
                var url = new Uri(fileName);
                absolutePath = Uri.UnescapeDataString(
                    url.AbsolutePath
                ).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                return true;
            }
            catch
            {
                //Nothing can be done.
            }
            absolutePath = null;
            return false;
        }

        public static bool HasDirectory(this string fileName)
        {
            if (fileName.IndexOf(Path.DirectorySeparatorChar) >= 0)
            {
                return true;
            }
            else if (fileName.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
            {
                return true;
            }
            return false;
        }

        public static string UCFirst(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            var words = value.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            for (var a = 0; a < words.Length; a++)
            {
                words[a] = char.ToUpper(words[a][0]) + words[a].Substring(1);
            }
            return string.Join(" ", words);
        }

        public static IEnumerable<string> GetLines(this string sequence)
        {
            if (string.IsNullOrEmpty(sequence))
            {
                yield break;
            }
            foreach (var element in sequence.Split('\n'))
            {
                yield return element.TrimEnd('\r');
            }
        }

        public static void ForEach(this IEnumerable<Action> sequence)
        {
            foreach (var action in sequence)
            {
                action();
            }
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            return sequence.Do(action).Enumerate();
        }

        public static IEnumerable<T> Try<T>(this IEnumerable<T> sequence, Action<T> action, Action<T, Exception> errorHandler = null)
        {
            return sequence.ForEach(element => element.Try(action, errorHandler));
        }

        public static T Try<T>(this T value, Action action, Action<Exception> errorHandler = null)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                if (errorHandler != null)
                {
                    errorHandler(e);
                }
            }
            return value;
        }

        public static T Try<T>(this T value, Action<T> action, Action<T, Exception> errorHandler = null)
        {
            try
            {
                action(value);
            }
            catch (Exception e)
            {
                if (errorHandler != null)
                {
                    errorHandler(value, e);
                }
            }
            return value;
        }

        public static IEnumerable<T> AddRange<T>(this IList<T> list, IEnumerable<T> sequence)
        {
            foreach (var element in sequence)
            {
                list.Add(element);
            }
            return sequence;
        }

        public static IEnumerable<KeyValuePair<TKey, TValue>> TryAddRange<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<KeyValuePair<TKey, TValue>> sequence)
        {
            foreach (var pair in sequence)
            {
                dictionary.TryAdd(pair.Key, pair.Value);
            }
            return sequence;
        }

        public static IEnumerable<T> EnqueueRange<T>(this Queue<T> queue, IEnumerable<T> sequence)
        {
            foreach (var element in sequence)
            {
                queue.Enqueue(element);
            }
            return sequence;
        }

        public static IEnumerable<T> RemoveRange<T>(this IList<T> list, IEnumerable<T> sequence)
        {
            foreach (var element in sequence)
            {
                list.Remove(element);
            }
            return sequence;
        }

        public static bool TryRemove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key)
        {
            if (EqualityComparer<TKey>.Default.Equals(key, default(TKey)))
            {
                return false;
            }
            var value = default(TValue);
            return dictionary.TryRemove(key, out value);
        }

        public static int ToNearestPower(this int value)
        {
            var result = value;
            var power = 10;
            var a = 0;

            while ((result /= power) >= power)
            {
                a++;
            }

            if (value % (int)(Math.Pow(power, a + 1) + 0.5) != 0)
            {
                result++;
            }

            for (; a >= 0; a--)
            {
                result *= power;
            }

            return result;
        }

        public static int ToNearestMultiple(this int value, int multiple)
        {
            return Math.Max(multiple * (value / multiple), multiple);
        }

        public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> sequence, TKey key, TValue value)
        {
            if (sequence.ContainsKey(key))
            {
                return false;
            }
            sequence.Add(key, value);
            return true;
        }

        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> sequence, TKey key, TValue value)
        {
            var current = default(TValue);
            if (sequence.TryGetValue(key, out current))
            {
                return current;
            }
            sequence.Add(key, value);
            return value;
        }

        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> sequence, TKey key, Func<TValue> factory)
        {
            var value = default(TValue);
            if (sequence.TryGetValue(key, out value))
            {
                return value;
            }
            value = factory();
            sequence.Add(key, value);
            return value;
        }

        public static TValue GetOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> sequence, TKey key, Func<TValue> factory)
        {
            return sequence.GetOrAdd(key, _key => factory());
        }

        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> sequence, TKey key, Func<TKey, TValue> factory)
        {
            var value = default(TValue);
            if (sequence.TryGetValue(key, out value))
            {
                return value;
            }
            value = factory(key);
            sequence.Add(key, value);
            return value;
        }


        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> sequence, TKey key)
        {
            var value = default(TValue);
            if (!sequence.TryGetValue(key, out value))
            {
                return default(TValue);
            }
            return value;
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> sequence, TKey key, TValue defaultValue)
        {
            var value = default(TValue);
            if (!sequence.TryGetValue(key, out value))
            {
                value = defaultValue;
            }
            return value;
        }

        public static void Shuffle<T>(this IList<T> sequence)
        {
            var random = new Random(unchecked((int)DateTime.Now.Ticks));
            for (var a = 0; a < sequence.Count; a++)
            {
                var b = sequence[a];
                var c = random.Next(sequence.Count);
                sequence[a] = sequence[c];
                sequence[c] = b;
            }
        }

        public static void Shuffle<TKey, TValue>(this IDictionary<TKey, TValue> sequence)
        {
            var random = new Random(unchecked((int)DateTime.Now.Ticks));
            var keys = sequence.Keys.ToArray();
            for (var a = 0; a < keys.Length; a++)
            {
                var key1 = keys[a];
                var key2 = keys[random.Next(sequence.Count)];
                var value1 = sequence[key1];
                var value2 = sequence[key2];
                sequence[key1] = value2;
                sequence[key2] = value1;
            }
        }

        public static string Replace(this string value, IEnumerable<string> oldValues, string newValue, bool ignoreCase, bool once)
        {
            foreach (var oldValue in oldValues)
            {
                var success = default(bool);
                value = value.Replace(oldValue, newValue, ignoreCase, out success);
                if (success && once)
                {
                    break;
                }
            }
            return value;
        }

        public static string Replace(this string value, string oldValue, string newValue, bool ignoreCase, out bool success)
        {
            var index = default(int);
            if (ignoreCase)
            {
                index = value.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                index = value.IndexOf(oldValue);
            }
            if (success = (index != -1))
            {
                var offset = index + oldValue.Length;
                return
                    value.Substring(0, index) +
                    newValue +
                    value.Substring(offset, value.Length - offset);
            }
            return value;
        }

        public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> sequence)
        {
            return sequence.OrderBy(element => element);
        }

        public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> sequence, IComparer<T> comparer)
        {
            return sequence.OrderBy(element => element, comparer);
        }

        public static string GetQueryParameter(this Uri uri, string name)
        {
            var parameters = uri.Query.TrimStart('?').Split('&');
            foreach (var parameter in parameters)
            {
                var parts = parameter.Split(new[] { '=' }, 2);
                if (parts.Length != 2)
                {
                    continue;
                }
                if (!string.Equals(parts[0], name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                return parts[1];
            }
            return null;
        }

        public static string Replace(this string subject, IEnumerable<char> oldChars, char newChar)
        {
            foreach (var oldChar in oldChars)
            {
                subject = subject.Replace(oldChar, newChar);
            }
            return subject;
        }

        public static void AddOrUpdate<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> sequence, TKey key, TValue value)
        {
            sequence.AddOrUpdate(
                key,
                _key => value,
                (_key, _value) => value
            );
        }

        public static bool TryRemove<TKey, TValue>(this ConditionalWeakTable<TKey, TValue> sequence, TKey key, out TValue value) where TKey : class where TValue : class
        {
            if (!sequence.TryGetValue(key, out value))
            {
                return false;
            }
            return sequence.Remove(key);
        }

        public static string Remove(this string subject, Func<char, bool> predicate)
        {
            var characters = subject.Where(@char => !predicate(@char));
            return new string(characters.ToArray());
        }

        public static int Distance(this string subject, string value, bool ignoreSpaces)
        {
            if (ignoreSpaces)
            {
                subject = subject.Remove(@char => char.IsWhiteSpace(@char));
                value = value.Remove(@char => char.IsWhiteSpace(@char));
            }

            if (string.Equals(subject, value, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            //https://en.wikipedia.org/wiki/Levenshtein_distance

            var distance = new int[subject.Length + 1, value.Length + 1];
            for (var a = 0; a <= subject.Length; a++)
            {
                distance[a, 0] = a;
            }

            for (var a = 0; a <= value.Length; a++)
            {
                distance[0, a] = a;
            }

            for (var a = 1; a <= subject.Length; a++)
            {
                for (var b = 1; b <= value.Length; b++)
                {
                    var cost = 0;
                    if (char.ToLowerInvariant(value[b - 1]) != char.ToLowerInvariant(subject[a - 1]))
                    {
                        cost++;
                    }
                    distance[a, b] = Math.Min(
                        Math.Min(
                            distance[a - 1, b] + 1,
                            distance[a, b - 1] + 1
                        ),
                        distance[a - 1, b - 1] + cost
                    );
                }
            }

            return distance[subject.Length, value.Length];
        }

        public static float Similarity(this string subject, string value, bool ignoreSpaces)
        {
            if (ignoreSpaces)
            {
                subject = subject.Remove(@char => char.IsWhiteSpace(@char));
                value = value.Remove(@char => char.IsWhiteSpace(@char));
            }

            if (string.Equals(subject, value, StringComparison.OrdinalIgnoreCase))
            {
                return 1.0f;
            }

            var distance = subject.Distance(value, false);
            return (1.0f - ((float)distance / (float)Math.Max(subject.Length, value.Length)));
        }

        public static IList<T> AsList<T>(this IEnumerable<T> sequence)
        {
            if (sequence is IList<T> list)
            {
                return list;
            }
            return sequence.ToList();
        }

        public static Stream ToStream(this string value)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(value);
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public static int GetDeterministicHashCode(this string value)
        {
            unchecked
            {
                var hash1 = (5381 << 16) + 5381;
                var hash2 = hash1;
                for (var a = 0; a < value.Length; a += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ value[a];
                    if (a == value.Length - 1)
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ value[a + 1];
                }
                return hash1 + (hash2 * 1566083941);
            }
        }

        public static bool Any<TKey, TValue>(this IDictionary<TKey, TValue> sequence, IEnumerable<TKey> keys, Func<TValue, bool> predicate)
        {
            return keys.Any(key => predicate(sequence.GetValueOrDefault(key)));
        }

        public static void AddRange<T>(this ConcurrentBag<T> bag, IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                bag.Add(value);
            }
        }

        public static T BySequence<T>(this IEnumerable<T> components, int sequence) where T : ISequenceableComponent
        {
            return components.Where(component => component.Sequence == sequence).FirstOrDefault();
        }

        public static IDictionary<string, MetaDataItem> ToDictionary(this IEnumerable<MetaDataItem> metaDatas)
        {
            var result = new Dictionary<string, MetaDataItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var metaData in metaDatas)
            {
                if (!result.TryAdd(metaData.Name, metaData))
                {
                    Logger.Write(typeof(Extensions), LogLevel.Error, "Failed to create dictionary from meta data, duplicate key: {0}", metaData.Name);
                }
            }
            return result;
        }

        public static IDictionary<string, string> ToDictionary2(this IEnumerable<MetaDataItem> metaDatas)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var metaData in metaDatas)
            {
                if (!result.TryAdd(metaData.Name, metaData.Value))
                {
                    Logger.Write(typeof(Extensions), LogLevel.Error, "Failed to create dictionary from meta data, duplicate key: {0}", metaData.Name);
                }
            }
            return result;
        }
    }

    public class NumericFallbackComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            var value1 = default(int);
            var value2 = default(int);
            if (int.TryParse(x, out value1) && int.TryParse(y, out value2))
            {
                return value1.CompareTo(value2);
            }
            return StringComparer.OrdinalIgnoreCase.Compare(x, y);
        }

        public static IComparer<string> Instance
        {
            get
            {
                return new NumericFallbackComparer();
            }
        }
    }
}
