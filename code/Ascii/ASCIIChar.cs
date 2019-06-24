using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sys
{
  public struct ASCIIChar
  {
    public static implicit operator Char(ASCIIChar value)
    {
      return (char)value.asciiCode;
    }

    public static bool ValidateByte(byte value)
    {
      return value >= 0 && value <= 127;
    }
    public static bool IsControl(byte value)
    {
      return (value >= 0 && value <= 31) ||
          value == 127;
    }
    public static bool IsDigit(byte value)
    {
      return value >= 48 && value <= 57;
    }
    public static bool IsLetter(byte value)
    {
      return (value >= 65 && value <= 90) ||
          (value >= 97 && value <= 122);
    }
    public static bool IsLetterOrDigit(byte value)
    {
      return (value >= 48 && value <= 57) ||
          (value >= 65 && value <= 90) ||
          (value >= 97 && value <= 122);
    }
    public static bool IsLower(byte value)
    {
      return value >= 97 && value <= 122;
    }
    public static bool IsPunctuation(byte value)
    {
      return (value >= 33 && value <= 35) ||
          (value >= 37 && value <= 42) ||
          (value >= 44 && value <= 47) ||
          (value >= 58 && value <= 59) ||
          (value >= 63 && value <= 64) ||
          (value >= 91 && value <= 93) ||
          value == 95 ||
          value == 123 ||
          value == 125;
    }
    public static bool IsSymbol(byte value)
    {
      return value == 36 ||
          value == 43 ||
          (value >= 60 && value <= 62) ||
          value == 94 ||
          value == 96 ||
          value == 124 ||
          value == 126;
    }
    public static bool IsUpper(byte value)
    {
      return value >= 65 && value <= 90;
    }
    public static bool IsWhitespace(byte value)
    {
      return value == 0 || (value >= 9 && value <= 13) || value == 32;

    }
    public static byte ToLower(byte value)
    {
      if (ASCIIChar.IsUpper(value)) return (byte)(value - 32);
      return value;
    }
    public static byte ToUpper(byte value)
    {
      if (ASCIIChar.IsLower(value)) return (byte)(value + 32);
      return value;
    }

    private readonly byte asciiCode;

    public int CompareTo(ASCIIChar value)
    {
      return this.asciiCode.CompareTo(value.asciiCode);
    }
    public bool Equals(ASCIIChar value)
    {
      return this.asciiCode.Equals(value.asciiCode);
    }
    public bool IsControl()
    {
      return ASCIIChar.IsControl(this.asciiCode);
    }
    public bool IsDigit()
    {
      return ASCIIChar.IsDigit(this.asciiCode);
    }
    public bool IsLetter()
    {
      return ASCIIChar.IsLetter(this.asciiCode);
    }
    public bool IsLetterOrDigit()
    {
      return ASCIIChar.IsLetterOrDigit(this.asciiCode);
    }
    public bool IsLower()
    {
      return ASCIIChar.IsLower(this.asciiCode);
    }
    public bool IsPunctuation()
    {
      return ASCIIChar.IsPunctuation(this.asciiCode);
    }
    public bool IsSymbol()
    {
      return ASCIIChar.IsSymbol(this.asciiCode);
    }
    public bool IsUpper()
    {
      return ASCIIChar.IsUpper(this.asciiCode);
    }
    public bool IsWhitespace()
    {
      return ASCIIChar.IsWhitespace(this.asciiCode);

    }
    public ASCIIChar ToLower()
    {
      if (this.IsUpper())
      {
        return new ASCIIChar((byte)(this.asciiCode + 32));
      }

      return this;
    }
    public byte ToByte()
    {
      return this.asciiCode;
    }
    public char ToChar()
    {
      return (char)this.asciiCode;
    }
    public override string ToString()
    {
      return this.ToChar().ToString();
    }
    public ASCIIString ToASCIIString()
    {
      return new ASCIIString(new byte[] { this.asciiCode }, 0, 1);
    }
    public ASCIIChar ToUpper()
    {
      if (this.IsLower())
      {
        return new ASCIIChar((byte)(this.asciiCode - 32));
      }

      return this;
    }

    public ASCIIChar(byte asciiCode)
    {
      if (!ASCIIChar.ValidateByte(asciiCode))
      {
        throw new ArgumentOutOfRangeException("asciiCode");
      }

      this.asciiCode = asciiCode;
    }

    public static ASCIIChar Parse(char value)
    {
      if (value < 0 || value > 127)
      {
        throw new ArgumentOutOfRangeException("value");
      }

      return new ASCIIChar((byte)value);
    }
    public static ASCIIChar Parse(string value)
    {
      if (value == null)
      {
        throw new ArgumentNullException("value");
      }

      if (value.Length != 1)
      {
        throw new FormatException();
      }

      if (value[0] > 127)
      {
        throw new ArgumentOutOfRangeException("value");
      }

      return new ASCIIChar((byte)value[0]);
    }
  }
}
