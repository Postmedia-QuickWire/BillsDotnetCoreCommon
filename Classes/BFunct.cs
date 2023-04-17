using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace Common.Classes
{

	// my string extensions
	public static class StringExtensions
	{
		public static string Left(this string str, int length)
		{
			if (str?.Length > length)
				return str.Substring(0, length);
			return str;
		}

		public static string Right(this string str, int length)
		{
			if (str?.Length > length)
				return str.Substring(str.Length - length);
			return str;
		}
	}

	public static class BFunct
	{

		// returns a SortedDictionary of all env strings - sorted by keys
		public static IDictionary<string, string> GetSortedEnvironmentVars()
		{
			var dict = new SortedDictionary<string, string>();
			var env = Environment.GetEnvironmentVariables();
			// i can't seem to 'Linq' the keys into a list
			var keys = new List<string>();
			foreach (var k in env.Keys)
			{
				keys.Add(k.ToString());
			}
			keys.Sort();
			foreach (var k in keys)
			{
				Console.Out.WriteLine($"{k} = {env[k]}");
				dict[k] = env[k].ToString();
			}
			return dict;
		}


		public static string RunCommand(string command, string args) //, string wdir =null)
		{
			var process = new Process()
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = command,
					Arguments = args,
					//WorkingDirectory = wdir,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				}
			};
			process.Start();
			string output = process.StandardOutput.ReadToEnd();
			string error = process.StandardError.ReadToEnd();
			process.WaitForExit();

			if (string.IsNullOrEmpty(error)) { return output; }
			else { return error; }
		}


		public static Regex invalidFilenameCharsRegEx = new Regex(string.Format("[{0}]",
				Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()))));

		public static string CleanFilename(string filename)
		{
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars())).Trim(" _".ToCharArray());
        }
		public static bool ContainsBadChars(string p_testName)
		{
			return invalidFilenameCharsRegEx.Match(p_testName).Success;
		}

        // changed to base 36 string from Base64 -- string is URL compliant, no encoding needed!
        // makes a longer string though  -  note I encode 7 bytes at a time into a 64 bit long to avoid negatives
        // note also it's NOT case sensitive 
        public static string GenerateRandomToken(int bytes = 32)
		{
			const int bytesPerLong = 7;
			var randomNumber = new byte[bytes];
			string snum = "";
			using (var rng = RandomNumberGenerator.Create())
			{
				rng.GetBytes(randomNumber);

				for (int N=0; N < randomNumber.Length;)
				{
					long x = 0;
					for (int b=0; N < randomNumber.Length && b < bytesPerLong; N++, b++) {
						long y = randomNumber[N];
						y = y << 8 * b;
						x |= y;
					}
					snum += DecimalToArbitraryBase(x, 36);
				}
				//return Convert.ToBase64String(randomNumber);
				//return Convert.ToHexString(randomNumber);
				return snum;
			}
		}

		public static byte[] GetHash(string val, string salt)
		{
			HashAlgorithm algorithm = MD5.Create();  //or use SHA256.Create();
			return algorithm.ComputeHash(Encoding.UTF8.GetBytes(val + salt));
		}

		public static string GetHashString(string val, string salt)
		{
			StringBuilder sb = new StringBuilder();
			foreach (byte b in GetHash(val, salt))
				sb.Append(b.ToString("X2"));

			return sb.ToString();
		}

		public static string GenerateCaptchaCode(int digits = 4)
		{
			var random = new Random(); // (int)DateTime.Now.Ticks & 0x7FFFFFFF);
			string s = "";
			for (int i = 0; i < digits; i++)
				s = String.Concat(s, random.Next(10).ToString());
			return (s);
		}

		public static bool IsImageFile(string filename)
		{
			string[] ext_list = { ".jpg", ".jpeg", ".gif", ".bmp", ".png", ".tiff", ".tif" };
			string ext = Path.GetExtension(filename).ToLower(); // with dot

			foreach (string e in ext_list)
			{
				if (ext == e)
					return (true);
			}

			return (false);
		}


		public static bool DeleteFolder(string FolderPath, bool fDeleteTopFolder = false)
		{
			bool ok = _DeleteFolder(FolderPath);
			if (ok && fDeleteTopFolder)
				Directory.Delete(FolderPath, true);
			return ok;
		}

		public static bool _DeleteFolder(string FolderPath)
		{
			// Process the list of files found in the directory.
			string[] fileEntries = Directory.GetFiles(FolderPath);
			foreach (string fileName in fileEntries)
				File.Delete(fileName);

			// Recurse into subdirectories of this directory.
			string[] subdirectoryEntries = Directory.GetDirectories(FolderPath);
			foreach (string subdirectory in subdirectoryEntries)
				DeleteFolder(subdirectory);

			Directory.Delete(FolderPath, true);

			return (true);
		}


		/// <summary>
		/// Converts the given decimal number to the numeral base with the
		/// specified radix (in the range [2, 36]).
		/// </summary>
		/// <param name="decimalNumber">The number to convert.</param>
		/// <param name="radix">The radix of the destination numeral base (in the range [2, 36]).</param>
		/// <returns></returns>
		public static string DecimalToArbitraryBase(long decimalNumber, int radix)
		{
			const int BitsInLong = 64;
			const string Digits = "0123456789abcdefghijklmnopqrstuvwxyz";

			if (radix < 2 || radix > Digits.Length)
				throw new ArgumentException("The radix must be >= 2 and <= " + Digits.Length.ToString());

			if (decimalNumber == 0)
				return "0";

			int index = BitsInLong - 1;
			long currentNumber = Math.Abs(decimalNumber);
			char[] charArray = new char[BitsInLong];

			while (currentNumber != 0)
			{
				int remainder = (int)(currentNumber % radix);
				charArray[index--] = Digits[remainder];
				currentNumber = currentNumber / radix;
			}

			string result = new String(charArray, index + 1, BitsInLong - index - 1);
			if (decimalNumber < 0)
			{
				result = "-" + result;
			}

			return result;
		}

		/// <summary>
		/// Converts the given number from the numeral system with the specified
		/// radix (in the range [2, 36]) to decimal numeral system.
		/// </summary>
		/// <param name="number">The arbitrary numeral system number to convert.</param>
		/// <param name="radix">The radix of the numeral system the given number
		/// is in (in the range [2, 36]).</param>
		/// <returns></returns>
		public static long ArbitraryToDecimalBase(string number, int radix)
		{
			const string Digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

			if (radix < 2 || radix > Digits.Length)
				throw new ArgumentException("The radix must be >= 2 and <= " +
					Digits.Length.ToString());

			if (String.IsNullOrEmpty(number))
				return 0;

			// Make sure the arbitrary numeral system number is in upper case
			number = number.ToUpperInvariant();

			long result = 0;
			long multiplier = 1;
			for (int i = number.Length - 1; i >= 0; i--)
			{
				char c = number[i];
				if (i == 0 && c == '-')
				{
					// This is the negative sign symbol
					result = -result;
					break;
				}

				int digit = Digits.IndexOf(c);
				if (digit == -1)
					throw new ArgumentException(
						"Invalid character in the arbitrary numeral system number",
						"number");

				result += digit * multiplier;
				multiplier *= radix;
			}

			return result;
		}

	}
}

