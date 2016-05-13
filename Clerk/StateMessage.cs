using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clerk {
    public class StateMessage {
        public enum Action {
            SET = 0,
            UNSET = 1,
            PUSH = 2,
            UNSHIFT = 3,
            SPLICE = 4,
            CONCAT = 5,
            DEEPMERGE = 6
        };

        public Action action { get; set; }
        public string key { get; set; }
        public string value { get; set; }
        public long? at { get; set; }
    }
}
