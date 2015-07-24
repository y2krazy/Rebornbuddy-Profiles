namespace ExBuddy.NeoProfiles
{
    /// <summary>
    /// Extensions
    /// </summary>
    public static class Extensions
    {
        public static void Shuffle<T>(this System.Collections.Generic.IList<T> list)
        {
            if (list.Count > 1)
            {
                using (var provider = new System.Security.Cryptography.RNGCryptoServiceProvider())
                {
                    int n = list.Count;
                    while (n > 1)
                    {
                        var box = new byte[1];
                        do
                        {
                            provider.GetBytes(box);
                        }
                        while (!(box[0] < n * (byte.MaxValue / n)));

                        int k = box[0] % n;
                        n--;
                        var value = list[k];
                        list[k] = list[n];
                        list[n] = value;
                    }
                }
            }
        }
    }
}