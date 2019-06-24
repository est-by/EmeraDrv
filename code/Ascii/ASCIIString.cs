using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sys
{
  public sealed class ASCIIString : IEnumerable<ASCIIChar>, IComparable<ASCIIString>
  {
    public static readonly ASCIIString Empty;

    static ASCIIString()
    {
      Empty = new ASCIIString(new byte[] { });
    }

    public static implicit operator String(ASCIIString value)
    {
      return value.ToString();
    }
    public static ASCIIString operator +(ASCIIString strA, ASCIIString strB)
    {
      return ASCIIString.Concat(strA, strB);
    }
    public static ASCIIString operator +(ASCIIString str, ASCIIChar chr)
    {
      if (str == null) throw new ArgumentNullException("str");

      int totalBytes = str.data.Length + 1;

      byte[] data = new byte[totalBytes];

      Buffer.BlockCopy(str.data, 0, data, 0, str.data.Length);
      data[totalBytes - 1] = chr.ToByte();

      return new ASCIIString(data);
    }
    public static bool operator ==(ASCIIString strA, ASCIIString strB)
    {
      return ASCIIString.Compare(strA, strB) == 0;
    }
    public static bool operator !=(ASCIIString strA, ASCIIString strB)
    {
      return ASCIIString.Compare(strA, strB) != 0;
    }

    public static int Compare(ASCIIString strA, ASCIIString strB)
    {
      return Compare(strA, strB, false);
    }
    public static int Compare(ASCIIString strA, ASCIIString strB, bool ignoreCase)
    {
      if (strA == null) throw new ArgumentNullException("strA");
      if (strB == null) throw new ArgumentNullException("strB");

      return SafeCompare(strA, 0, strB, 0, Math.Max(strA.data.Length, strB.data.Length), ignoreCase);
    }
    public static int Compare(ASCIIString strA, int indexA, ASCIIString strB, int indexB, int length)
    {
      return Compare(strA, indexA, strB, indexB, length, false);
    }
    public static int Compare(ASCIIString strA, int indexA, ASCIIString strB, int indexB, int length, bool ignoreCase)
    {
      if (strA == null) throw new ArgumentNullException("strA");
      if (strB == null) throw new ArgumentNullException("strB");
      if (indexA < 0 || indexA > strA.data.Length) throw new ArgumentOutOfRangeException("indexA");
      if (indexB < 0 || indexB > strB.data.Length) throw new ArgumentOutOfRangeException("indexB");
      if (length < 0 || indexA + length > strA.data.Length || indexB + length > strB.data.Length) throw new ArgumentOutOfRangeException("length");

      return SafeCompare(strA, indexA, strB, indexB, length, ignoreCase);
    }
    private static int SafeCompare(ASCIIString strA, int indexA, ASCIIString strB, int indexB, int length, bool ignoreCase)
    {
      for (int i = 0; i < length; i++)
      {
        int iA = i + indexA;
        int iB = i + indexB;

        if (iA == strA.data.Length && iB == strB.data.Length) return 0;
        if (iA == strA.data.Length) return -1;
        if (iB == strB.data.Length) return 1;

        byte byteA = strA.data[iA];
        byte byteB = strB.data[iB];

        if (ignoreCase)
        {
          byteA = ASCIIChar.ToLower(byteA);
          byteB = ASCIIChar.ToLower(byteB);
        }

        if (byteA < byteB) return -1;
        if (byteB < byteA) return 1;
      }

      return 0;
    }
    public static ASCIIString Copy(ASCIIString value)
    {
      byte[] data = new byte[value.data.Length];
      Buffer.BlockCopy(value.data, 0, data, 0, value.data.Length);
      return new ASCIIString(data);
    }
    public static ASCIIString Concat(params ASCIIString[] values)
    {
      return Concat((IEnumerable<ASCIIString>)values);
    }
    public static ASCIIString Concat(IEnumerable<ASCIIString> values)
    {
      if (values == null) throw new ArgumentNullException("values");

      int totalBytes = 0;
      int offset = 0;

      foreach (ASCIIString asciiString in values)
      {
        if (asciiString == null) continue;
        totalBytes += asciiString.data.Length;
      }

      byte[] data = new byte[totalBytes];

      foreach (ASCIIString asciiString in values)
      {
        if (asciiString == null) continue;
        Buffer.BlockCopy(asciiString.data, 0, data, offset, asciiString.data.Length);
        offset += asciiString.data.Length;
      }

      return new ASCIIString(data);
    }
    public static bool IsNullOrEmpty(ASCIIString value)
    {
      return value == null || value.data.Length == 0;
    }
    public static bool IsNullOrWhitespace(ASCIIString value)
    {
      if (value == null || value.data.Length == 0)
      {
        return true;
      }

      foreach (byte b in value.data)
      {
        if (!ASCIIChar.IsWhitespace(b))
        {
          return false;
        }
      }

      return true;
    }
    public static ASCIIString Join(ASCIIString seperator, params ASCIIString[] values)
    {
      return Join(seperator, (IEnumerable<ASCIIString>)values);
    }
    public static ASCIIString Join(ASCIIString seperator, IEnumerable<ASCIIString> values)
    {
      if (seperator == null) throw new ArgumentNullException("seperator");
      if (values == null) throw new ArgumentNullException("values");

      int totalBytes = 0;
      int offset = 0;

      foreach (ASCIIString asciiString in values)
      {
        if (asciiString == null) continue;
        totalBytes += asciiString.data.Length;
        totalBytes += seperator.data.Length;
      }

      if (totalBytes > 0) totalBytes -= seperator.data.Length;

      byte[] data = new byte[totalBytes];

      foreach (ASCIIString asciiString in values)
      {
        if (asciiString == null) continue;

        Buffer.BlockCopy(asciiString.data, 0, data, offset, asciiString.data.Length);
        offset += asciiString.data.Length;

        if (offset < totalBytes)
        {
          Buffer.BlockCopy(seperator.data, 0, data, offset, seperator.data.Length);
          offset += seperator.data.Length;
        }
      }

      return new ASCIIString(data);
    }
    public static ASCIIString Parse(string value)
    {
      if (value == null) throw new ArgumentNullException("value");

      return new ASCIIString(Encoding.ASCII.GetBytes(value));
    }

    private readonly byte[] data;

    public int Length
    {
      get
      {
        return data.Length;
      }
    }

    public ASCIIString(byte[] data, int startIndex, int length)
    {
      if (data == null) throw new ArgumentNullException("data");
      if (startIndex < 0 || startIndex > data.Length) throw new ArgumentOutOfRangeException("startIndex");
      if (length < 0 || startIndex + length > data.Length) throw new ArgumentOutOfRangeException("length");

      foreach (byte b in data)
      {
        if (!ASCIIChar.ValidateByte(b))
        {
          throw new ArgumentOutOfRangeException("data");
        }
      }

      this.data = new byte[length];
      Buffer.BlockCopy(data, startIndex, this.data, 0, length);
    }
    private ASCIIString(byte[] data)
    {
      this.data = data;
    }

    public ASCIIChar this[int index]
    {
      get
      {
        return new ASCIIChar(this.data[index]);
      }
    }

    public ASCIIString Clone()
    {
      return this;
    }
    public int CompareTo(ASCIIString value)
    {
      if (value == null) throw new ArgumentNullException("value");
      return ASCIIString.Compare(this, value);
    }
    public bool Contains(ASCIIString value)
    {
      if (value == null) throw new ArgumentNullException("value");
      return this.IndexOf(value) >= 0;
    }
    public bool Contains(ASCIIChar value)
    {
      byte valueByte = value.ToByte();

      foreach (byte b in this.data)
      {
        if (b.Equals(valueByte))
        {
          return true;
        }
      }

      return false;
    }
    public bool EndsWith(ASCIIString value)
    {
      return this.EndsWith(value, false);
    }
    public bool EndsWith(ASCIIString value, bool ignoreCase)
    {
      if (value == null) throw new ArgumentNullException("value");
      return ASCIIString.Compare(this, this.data.Length - value.data.Length, value, 0, value.Length, ignoreCase) == 0;
    }
    public override bool Equals(object obj)
    {
      if (obj is ASCIIString)
      {
        return base.Equals((ASCIIString)obj);
      }
      else
      {
        return false;
      }
    }
    public bool Equals(ASCIIString value)
    {
      return this.CompareTo(value) == 0;
    }
    public override int GetHashCode()
    {
      return base.GetHashCode();
    }
    public int IndexOf(ASCIIString value)
    {
      return this.IndexOf(value, 0, this.Length, false);
    }
    public int IndexOf(ASCIIString value, bool ignoreCase)
    {
      return this.IndexOf(value, 0, this.Length, ignoreCase);
    }
    public int IndexOf(ASCIIString value, int startIndex)
    {
      return this.IndexOf(value, startIndex, this.Length - startIndex, false);
    }
    public int IndexOf(ASCIIString value, int startIndex, bool ignoreCase)
    {
      return this.IndexOf(value, startIndex, this.Length - startIndex, ignoreCase);
    }
    public int IndexOf(ASCIIString value, int startIndex, int count)
    {
      return this.IndexOf(value, startIndex, count, false);
    }
    public int IndexOf(ASCIIString value, int startIndex, int count, bool ignoreCase)
    {
      if (value == null) throw new ArgumentNullException("value");
      if (startIndex < 0 || startIndex > this.data.Length) throw new ArgumentOutOfRangeException("startIndex");
      if (count < 0 || startIndex + count > this.data.Length || count < value.data.Length) throw new ArgumentOutOfRangeException("count");

      int charactersFound = 0;

      for (int i = startIndex; i < startIndex + count; i++)
      {
        if (i + (value.data.Length - charactersFound) > this.data.Length) return -1;

        byte byteA = this.data[i];
        byte byteB = value.data[charactersFound];

        if (ignoreCase)
        {
          byteA = ASCIIChar.ToLower(byteA);
          byteB = ASCIIChar.ToLower(byteB);
        }

        if (byteA == byteB) charactersFound++;
        else charactersFound = 0;

        if (charactersFound == value.data.Length) return (i - charactersFound + 1);
      }

      return -1;
    }
    public int IndexOfAny(params ASCIIChar[] values)
    {
      return this.IndexOfAny(values, 0, this.data.Length, false);
    }
    public int IndexOfAny(IEnumerable<ASCIIChar> values)
    {
      return this.IndexOfAny(values, 0, this.data.Length, false);
    }
    public int IndexOfAny(IEnumerable<ASCIIChar> values, bool ignoreCase)
    {
      return this.IndexOfAny(values, 0, this.data.Length, ignoreCase);
    }
    public int IndexOfAny(IEnumerable<ASCIIChar> values, int startIndex)
    {
      return this.IndexOfAny(values, startIndex, this.data.Length - startIndex, false);
    }
    public int IndexOfAny(IEnumerable<ASCIIChar> values, int startIndex, bool ignoreCase)
    {
      return this.IndexOfAny(values, startIndex, this.data.Length - startIndex, ignoreCase);
    }
    public int IndexOfAny(IEnumerable<ASCIIChar> values, int startIndex, int count)
    {
      return this.IndexOfAny(values, startIndex, count, false);
    }
    public int IndexOfAny(IEnumerable<ASCIIChar> values, int startIndex, int count, bool ignoreCase)
    {
      if (values == null) throw new ArgumentNullException("values");
      if (startIndex < 0 || startIndex > this.data.Length) throw new ArgumentOutOfRangeException("startIndex");
      if (count < 0 || startIndex + count > this.data.Length) throw new ArgumentOutOfRangeException("count");

      List<byte> valueBytes = new List<byte>();

      foreach (ASCIIChar c in values)
      {
        if (ignoreCase) valueBytes.Add(ASCIIChar.ToLower(c.ToByte()));
        else valueBytes.Add(c.ToByte());
      }

      for (int i = 0; i < this.data.Length; i++)
      {
        byte b = this.data[i];
        if (ignoreCase) b = ASCIIChar.ToLower(b);
        if (valueBytes.Contains(this.data[i])) return i;
      }

      return -1;
    }
    public ASCIIString Insert(ASCIIString value, int index)
    {
      if (value == null) throw new ArgumentNullException("value");
      if (index < 0 || index > this.data.Length) throw new ArgumentOutOfRangeException("index");

      int totalBytes = this.data.Length + value.data.Length;
      byte[] data = new byte[totalBytes];

      Buffer.BlockCopy(this.data, 0, data, 0, index);
      Buffer.BlockCopy(value.data, 0, data, index, value.data.Length);
      Buffer.BlockCopy(this.data, index, data, index + value.data.Length, this.data.Length - index);

      return new ASCIIString(data);
    }
    public int LastIndexOf(ASCIIString value)
    {
      return this.LastIndexOf(value, 0, this.Length, false);
    }
    public int LastIndexOf(ASCIIString value, bool ignoreCase)
    {
      return this.LastIndexOf(value, 0, this.Length, ignoreCase);
    }
    public int LastIndexOf(ASCIIString value, int startIndex)
    {
      return this.LastIndexOf(value, startIndex, this.Length - startIndex, false);
    }
    public int LastIndexOf(ASCIIString value, int startIndex, bool ignoreCase)
    {
      return this.LastIndexOf(value, startIndex, this.Length - startIndex, ignoreCase);
    }
    public int LastIndexOf(ASCIIString value, int startIndex, int count)
    {
      return this.LastIndexOf(value, startIndex, count, false);
    }
    public int LastIndexOf(ASCIIString value, int startIndex, int count, bool ignoreCase)
    {
      if (value == null) throw new ArgumentNullException("value");
      if (startIndex < 0 || startIndex > this.data.Length) throw new ArgumentOutOfRangeException("startIndex");
      if (count < 0 || startIndex + count > this.data.Length) throw new ArgumentOutOfRangeException("count");

      int lastIndexFound = -1;
      int result = startIndex - 1;

      do
      {
        result = this.IndexOf(value, result + 1, count - (result + 1), ignoreCase);

        if (result >= 0)
        {
          lastIndexFound = result;
        }
      }
      while (result >= 0 && result + 1 < this.data.Length - value.data.Length);

      return lastIndexFound;
    }
    public int LastIndexOfAny(params ASCIIChar[] values)
    {
      return this.LastIndexOfAny(values, 0, this.data.Length, false);
    }
    public int LastIndexOfAny(IEnumerable<ASCIIChar> values)
    {
      return this.LastIndexOfAny(values, 0, this.data.Length, false);
    }
    public int LastIndexOfAny(IEnumerable<ASCIIChar> values, bool ignoreCase)
    {
      return this.LastIndexOfAny(values, 0, this.data.Length, ignoreCase);
    }
    public int LastIndexOfAny(IEnumerable<ASCIIChar> values, int startIndex)
    {
      return this.LastIndexOfAny(values, startIndex, this.data.Length - startIndex, false);
    }
    public int LastIndexOfAny(IEnumerable<ASCIIChar> values, int startIndex, bool ignoreCase)
    {
      return this.LastIndexOfAny(values, startIndex, this.data.Length - startIndex, ignoreCase);
    }
    public int LastIndexOfAny(IEnumerable<ASCIIChar> values, int startIndex, int count)
    {
      return this.LastIndexOfAny(values, startIndex, count, false);
    }
    public int LastIndexOfAny(IEnumerable<ASCIIChar> values, int startIndex, int count, bool ignoreCase)
    {
      if (values == null) throw new ArgumentNullException("values");
      if (startIndex < 0 || startIndex > this.data.Length) throw new ArgumentOutOfRangeException("startIndex");
      if (count < 0 || startIndex + count > this.data.Length) throw new ArgumentOutOfRangeException("count");

      List<byte> valueBytes = new List<byte>();

      foreach (ASCIIChar c in values)
      {
        if (ignoreCase) valueBytes.Add(ASCIIChar.ToLower(c.ToByte()));
        else valueBytes.Add(c.ToByte());
      }

      int lastIndex = -1;

      for (int i = 0; i < this.data.Length; i++)
      {
        byte b = this.data[i];
        if (ignoreCase) b = ASCIIChar.ToLower(b);
        if (valueBytes.Contains(this.data[i])) lastIndex = i;
      }

      return lastIndex;
    }
    public ASCIIString PadLeft(int totalLength)
    {
      return this.PadLeft(totalLength, ASCIIChars.Space);
    }
    public ASCIIString PadLeft(int totalLength, ASCIIChar c)
    {
      if (totalLength < this.data.Length) throw new ArgumentOutOfRangeException("totalLength");

      byte[] data = new byte[totalLength];
      byte charByte = c.ToByte();

      int i = 0;

      for (; i + this.data.Length < totalLength; i++)
      {
        data[i] = charByte;
      }

      Buffer.BlockCopy(this.data, 0, data, i, this.data.Length);

      return new ASCIIString(data);
    }
    public ASCIIString PadRight(int totalLength)
    {
      return this.PadRight(totalLength, ASCIIChars.Space);
    }
    public ASCIIString PadRight(int totalLength, ASCIIChar c)
    {
      if (totalLength < this.data.Length) throw new ArgumentOutOfRangeException("totalLength");

      byte[] data = new byte[totalLength];
      byte charByte = c.ToByte();

      Buffer.BlockCopy(this.data, 0, data, 0, this.data.Length);

      for (int i = this.data.Length; i < totalLength; i++)
      {
        data[i] = charByte;
      }

      return new ASCIIString(data);
    }
    public ASCIIString Remove(int startIndex)
    {
      return this.Remove(startIndex, this.data.Length - startIndex);
    }
    public ASCIIString Remove(int startIndex, int count)
    {
      if (startIndex < 0 || startIndex > this.data.Length) throw new ArgumentOutOfRangeException("startIndex");
      if (count < 0 || startIndex + count > this.data.Length) throw new ArgumentOutOfRangeException("count");

      byte[] data = new byte[this.data.Length - count];

      Buffer.BlockCopy(this.data, 0, data, 0, startIndex);
      Buffer.BlockCopy(this.data, startIndex + count, data, startIndex, this.data.Length - count - startIndex);

      return new ASCIIString(data);
    }
    public ASCIIString Replace(ASCIIString oldString, ASCIIString newString)
    {
      if (oldString == null) throw new ArgumentNullException("oldString");
      if (newString == null) throw new ArgumentNullException("newString");

      List<int> indexes = new List<int>();
      int index = 0;

      do
      {
        index = this.IndexOf(oldString, index, false);

        if (index >= 0)
        {
          indexes.Add(index);
          index += oldString.data.Length;
        }
      }
      while (index >= 0 && index + oldString.Length < this.data.Length);

      if (indexes.Count == 0)
      {
        return this.Clone();
      }

      byte[] data = new byte[this.data.Length - (oldString.data.Length * indexes.Count) + (newString.data.Length * indexes.Count)];

      int oldIndex = 0;
      int newIndex = 0;

      foreach (int stringIndex in indexes)
      {
        Buffer.BlockCopy(this.data, oldIndex, data, newIndex, stringIndex - oldIndex);
        newIndex += stringIndex - oldIndex;
        oldIndex = stringIndex + oldString.data.Length;
        Buffer.BlockCopy(newString.data, 0, data, newIndex, newString.data.Length);
        newIndex += newString.data.Length;
      }

      Buffer.BlockCopy(this.data, oldIndex, data, newIndex, this.data.Length - oldIndex);

      return new ASCIIString(data);
    }
    public ASCIIString Replace(ASCIIChar oldChar, ASCIIChar newChar)
    {
      if (oldChar == newChar) return this.Clone();

      ASCIIChar[] oldChars = new ASCIIChar[] { oldChar };

      List<int> indexes = new List<int>();
      int index = 0;

      do
      {
        index = this.IndexOfAny(oldChars, index, false);

        if (index >= 0)
        {
          indexes.Add(index);
          index++;
        }
      }
      while (index >= 0 && index + 1 < this.data.Length);

      if (indexes.Count == 0) return this.Clone();

      byte[] data = new byte[this.data.Length];

      int oldIndex = 0;
      int newIndex = 0;

      foreach (int stringIndex in indexes)
      {
        Buffer.BlockCopy(this.data, oldIndex, data, newIndex, stringIndex - oldIndex);
        newIndex += stringIndex - oldIndex;
        oldIndex = stringIndex + 1;
        data[newIndex] = newChar.ToByte();
        newIndex++;
      }

      Buffer.BlockCopy(this.data, oldIndex, data, newIndex, this.data.Length - oldIndex);

      return new ASCIIString(data);
    }
    public ASCIIString[] Split(params ASCIIString[] seperators)
    {
      return this.Split(seperators, int.MaxValue, StringSplitOptions.None);
    }
    public ASCIIString[] Split(IEnumerable<ASCIIString> seperators)
    {
      return this.Split(seperators, int.MaxValue, StringSplitOptions.None);
    }
    public ASCIIString[] Split(IEnumerable<ASCIIString> seperators, StringSplitOptions options)
    {
      return this.Split(seperators, int.MaxValue, options);
    }
    public ASCIIString[] Split(IEnumerable<ASCIIString> seperators, int count)
    {
      return this.Split(seperators, count, StringSplitOptions.None);
    }
    public ASCIIString[] Split(IEnumerable<ASCIIString> seperators, int count, StringSplitOptions options)
    {
      List<ASCIIString> parts = new List<ASCIIString>();

      int startIndex = 0;

      for (int dataIndex = 0; dataIndex < this.data.Length; dataIndex++)
      {
        int charsFound = 0;
        bool found = false;

        foreach (ASCIIString seperator in seperators)
        {
          charsFound = 0;

          if (dataIndex + seperator.data.Length > this.data.Length) break;

          for (int sepIndex = 0; sepIndex < seperator.Length; sepIndex++)
          {
            if (this.data[dataIndex + sepIndex] == seperator[sepIndex]) charsFound++;
            else charsFound = 0;
          }

          if (charsFound == seperator.data.Length) found = true;
        }

        if (found)
        {
          ASCIIString part = this.Substring(startIndex, dataIndex - startIndex);
          if (part.data.Length > 0 || options == StringSplitOptions.None)
          {
            parts.Add(part);
          }
          startIndex = dataIndex + charsFound;
          dataIndex += charsFound - 1;

          if (parts.Count + 1 == count) break;
        }
      }

      ASCIIString remainingPart = this.Substring(startIndex);
      if (remainingPart.data.Length > 0 || options == StringSplitOptions.None)
      {
        parts.Add(remainingPart);
      }

      return parts.ToArray();
    }
    public ASCIIString[] Split(params ASCIIChar[] seperators)
    {
      return this.Split(seperators, int.MaxValue, StringSplitOptions.None);
    }
    public ASCIIString[] Split(IEnumerable<ASCIIChar> seperators)
    {
      return this.Split(seperators, int.MaxValue, StringSplitOptions.None);
    }
    public ASCIIString[] Split(IEnumerable<ASCIIChar> seperators, StringSplitOptions options)
    {
      return this.Split(seperators, int.MaxValue, options);
    }
    public ASCIIString[] Split(IEnumerable<ASCIIChar> seperators, int count)
    {
      return this.Split(seperators, count, StringSplitOptions.None);
    }
    public ASCIIString[] Split(IEnumerable<ASCIIChar> seperators, int count, StringSplitOptions options)
    {
      List<ASCIIString> parts = new List<ASCIIString>();

      int startIndex = 0;

      for (int dataIndex = 0; dataIndex < this.data.Length; dataIndex++)
      {
        bool found = false;

        foreach (ASCIIChar seperator in seperators)
        {
          if (this.data[dataIndex] == seperator)
          {
            found = true;
          }
        }

        if (found)
        {
          ASCIIString part = this.Substring(startIndex, dataIndex - startIndex);
          if (part.data.Length > 0 || options == StringSplitOptions.None)
          {
            parts.Add(part);
          }

          startIndex = dataIndex + 1;

          if (parts.Count + 1 == count) break;
        }
      }

      ASCIIString remainingPart = this.Substring(startIndex);
      if (remainingPart.data.Length > 0 || options == StringSplitOptions.None)
      {
        parts.Add(remainingPart);
      }

      return parts.ToArray();
    }
    public bool StartsWith(ASCIIString value)
    {
      return this.StartsWith(value, false);
    }
    public bool StartsWith(ASCIIString value, bool ignoreCase)
    {
      if (value == null) throw new ArgumentNullException("value");
      return ASCIIString.Compare(this, 0, value, 0, value.Length, ignoreCase) == 0;
    }
    public ASCIIString Substring(int startIndex)
    {
      return this.Substring(startIndex, this.data.Length - startIndex);
    }
    public ASCIIString Substring(int startIndex, int length)
    {
      if (startIndex < 0 || startIndex > data.Length) throw new ArgumentOutOfRangeException("startIndex");
      if (length < 0 || startIndex + length > data.Length) throw new ArgumentOutOfRangeException("length");

      byte[] newData = new byte[length];
      Buffer.BlockCopy(data, startIndex, newData, 0, length);
      return new ASCIIString(newData);
    }
    public ASCIIChar[] ToCharArray()
    {
      ASCIIChar[] chars = new ASCIIChar[this.data.Length];
      for (int i = 0; i < this.data.Length; i++)
      {
        chars[i] = new ASCIIChar(this.data[i]);
      }
      return chars;
    }
    public ASCIIString ToLower()
    {
      ASCIIString s = ASCIIString.Copy(this);

      for (int i = 0; i < s.data.Length; i++)
      {
        byte b = s.data[i];
        if (ASCIIChar.IsUpper(b))
        {
          s.data[i] = ASCIIChar.ToLower(b);
        }
      }

      return s;
    }
    public ASCIIString ToUpper()
    {
      ASCIIString s = ASCIIString.Copy(this);

      for (int i = 0; i < s.data.Length; i++)
      {
        byte b = s.data[i];
        if (ASCIIChar.IsLower(b))
        {
          s.data[i] = ASCIIChar.ToUpper(b);
        }
      }

      return s;
    }
    public ASCIIString Trim()
    {
      int charsAtStart = 0;
      int charsAtEnd = 0;

      for (int i = 0; i < this.data.Length; i++)
      {
        if (ASCIIChar.IsWhitespace(this.data[i]))
        {
          charsAtStart++;
        }
        else
        {
          break;
        }
      }

      for (int i = this.data.Length - 1; i >= charsAtStart; i--)
      {
        if (ASCIIChar.IsWhitespace(this.data[i]))
        {
          charsAtEnd++;
        }
        else
        {
          break;
        }
      }

      byte[] data = new byte[this.data.Length - charsAtStart - charsAtEnd];
      Buffer.BlockCopy(this.data, charsAtStart, data, 0, data.Length);
      return new ASCIIString(data);
    }
    public ASCIIString Trim(params ASCIIChar[] values)
    {
      int charsAtStart = 0;
      int charsAtEnd = 0;

      for (int i = 0; i < this.data.Length; i++)
      {
        bool found = false;

        foreach (ASCIIChar c in values)
        {
          if (this.data[i].Equals(c.ToByte()))
          {
            charsAtStart++;
            found = true;
            break;
          }
        }

        if (!found) break;
      }

      for (int i = this.data.Length - 1; i >= charsAtStart; i--)
      {
        bool found = false;

        foreach (ASCIIChar c in values)
        {
          if (this.data[i].Equals(c.ToByte()))
          {
            charsAtEnd++;
            found = true;
            break;
          }
        }

        if (!found) break;
      }

      byte[] data = new byte[this.data.Length - charsAtStart - charsAtEnd];
      Buffer.BlockCopy(this.data, charsAtStart, data, 0, data.Length);
      return new ASCIIString(data);
    }
    public ASCIIString TrimEnd()
    {
      int charsAtEnd = 0;

      for (int i = this.data.Length - 1; i >= 0; i--)
      {
        if (ASCIIChar.IsWhitespace(this.data[i]))
        {
          charsAtEnd++;
        }
        else
        {
          break;
        }
      }

      byte[] data = new byte[this.data.Length - charsAtEnd];
      Buffer.BlockCopy(this.data, 0, data, 0, data.Length);
      return new ASCIIString(data);
    }
    public ASCIIString TrimEnd(params ASCIIChar[] values)
    {
      int charsAtEnd = 0;

      for (int i = this.data.Length - 1; i >= 0; i--)
      {
        bool found = false;

        foreach (ASCIIChar c in values)
        {
          if (this.data[i].Equals(c.ToByte()))
          {
            charsAtEnd++;
            found = true;
            break;
          }
        }

        if (!found) break;
      }

      byte[] data = new byte[this.data.Length - charsAtEnd];
      Buffer.BlockCopy(this.data, 0, data, 0, data.Length);
      return new ASCIIString(data);
    }
    public ASCIIString TrimStart()
    {
      int charsAtStart = 0;

      for (int i = 0; i < this.data.Length; i++)
      {
        if (ASCIIChar.IsWhitespace(this.data[i]))
        {
          charsAtStart++;
        }
        else
        {
          break;
        }
      }

      byte[] data = new byte[this.data.Length - charsAtStart];
      Buffer.BlockCopy(this.data, charsAtStart, data, 0, data.Length);
      return new ASCIIString(data);
    }
    public ASCIIString TrimStart(params ASCIIChar[] values)
    {
      int charsAtStart = 0;

      for (int i = 0; i < this.data.Length; i++)
      {
        bool found = false;

        foreach (ASCIIChar c in values)
        {
          if (this.data[i].Equals(c.ToByte()))
          {
            charsAtStart++;
            found = true;
            break;
          }
        }

        if (!found) break;
      }

      byte[] data = new byte[this.data.Length - charsAtStart];
      Buffer.BlockCopy(this.data, charsAtStart, data, 0, data.Length);
      return new ASCIIString(data);
    }

    public override string ToString()
    {
      return Encoding.ASCII.GetString(this.data);
    }

    public IEnumerator<ASCIIChar> GetEnumerator()
    {
      foreach (byte b in this.data)
      {
        yield return new ASCIIChar(b);
      }
    }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return this.GetEnumerator();
    }

    int IComparable<ASCIIString>.CompareTo(ASCIIString other)
    {
      return this.CompareTo(other);
    }
  }
}
