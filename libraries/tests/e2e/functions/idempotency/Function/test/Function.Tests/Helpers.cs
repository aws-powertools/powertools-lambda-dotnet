using System.Security.Cryptography;
using System.Text;

namespace Function.Tests;

public static class Helpers
{
    public static string HashRequest(string input)
    {
        using var hashAlgorithm = MD5.Create();
        if (hashAlgorithm == null)
        {
            throw new ArgumentException("Invalid HashAlgorithm");
        }

        var data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
        var sBuilder = new StringBuilder();
        for (var i = 0; i < data.Length; i++)
        {
            sBuilder.Append(data[i].ToString("x2"));
        }

        return sBuilder.ToString();
    }
}