using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Text;
using System.Threading.Tasks;

namespace Common.Services
{

	public interface ICipherService
	{
		string Encrypt(string input);
		string Decrypt(string cipherText);
		string GetVal(string key);
		string MakeMD5(string input, string salt_val = "");
		public string HashPassword(string provided_pw, string salt_val = null);
	}

	public class CipherService : ICipherService
	{
		private readonly IDataProtectionProvider _dataProtectionProvider;
		private const string _ProviderKey = "{D95D98FE-0134-427F-BB4F-6387F39187FA}";
		private const string _DefaultSalt = "{DD0069CC-F869-44D7-AD5E-C50DE27CEE89}";
		private readonly ILogger _logger;
		private readonly IConfiguration _config;

		private Dictionary<string, string> _machine_keys;
		public CipherService(ILoggerFactory loggerFactory, IDataProtectionProvider dataProtectionProvider, IConfiguration config)
		{
			_logger = loggerFactory.CreateLogger("CipherService");
			_dataProtectionProvider = dataProtectionProvider;
			_config = config;

			// on the fist startup of the cipher service I decrypt the "machine keys" / passwords / connection strings
			// going to change this to something more generic (ie. user secrets)

			var ckeys = config.GetSection("AppSettings:machine_clear_keys").Get<Dictionary<string, string>>();

			var mkeys = config.GetSection("AppSettings:machine_enc_keys").Get<Dictionary<string, string>>();

			_machine_keys = new Dictionary<string, string>();

			// check for a clear keys section, else check for encrypted keys
			if (ckeys != null)
			{
				foreach (KeyValuePair<string, string> kv in ckeys)
				{
					_machine_keys[kv.Key.ToLower()] = kv.Value; // lowercase key
				}
			}
			else if (mkeys != null)
			{
				foreach (KeyValuePair<string, string> kv in mkeys)
				{
					try
					{
						// note we decrypt them all, now, this allows us to see in the log if there was an cipher key error or something right away
						// if I could apply this to the AppSettings object I would, can't figure that out though (IOptions<> issues)
						_machine_keys[kv.Key.ToLower()] = Decrypt(kv.Value); // make sure we have a lowercase rep in the dict
					}
					catch(Exception e)
					{
						_logger.LogCritical("unable to decrypt key={k}, err={err}", kv.Key.ToLower(), e.Message);
					}
				}


			}

		}

		public string GetVal(string key)
		{
			try
			{
				//return Decrypt(_machine_keys[key.ToLower()]);
				return _machine_keys[key.ToLower()];
			}
			catch (Exception e)
			{
				_logger.LogError("GetVal error on key={key}, err={err}", key, e.Message);
			}
			return "";
		}


		// make an MD5 HEX string
		public string MakeMD5(string input, string salt_val = "")
		{
			return MakeMD5(Encoding.UTF8.GetBytes(input + salt_val));
		}

		public string MakeMD5(byte[] input)
		{
			using (MD5 md5Hash = MD5.Create())
			{
				byte[] data = md5Hash.ComputeHash(input);

				StringBuilder sBuilder = new StringBuilder();

				for (int i = 0; i < data.Length; i++)
				{
					sBuilder.Append(data[i].ToString("x2"));
				}

				return sBuilder.ToString();
			}

		}

		public string HashPassword(string provided_pw, string salt_val = null)
		{
			string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
					   password: provided_pw,
					   salt: Encoding.ASCII.GetBytes(salt_val ?? _DefaultSalt),
					   prf: KeyDerivationPrf.HMACSHA256, //.HMACSHA1,
					   iterationCount: 10000,
					   numBytesRequested: 256 / 8));

			return hashed;
		}

		public string Encrypt(string input)
		{
			try
			{
				var protector = _dataProtectionProvider.CreateProtector(_ProviderKey);
				return protector.Protect(input);
			}
			catch (CryptographicException e)
			{
				_logger.LogCritical(e, "unable to Encrypt string; {}", e.Message);
			}
			return "error";
		}

		public string Decrypt(string cipherText)
		{
			try
			{
				var protector = _dataProtectionProvider.CreateProtector(_ProviderKey);
				return protector.Unprotect(cipherText);
			}
			catch (CryptographicException e)
			{
				_logger.LogCritical(e, "unable to Decrypt string; {}", e.Message);
			}
			return "error";
		}
	}
}
