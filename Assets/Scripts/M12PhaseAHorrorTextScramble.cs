using System.Text;
using UnityEngine;

/// <summary>M-1-2 段考恐怖狀態：將可見文字／數字轉為亂碼（保留 Rich Text 標籤）。</summary>
public static class M12PhaseAHorrorTextScramble
{
    private static readonly char[] Pool =
        "!@#$%^&*?~0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz錯亂熵誰".ToCharArray();

    public static string ScrambleRichText(string source)
    {
        if (string.IsNullOrEmpty(source))
            return source;

        var sb = new StringBuilder(source.Length);
        bool inTag = false;
        for (int i = 0; i < source.Length; i++)
        {
            char c = source[i];
            if (c == '<')
            {
                inTag = true;
                sb.Append(c);
                continue;
            }

            if (inTag)
            {
                sb.Append(c);
                if (c == '>')
                    inTag = false;
                continue;
            }

            if (char.IsWhiteSpace(c) || c == '/' || c == '-' || c == ':' || c == '.')
            {
                sb.Append(c);
                continue;
            }

            sb.Append(Pool[Random.Range(0, Pool.Length)]);
        }

        return sb.ToString();
    }
}
