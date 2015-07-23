namespace ExBuddy.NeoProfiles
{
    using System;

    internal class FishResult
    {
        public bool IsHighQuality { get; set; }

        public string Name { get; set; }

        public string FishName
        {
            get
            {
                if (this.IsHighQuality)
                {
                    return this.Name.Substring(0, this.Name.Length - 2);
                }

                return this.Name;
            }
        }
    }
}