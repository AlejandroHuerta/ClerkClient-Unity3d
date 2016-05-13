﻿using System;

namespace Clerk {
    static class Utils {
        public static DateTime FromUnixTime(long unixTime) {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }
    }//Utils
}