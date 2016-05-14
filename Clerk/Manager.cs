using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clerk {
    public class Manager {

        class TimedAction {
            public DateTime ExecuteTime { get; set; }
            public StateMessage Message { get; set; }
        }//TimedAction

        List<TimedAction> queue = new List<TimedAction>();

        JObject state;
        
        public Manager(JObject state) {
            this.state = state;
        }

        public void Enqueue(StateMessage message) {
            var action = new TimedAction();

            action.Message = message;

            if (message.at == null) {
                action.ExecuteTime = DateTime.Now;
            } else {
                action.ExecuteTime = Utils.FromUnixTime((long)message.at);
            }//else

            Enqueue(action);
        }//Enqueue

        void Enqueue(TimedAction action) {
            lock (queue) {
                queue.Add(action);
                queue.Sort((x, y) => x.ExecuteTime.CompareTo(y.ExecuteTime));
            }//lock
        }//Enqueue

        public void Update() {
            lock (queue) {
                while (queue.Count > 0) {
                    var now = DateTime.Now;
                    if (now > queue[0].ExecuteTime) {
                        Process(queue[0].Message);
                        queue.RemoveAt(0);
                    } else {
                        break;
                    }//else
                }//while
            }//lock
        }//Update

        void Process(StateMessage message) {

        }//Process

        List<string> Tokenize(string path) {
            return path.Split(new char[] { '.', '[', ']', '-' }).AsEnumerable().Where(s => s != "").ToList();
        }

        public object Get(string path) {
            return Get<object>(path);
        }

        public T Get<T>(string path) {
            var jtoken = state.SelectToken(path);
            
            if (jtoken == null) {
                return default(T);
            } else {
                return jtoken.ToObject<T>();
            }//else
        }//Get<T>

        public void Set(string path, object value) {
            var tokens = Tokenize(path);

            JToken jtoken = state;
            
            for (int i = 0; i < tokens.Count - 1; i++) {
                var token = tokens[i];
                int index;
                if (int.TryParse(token, out index)) {
                    jtoken = jtoken[index];
                } else {
                    var nextJtoken = jtoken[token];

                    if (nextJtoken == null) {
                        nextJtoken = new JObject();
                        jtoken[token] = nextJtoken;
                    }//if

                    jtoken = nextJtoken;
                }//else
            }//for

            jtoken[tokens[tokens.Count - 1]] = JToken.FromObject(value);
        }//Set

        public void Unset(string path) {
            var tokens = Tokenize(path);

            JToken jtoken = state;

            for (int i = 0; i < tokens.Count - 1; i++) {
                var token = tokens[i];
                int index;
                if (int.TryParse(token, out index)) {
                    jtoken = jtoken[index];
                } else {
                    jtoken = jtoken[token];
                }//else
            }//for

            ((JObject)jtoken).Remove(tokens[tokens.Count - 1]);
        }//Unset

        public void Push(string path, object obj) {            
            var jArray = state.Value<JArray>(path);
            var json = JToken.FromObject(obj);
            
            if (json is JArray) {
                foreach (var child in json.Children()) {
                    jArray.Add(child);
                }//foreach
            } else {
                jArray.Add(json);
            }//else
        }//Push

        public void Unshift(string path, object obj) {
            var jArray = state.Value<JArray>(path);
            var json = JToken.FromObject(obj);

            if (json is JArray) {
                foreach (var child in json.Children().Reverse()) {
                    jArray.AddFirst(child);
                }//foreach
            } else {
                jArray.AddFirst(json);
            }//else
        }

        public void Splice(string path) {
            var tokens = Tokenize(path);

            JToken jtoken = state;

            for (int i = 0; i < tokens.Count - 2; i++) {
                var token = tokens[i];
                int idx;
                if (int.TryParse(token, out idx)) {
                    jtoken = jtoken[idx];
                } else {
                    jtoken = jtoken[token];
                }//else
            }//for

            int index = int.Parse(tokens[tokens.Count - 2]);
            int count = int.Parse(tokens[tokens.Count - 1]);

            for (int i = 0; i < count; i++) {
                ((JArray)jtoken).RemoveAt(index);
            }//for
        }//Splice

        public void Concat(string path, object obj) {
            ((JArray)state.SelectToken(path)).Add(obj);
        }//Concat

        public void DeepMerge(string path, object obj) {
            
        }
    }
}
