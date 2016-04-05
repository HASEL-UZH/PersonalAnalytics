// Created by André Meyer at MSR
// Created: 2016-01-29
// 
// Licensed under the MIT License. 

using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;

namespace Shared.Data
{
    /// <summary>
    /// Create a MD5 function for SQLite
    /// </summary>
    [SQLiteFunction(Name = "Hash", Arguments = 1, FuncType = FunctionType.Scalar)]
    public class Hash : SQLiteFunction
    {
        public override object Invoke(object[] args)
        {
            // Use input string to calculate MD5 hash
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.Unicode.GetBytes(args[0] as string);
            if (inputBytes == null) return null;
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            if (hashBytes == null) return null;

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
