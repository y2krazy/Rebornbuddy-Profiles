namespace ExBuddy.NeoProfiles
{
    using System;

    public class FishResult
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
        
        public bool IsKeeper(Keeper keeper)
        {
            return string.Equals(keeper.Name, this.FishName, StringComparison.InvariantCultureIgnoreCase)
                    && (this.IsHighQuality || !keeper.OnlyHq);
        }
    }
}