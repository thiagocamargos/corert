// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////
//
//
//  Purpose:  This Class defines behaviors specific to a writing system.
//            A writing system is the collection of scripts and
//            orthographic rules required to represent a language as text.
//
//
////////////////////////////////////////////////////////////////////////////

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;

namespace System.Globalization
{
    public partial class TextInfo : ICloneable
    {
        ////--------------------------------------------------------------------//
        ////                        Internal Information                        //
        ////--------------------------------------------------------------------//

        private enum Tristate : byte
        {
            NotInitialized,
            True,
            False,
        }

        ////
        ////  Variables.
        ////

        private String _listSeparator;
        private bool _isReadOnly = false;

        ////      _cultureName is the name of the creating culture.  Note that we consider this authoratative,
        ////              if the culture's textinfo changes when deserializing, then behavior may change.
        ////              (ala Whidbey behavior).  This is the only string Arrowhead needs to serialize.
        ////      _cultureData is the data that backs this class.
        ////      _textInfoName is the actual name of the textInfo (from cultureData.STEXTINFO)
        ////              this can be the same as _cultureName on Silverlight since the OS knows
        ////              how to do the sorting. However in the desktop, when we call the sorting dll, it doesn't
        ////              know how to resolve custom locle names to sort ids so we have to have alredy resolved this.
        ////      

        private readonly String _cultureName;      // Name of the culture that created this text info
        private readonly CultureData _cultureData;      // Data record for the culture that made us, not for this textinfo
        private readonly String _textInfoName;     // Name of the text info we're using (ie: m_cultureData.STEXTINFO)
        private Tristate _isAsciiCasingSameAsInvariant = Tristate.NotInitialized;

        // Invariant text info
        internal static TextInfo Invariant
        {
            get
            {
                if (s_Invariant == null)
                    s_Invariant = new TextInfo(CultureData.Invariant);
                return s_Invariant;
            }
        }
        internal volatile static TextInfo s_Invariant;

        //
        // Internal ordinal comparison functions
        //

        internal static int GetHashCodeOrdinalIgnoreCase(String s)
        {
            // This is the same as an case insensitive hash for Invariant
            // (not necessarily true for sorting, but OK for casing & then we apply normal hash code rules)
            return (Invariant.GetCaseInsensitiveHashCode(s));
        }

        // Currently we don't have native functions to do this, so we do it the hard way
        internal static int IndexOfStringOrdinalIgnoreCase(String source, String value, int startIndex, int count)
        {
            if (count > source.Length || count < 0 || startIndex < 0 || startIndex >= source.Length || startIndex + count > source.Length)
            {
                return -1;
            }

            return CompareInfo.IndexOfOrdinal(source, value, startIndex, count, ignoreCase: true);
        }

        // Currently we don't have native functions to do this, so we do it the hard way
        internal static int LastIndexOfStringOrdinalIgnoreCase(String source, String value, int startIndex, int count)
        {
            if (count > source.Length || count < 0 || startIndex < 0 || startIndex > source.Length - 1 || (startIndex - count + 1 < 0))
            {
                return -1;
            }

            return CompareInfo.LastIndexOfOrdinal(source, value, startIndex, count, ignoreCase: true);
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  CodePage
        //
        //  Returns the number of the code page used by this writing system.
        //  The type parameter can be any of the following values:
        //      ANSICodePage
        //      OEMCodePage
        //      MACCodePage
        //
        ////////////////////////////////////////////////////////////////////////


        public virtual int ANSICodePage
        {
            get
            {
                return (_cultureData.IDEFAULTANSICODEPAGE);
            }
        }


        public virtual int OEMCodePage
        {
            get
            {
                return (_cultureData.IDEFAULTOEMCODEPAGE);
            }
        }


        public virtual int MacCodePage
        {
            get
            {
                return (_cultureData.IDEFAULTMACCODEPAGE);
            }
        }


        public virtual int EBCDICCodePage
        {
            get
            {
                return (_cultureData.IDEFAULTEBCDICCODEPAGE);
            }
        }

        public int LCID
        {
            get
            {
                // Just use the LCID from our text info name
                return CultureInfo.GetCultureInfo(_textInfoName).LCID;
            }
        }

        //////////////////////////////////////////////////////////////////////////
        ////
        ////  CultureName
        ////
        ////  The name of the culture associated with the current TextInfo.
        ////
        //////////////////////////////////////////////////////////////////////////
        public string CultureName
        {
            get
            {
                return _textInfoName;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  IsReadOnly
        //
        //  Detect if the object is readonly.
        //
        ////////////////////////////////////////////////////////////////////////
        public bool IsReadOnly
        {
            get { return (_isReadOnly); }
        }

        //////////////////////////////////////////////////////////////////////////
        ////
        ////  Clone
        ////
        ////  Is the implementation of ICloneable.
        ////
        //////////////////////////////////////////////////////////////////////////
        public virtual object Clone()
        {
            object o = MemberwiseClone();
            ((TextInfo)o).SetReadOnlyState(false);
            return (o);
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  ReadOnly
        //
        //  Create a cloned readonly instance or return the input one if it is 
        //  readonly.
        //
        ////////////////////////////////////////////////////////////////////////
        public static TextInfo ReadOnly(TextInfo textInfo)
        {
            if (textInfo == null) { throw new ArgumentNullException(nameof(textInfo)); }
            Contract.EndContractBlock();
            if (textInfo.IsReadOnly) { return (textInfo); }

            TextInfo clonedTextInfo = (TextInfo)(textInfo.MemberwiseClone());
            clonedTextInfo.SetReadOnlyState(true);

            return (clonedTextInfo);
        }

        private void VerifyWritable()
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
            }
        }

        internal void SetReadOnlyState(bool readOnly)
        {
            _isReadOnly = readOnly;
        }


        ////////////////////////////////////////////////////////////////////////
        //
        //  ListSeparator
        //
        //  Returns the string used to separate items in a list.
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual String ListSeparator
        {
            get
            {
                if (_listSeparator == null)
                {
                    _listSeparator = _cultureData.SLIST;
                }
                return (_listSeparator);
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value), SR.ArgumentNull_String);
                }
                VerifyWritable();
                _listSeparator = value;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  ToLower
        //
        //  Converts the character or string to lower case.  Certain locales
        //  have different casing semantics from the file systems in Win32.
        //
        ////////////////////////////////////////////////////////////////////////
        public unsafe virtual char ToLower(char c)
        {
            if (IsAscii(c) && IsAsciiCasingSameAsInvariant)
            {
                return ToLowerAsciiInvariant(c);
            }
            return (ChangeCase(c, toUpper: false));
        }

        public unsafe virtual String ToLower(String str)
        {
            if (str == null) { throw new ArgumentNullException(nameof(str)); }

            return ChangeCase(str, toUpper: false);
        }

        private static Char ToLowerAsciiInvariant(Char c)
        {
            if ((uint)(c - 'A') <= (uint)('Z' - 'A'))
            {
                c = (Char)(c | 0x20);
            }
            return c;
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  ToUpper
        //
        //  Converts the character or string to upper case.  Certain locales
        //  have different casing semantics from the file systems in Win32.
        //
        ////////////////////////////////////////////////////////////////////////
        public unsafe virtual char ToUpper(char c)
        {
            if (IsAscii(c) && IsAsciiCasingSameAsInvariant)
            {
                return ToUpperAsciiInvariant(c);
            }
            return (ChangeCase(c, toUpper: true));
        }

        public unsafe virtual String ToUpper(String str)
        {
            if (str == null) { throw new ArgumentNullException(nameof(str)); }

            return ChangeCase(str, toUpper: true);
        }

        private static Char ToUpperAsciiInvariant(Char c)
        {
            if ((uint)(c - 'a') <= (uint)('z' - 'a'))
            {
                c = (Char)(c & ~0x20);
            }
            return c;
        }

        private static bool IsAscii(Char c)
        {
            return c < 0x80;
        }

        private bool IsAsciiCasingSameAsInvariant
        {
            get
            {
                if (_isAsciiCasingSameAsInvariant == Tristate.NotInitialized)
                {
                    _isAsciiCasingSameAsInvariant = CultureInfo.GetCultureInfo(_textInfoName).CompareInfo.Compare("abcdefghijklmnopqrstuvwxyz",
                                                                             "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                                                                             CompareOptions.IgnoreCase) == 0 ? Tristate.True : Tristate.False;
                }
                return _isAsciiCasingSameAsInvariant == Tristate.True;
            }
        }

        // IsRightToLeft
        //
        // Returns true if the dominant direction of text and UI such as the relative position of buttons and scroll bars
        //
        public bool IsRightToLeft
        {
            get
            {
                return _cultureData.IsRightToLeft;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  Equals
        //
        //  Implements Object.Equals().  Returns a boolean indicating whether
        //  or not object refers to the same CultureInfo as the current instance.
        //
        ////////////////////////////////////////////////////////////////////////
        public override bool Equals(Object obj)
        {
            TextInfo that = obj as TextInfo;

            if (that != null)
            {
                return this.CultureName.Equals(that.CultureName);
            }

            return (false);
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  GetHashCode
        //
        //  Implements Object.GetHashCode().  Returns the hash code for the
        //  CultureInfo.  The hash code is guaranteed to be the same for CultureInfo A
        //  and B where A.Equals(B) is true.
        //
        ////////////////////////////////////////////////////////////////////////
        public override int GetHashCode()
        {
            return (this.CultureName.GetHashCode());
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  ToString
        //
        //  Implements Object.ToString().  Returns a string describing the
        //  TextInfo.
        //
        ////////////////////////////////////////////////////////////////////////
        public override String ToString()
        {
            return ("TextInfo - " + _cultureData.CultureName);
        }

        //
        // Titlecasing:
        // -----------
        // Titlecasing refers to a casing practice wherein the first letter of a word is an uppercase letter
        // and the rest of the letters are lowercase.  The choice of which words to titlecase in headings
        // and titles is dependent on language and local conventions.  For example, "The Merry Wives of Windor"
        // is the appropriate titlecasing of that play's name in English, with the word "of" not titlecased.
        // In German, however, the title is "Die lustigen Weiber von Windsor," and both "lustigen" and "von"
        // are not titlecased.  In French even fewer words are titlecased: "Les joyeuses commeres de Windsor."
        //
        // Moreover, the determination of what actually constitutes a word is language dependent, and this can
        // influence which letter or letters of a "word" are uppercased when titlecasing strings.  For example
        // "l'arbre" is considered two words in French, whereas "can't" is considered one word in English.
        //
        public unsafe String ToTitleCase(String str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }
            Contract.EndContractBlock();
            if (str.Length == 0)
            {
                return (str);
            }

            StringBuilder result = new StringBuilder();
            string lowercaseData = null;

            for (int i = 0; i < str.Length; i++)
            {
                UnicodeCategory charType;
                int charLen;

                charType = CharUnicodeInfo.InternalGetUnicodeCategory(str, i, out charLen);
                if (Char.CheckLetter(charType))
                {
                    // Do the titlecasing for the first character of the word.
                    i = AddTitlecaseLetter(ref result, ref str, i, charLen) + 1;

                    //
                    // Convert the characters until the end of the this word
                    // to lowercase.
                    //
                    int lowercaseStart = i;

                    //
                    // Use hasLowerCase flag to prevent from lowercasing acronyms (like "URT", "USA", etc)
                    // This is in line with Word 2000 behavior of titlecasing.
                    //
                    bool hasLowerCase = (charType == UnicodeCategory.LowercaseLetter);
                    // Use a loop to find all of the other letters following this letter.
                    while (i < str.Length)
                    {
                        charType = CharUnicodeInfo.InternalGetUnicodeCategory(str, i, out charLen);
                        if (IsLetterCategory(charType))
                        {
                            if (charType == UnicodeCategory.LowercaseLetter)
                            {
                                hasLowerCase = true;
                            }
                            i += charLen;
                        }
                        else if (str[i] == '\'')
                        {
                            i++;
                            if (hasLowerCase)
                            {
                                if (lowercaseData == null)
                                {
                                    lowercaseData = this.ToLower(str);
                                }
                                result.Append(lowercaseData, lowercaseStart, i - lowercaseStart);
                            }
                            else
                            {
                                result.Append(str, lowercaseStart, i - lowercaseStart);
                            }
                            lowercaseStart = i;
                            hasLowerCase = true;
                        }
                        else if (!IsWordSeparator(charType))
                        {
                            // This category is considered to be part of the word.
                            // This is any category that is marked as false in wordSeprator array.
                            i += charLen;
                        }
                        else
                        {
                            // A word separator. Break out of the loop.
                            break;
                        }
                    }

                    int count = i - lowercaseStart;

                    if (count > 0)
                    {
                        if (hasLowerCase)
                        {
                            if (lowercaseData == null)
                            {
                                lowercaseData = this.ToLower(str);
                            }
                            result.Append(lowercaseData, lowercaseStart, count);
                        }
                        else
                        {
                            result.Append(str, lowercaseStart, count);
                        }
                    }

                    if (i < str.Length)
                    {
                        // not a letter, just append it
                        i = AddNonLetter(ref result, ref str, i, charLen);
                    }
                }
                else
                {
                    // not a letter, just append it
                    i = AddNonLetter(ref result, ref str, i, charLen);
                }
            }
            return (result.ToString());
        }

        private static int AddNonLetter(ref StringBuilder result, ref String input, int inputIndex, int charLen)
        {
            Debug.Assert(charLen == 1 || charLen == 2, "[TextInfo.AddNonLetter] CharUnicodeInfo.InternalGetUnicodeCategory returned an unexpected charLen!");
            if (charLen == 2)
            {
                // Surrogate pair
                result.Append(input[inputIndex++]);
                result.Append(input[inputIndex]);
            }
            else
            {
                result.Append(input[inputIndex]);
            }
            return inputIndex;
        }

        private int AddTitlecaseLetter(ref StringBuilder result, ref String input, int inputIndex, int charLen)
        {
            Debug.Assert(charLen == 1 || charLen == 2, "[TextInfo.AddTitlecaseLetter] CharUnicodeInfo.InternalGetUnicodeCategory returned an unexpected charLen!");

            // for surrogate pairs do a simple ToUpper operation on the substring
            if (charLen == 2)
            {
                // Surrogate pair
                result.Append(ToUpper(input.Substring(inputIndex, charLen)));
                inputIndex++;
            }
            else
            {
                switch (input[inputIndex])
                {
                    //
                    // For AppCompat, the Titlecase Case Mapping data from NDP 2.0 is used below.
                    case (char)0x01C4:  // DZ with Caron -> Dz with Caron
                    case (char)0x01C5:  // Dz with Caron -> Dz with Caron
                    case (char)0x01C6:  // dz with Caron -> Dz with Caron
                        result.Append((char)0x01C5);
                        break;
                    case (char)0x01C7:  // LJ -> Lj
                    case (char)0x01C8:  // Lj -> Lj
                    case (char)0x01C9:  // lj -> Lj
                        result.Append((char)0x01C8);
                        break;
                    case (char)0x01CA:  // NJ -> Nj
                    case (char)0x01CB:  // Nj -> Nj
                    case (char)0x01CC:  // nj -> Nj
                        result.Append((char)0x01CB);
                        break;
                    case (char)0x01F1:  // DZ -> Dz
                    case (char)0x01F2:  // Dz -> Dz
                    case (char)0x01F3:  // dz -> Dz
                        result.Append((char)0x01F2);
                        break;
                    default:
                        result.Append(ToUpper(input[inputIndex]));
                        break;
                }
            }
            return inputIndex;
        }

        //
        // Used in ToTitleCase():
        // When we find a starting letter, the following array decides if a category should be
        // considered as word seprator or not.
        //
        private const int c_wordSeparatorMask =
            /* false */ (0 <<  0) | // UppercaseLetter = 0,
            /* false */ (0 <<  1) | // LowercaseLetter = 1,
            /* false */ (0 <<  2) | // TitlecaseLetter = 2,
            /* false */ (0 <<  3) | // ModifierLetter = 3,
            /* false */ (0 <<  4) | // OtherLetter = 4,
            /* false */ (0 <<  5) | // NonSpacingMark = 5,
            /* false */ (0 <<  6) | // SpacingCombiningMark = 6,
            /* false */ (0 <<  7) | // EnclosingMark = 7,
            /* false */ (0 <<  8) | // DecimalDigitNumber = 8,
            /* false */ (0 <<  9) | // LetterNumber = 9,
            /* false */ (0 << 10) | // OtherNumber = 10,
            /* true  */ (1 << 11) | // SpaceSeparator = 11,
            /* true  */ (1 << 12) | // LineSeparator = 12,
            /* true  */ (1 << 13) | // ParagraphSeparator = 13,
            /* true  */ (1 << 14) | // Control = 14,
            /* true  */ (1 << 15) | // Format = 15,
            /* false */ (0 << 16) | // Surrogate = 16,
            /* false */ (0 << 17) | // PrivateUse = 17,
            /* true  */ (1 << 18) | // ConnectorPunctuation = 18,
            /* true  */ (1 << 19) | // DashPunctuation = 19,
            /* true  */ (1 << 20) | // OpenPunctuation = 20,
            /* true  */ (1 << 21) | // ClosePunctuation = 21,
            /* true  */ (1 << 22) | // InitialQuotePunctuation = 22,
            /* true  */ (1 << 23) | // FinalQuotePunctuation = 23,
            /* true  */ (1 << 24) | // OtherPunctuation = 24,
            /* true  */ (1 << 25) | // MathSymbol = 25,
            /* true  */ (1 << 26) | // CurrencySymbol = 26,
            /* true  */ (1 << 27) | // ModifierSymbol = 27,
            /* true  */ (1 << 28) | // OtherSymbol = 28,
            /* false */ (0 << 29);  // OtherNotAssigned = 29;
        
        private static bool IsWordSeparator(UnicodeCategory category)
        {
            return (c_wordSeparatorMask & (1 << (int)category)) != 0;
        }

        private static bool IsLetterCategory(UnicodeCategory uc)
        {
            return (uc == UnicodeCategory.UppercaseLetter
                 || uc == UnicodeCategory.LowercaseLetter
                 || uc == UnicodeCategory.TitlecaseLetter
                 || uc == UnicodeCategory.ModifierLetter
                 || uc == UnicodeCategory.OtherLetter);
        }

        //
        // Get case-insensitive hash code for the specified string.
        //
        internal unsafe int GetCaseInsensitiveHashCode(String str)
        {
            // Validate inputs
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            // This code assumes that ASCII casing is safe for whatever context is passed in.
            // this is true today, because we only ever call these methods on Invariant.  It would be ideal to refactor
            // these methods so they were correct by construction and we could only ever use Invariant.

            uint hash = 5381;
            uint c;

            // Note: We assume that str contains only ASCII characters until
            // we hit a non-ASCII character to optimize the common case.
            for (int i = 0; i < str.Length; i++)
            {
                c = str[i];
                if (c >= 0x80)
                {
                    return GetCaseInsensitiveHashCodeSlow(str);
                }

                // If we have a lowercase character, ANDing off 0x20
                // will make it an uppercase character.
                if ((c - 'a') <= ('z' - 'a'))
                {
                    c = (uint)((int)c & ~0x20);
                }

                hash = ((hash << 5) + hash) ^ c;
            }

            return (int)hash;
        }

        private unsafe int GetCaseInsensitiveHashCodeSlow(String str)
        {
            Debug.Assert(str != null);

            string upper = ToUpper(str);

            uint hash = 5381;
            uint c;

            for (int i = 0; i < upper.Length; i++)
            {
                c = upper[i];
                hash = ((hash << 5) + hash) ^ c;
            }

            return (int)hash;
        }
    }
}
