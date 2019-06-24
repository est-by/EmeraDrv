﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sys
{
  public static class ASCIIChars
  {
    public static ASCIIChar Null { get { return new ASCIIChar((byte)0); } }
    public static ASCIIChar StartOfHeading { get { return new ASCIIChar((byte)1); } }
    public static ASCIIChar StartOfText { get { return new ASCIIChar((byte)2); } }
    public static ASCIIChar EndOfText { get { return new ASCIIChar((byte)3); } }
    public static ASCIIChar EndOfTransmission { get { return new ASCIIChar((byte)4); } }
    public static ASCIIChar Enquiry { get { return new ASCIIChar((byte)5); } }
    public static ASCIIChar Acknowledge { get { return new ASCIIChar((byte)6); } }
    public static ASCIIChar Bell { get { return new ASCIIChar((byte)7); } }
    public static ASCIIChar Backspace { get { return new ASCIIChar((byte)8); } }
    public static ASCIIChar HorizontalTab { get { return new ASCIIChar((byte)9); } }
    public static ASCIIChar LineFeed { get { return new ASCIIChar((byte)10); } }
    public static ASCIIChar VerticalTab { get { return new ASCIIChar((byte)11); } }
    public static ASCIIChar FormFeed { get { return new ASCIIChar((byte)12); } }
    public static ASCIIChar CarriageReturn { get { return new ASCIIChar((byte)13); } }
    public static ASCIIChar ShiftOut { get { return new ASCIIChar((byte)14); } }
    public static ASCIIChar ShiftIn { get { return new ASCIIChar((byte)15); } }
    public static ASCIIChar DataLinkEscape { get { return new ASCIIChar((byte)16); } }
    public static ASCIIChar DeviceControl1 { get { return new ASCIIChar((byte)17); } }
    public static ASCIIChar DeviceControl2 { get { return new ASCIIChar((byte)18); } }
    public static ASCIIChar DeviceControl3 { get { return new ASCIIChar((byte)19); } }
    public static ASCIIChar DeviceControl4 { get { return new ASCIIChar((byte)20); } }
    public static ASCIIChar NegativeAcknowledge { get { return new ASCIIChar((byte)21); } }
    public static ASCIIChar SynchronousIdle { get { return new ASCIIChar((byte)22); } }
    public static ASCIIChar EndOfTransmissionBlock { get { return new ASCIIChar((byte)23); } }
    public static ASCIIChar Cancel { get { return new ASCIIChar((byte)24); } }
    public static ASCIIChar EndOfMedium { get { return new ASCIIChar((byte)25); } }
    public static ASCIIChar Substitute { get { return new ASCIIChar((byte)26); } }
    public static ASCIIChar Escape { get { return new ASCIIChar((byte)27); } }
    public static ASCIIChar FileSeperator { get { return new ASCIIChar((byte)28); } }
    public static ASCIIChar GroupSeperator { get { return new ASCIIChar((byte)29); } }
    public static ASCIIChar RecordSeperator { get { return new ASCIIChar((byte)30); } }
    public static ASCIIChar UnitSeperator { get { return new ASCIIChar((byte)31); } }
    public static ASCIIChar Space { get { return new ASCIIChar((byte)32); } }
    public static ASCIIChar ExclamationMark { get { return new ASCIIChar((byte)33); } }
    public static ASCIIChar QuotationMark { get { return new ASCIIChar((byte)34); } }
    public static ASCIIChar NumberSign { get { return new ASCIIChar((byte)35); } }
    public static ASCIIChar DollarSign { get { return new ASCIIChar((byte)36); } }
    public static ASCIIChar PercentSign { get { return new ASCIIChar((byte)37); } }
    public static ASCIIChar Ampersand { get { return new ASCIIChar((byte)38); } }
    public static ASCIIChar Apostrophe { get { return new ASCIIChar((byte)39); } }
    public static ASCIIChar OpenParentheses { get { return new ASCIIChar((byte)40); } }
    public static ASCIIChar CloseParentheses { get { return new ASCIIChar((byte)41); } }
    public static ASCIIChar Asterisk { get { return new ASCIIChar((byte)42); } }
    public static ASCIIChar PlusSign { get { return new ASCIIChar((byte)43); } }
    public static ASCIIChar Comma { get { return new ASCIIChar((byte)44); } }
    public static ASCIIChar MinusSign { get { return new ASCIIChar((byte)45); } }
    public static ASCIIChar FullStop { get { return new ASCIIChar((byte)46); } }
    public static ASCIIChar FrontSlash { get { return new ASCIIChar((byte)47); } }
    public static ASCIIChar Zero { get { return new ASCIIChar((byte)48); } }
    public static ASCIIChar One { get { return new ASCIIChar((byte)49); } }
    public static ASCIIChar Two { get { return new ASCIIChar((byte)50); } }
    public static ASCIIChar Three { get { return new ASCIIChar((byte)51); } }
    public static ASCIIChar Four { get { return new ASCIIChar((byte)52); } }
    public static ASCIIChar Five { get { return new ASCIIChar((byte)53); } }
    public static ASCIIChar Six { get { return new ASCIIChar((byte)54); } }
    public static ASCIIChar Seven { get { return new ASCIIChar((byte)55); } }
    public static ASCIIChar Eight { get { return new ASCIIChar((byte)56); } }
    public static ASCIIChar Nine { get { return new ASCIIChar((byte)57); } }
    public static ASCIIChar Colon { get { return new ASCIIChar((byte)58); } }
    public static ASCIIChar Semicolon { get { return new ASCIIChar((byte)59); } }
    public static ASCIIChar LessThanSign { get { return new ASCIIChar((byte)60); } }
    public static ASCIIChar EqualsSign { get { return new ASCIIChar((byte)61); } }
    public static ASCIIChar GreaterThanSign { get { return new ASCIIChar((byte)62); } }
    public static ASCIIChar QuestionMark { get { return new ASCIIChar((byte)63); } }
    public static ASCIIChar AtSign { get { return new ASCIIChar((byte)64); } }
    public static ASCIIChar A { get { return new ASCIIChar((byte)65); } }
    public static ASCIIChar B { get { return new ASCIIChar((byte)66); } }
    public static ASCIIChar C { get { return new ASCIIChar((byte)67); } }
    public static ASCIIChar D { get { return new ASCIIChar((byte)68); } }
    public static ASCIIChar E { get { return new ASCIIChar((byte)69); } }
    public static ASCIIChar F { get { return new ASCIIChar((byte)70); } }
    public static ASCIIChar G { get { return new ASCIIChar((byte)71); } }
    public static ASCIIChar H { get { return new ASCIIChar((byte)72); } }
    public static ASCIIChar I { get { return new ASCIIChar((byte)73); } }
    public static ASCIIChar J { get { return new ASCIIChar((byte)74); } }
    public static ASCIIChar K { get { return new ASCIIChar((byte)75); } }
    public static ASCIIChar L { get { return new ASCIIChar((byte)76); } }
    public static ASCIIChar M { get { return new ASCIIChar((byte)77); } }
    public static ASCIIChar N { get { return new ASCIIChar((byte)78); } }
    public static ASCIIChar O { get { return new ASCIIChar((byte)79); } }
    public static ASCIIChar P { get { return new ASCIIChar((byte)80); } }
    public static ASCIIChar Q { get { return new ASCIIChar((byte)81); } }
    public static ASCIIChar R { get { return new ASCIIChar((byte)82); } }
    public static ASCIIChar S { get { return new ASCIIChar((byte)83); } }
    public static ASCIIChar T { get { return new ASCIIChar((byte)84); } }
    public static ASCIIChar U { get { return new ASCIIChar((byte)85); } }
    public static ASCIIChar V { get { return new ASCIIChar((byte)86); } }
    public static ASCIIChar W { get { return new ASCIIChar((byte)87); } }
    public static ASCIIChar X { get { return new ASCIIChar((byte)88); } }
    public static ASCIIChar Y { get { return new ASCIIChar((byte)89); } }
    public static ASCIIChar Z { get { return new ASCIIChar((byte)90); } }
    public static ASCIIChar OpenSquareBrackets { get { return new ASCIIChar((byte)91); } }
    public static ASCIIChar Backslash { get { return new ASCIIChar((byte)92); } }
    public static ASCIIChar CloseSquareBrackets { get { return new ASCIIChar((byte)93); } }
    public static ASCIIChar Caret { get { return new ASCIIChar((byte)94); } }
    public static ASCIIChar Underscore { get { return new ASCIIChar((byte)95); } }
    public static ASCIIChar GraveAccent { get { return new ASCIIChar((byte)96); } }
    public static ASCIIChar a { get { return new ASCIIChar((byte)97); } }
    public static ASCIIChar b { get { return new ASCIIChar((byte)98); } }
    public static ASCIIChar c { get { return new ASCIIChar((byte)99); } }
    public static ASCIIChar d { get { return new ASCIIChar((byte)100); } }
    public static ASCIIChar e { get { return new ASCIIChar((byte)101); } }
    public static ASCIIChar f { get { return new ASCIIChar((byte)102); } }
    public static ASCIIChar g { get { return new ASCIIChar((byte)103); } }
    public static ASCIIChar h { get { return new ASCIIChar((byte)104); } }
    public static ASCIIChar i { get { return new ASCIIChar((byte)105); } }
    public static ASCIIChar j { get { return new ASCIIChar((byte)106); } }
    public static ASCIIChar k { get { return new ASCIIChar((byte)107); } }
    public static ASCIIChar l { get { return new ASCIIChar((byte)108); } }
    public static ASCIIChar m { get { return new ASCIIChar((byte)109); } }
    public static ASCIIChar n { get { return new ASCIIChar((byte)110); } }
    public static ASCIIChar o { get { return new ASCIIChar((byte)111); } }
    public static ASCIIChar p { get { return new ASCIIChar((byte)112); } }
    public static ASCIIChar q { get { return new ASCIIChar((byte)113); } }
    public static ASCIIChar r { get { return new ASCIIChar((byte)114); } }
    public static ASCIIChar s { get { return new ASCIIChar((byte)115); } }
    public static ASCIIChar t { get { return new ASCIIChar((byte)116); } }
    public static ASCIIChar u { get { return new ASCIIChar((byte)117); } }
    public static ASCIIChar v { get { return new ASCIIChar((byte)118); } }
    public static ASCIIChar w { get { return new ASCIIChar((byte)119); } }
    public static ASCIIChar x { get { return new ASCIIChar((byte)120); } }
    public static ASCIIChar y { get { return new ASCIIChar((byte)121); } }
    public static ASCIIChar z { get { return new ASCIIChar((byte)122); } }
    public static ASCIIChar OpenBraces { get { return new ASCIIChar((byte)123); } }
    public static ASCIIChar VerticalBar { get { return new ASCIIChar((byte)124); } }
    public static ASCIIChar CloseBraces { get { return new ASCIIChar((byte)125); } }
    public static ASCIIChar Tilde { get { return new ASCIIChar((byte)126); } }
    public static ASCIIChar Delete { get { return new ASCIIChar((byte)127); } }
  }
}