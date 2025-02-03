﻿using FmuApiDomain.TrueApiCdn;

namespace FmuApiDomain.Configuration.Options.TrueSign
{
    public class TrueSignCdn_old
    {
        public string Host { get; set; } = string.Empty;
        public int Latency { get; set; } = 0;
        public bool IsOffline { get; private set; } = false;
        public DateTime OfflineSetDate { get; private set; } = DateTime.MinValue;

        public void BringOffline()
        {
            IsOffline = true;
            OfflineSetDate = DateTime.Now;
        }

        public void BringOnline()
        {
            IsOffline = false;
        }

        public bool Offline()
        {
            return IsOffline;
        }
    }
}
