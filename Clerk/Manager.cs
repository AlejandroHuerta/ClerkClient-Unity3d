using HighEnergy.Collections;
using Newtonsoft.Json.Linq;
using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clerk {
    public class Manager {
        readonly char[] delims = new char[] { '.', '[', ']', '-' };

        SimplePriorityQueue<StateMessage> queue = new SimplePriorityQueue<StateMessage>();

        JObject state;

        class Event {
            public string Key { get; set; }
            public List<Action<DataChange>> Handlers { get; }

            public Event() {
                Handlers = new List<Action<DataChange>>();
            }//Event
        }//Event

        Tree<Event> listeners = new Tree<Event>(new Event() { Key = "" });

        class Token {
            public string Value { get; set; }
            public string Remainder { get; set; }

            public Token() {
                Remainder = "";
            }
        }

        public class DataChange {
            public JToken Data { get; set; }
            public string Path { get; set; }
            public StateMessage.Action Action { get; set; }
        }

        public Manager(JObject state) {
            this.state = state;
        }

        public void Enqueue(StateMessage message) {

            if (message.at == null) {
                message.at = DateTime.Now.ConvertToUnixTime();
            }

            lock (queue) {
                queue.Enqueue(message, (long)message.at);
            }//lock
        }//Enqueue

        public void RegisterListener(string path, Action<DataChange> action) {
            var tokens = Tokenize(path);

            ITreeNode<Event> node = listeners;
            foreach (var token in tokens) {
                var match = node.Children.SingleOrDefault(e => e.Value.Key == token.Value);

                if (match == null) {
                    var child = new TreeNode<Event>(new Event() { Key = token.Value });
                    node.Children.Add(child);
                    node = child;
                } else {
                    node = match;
                }//else
            }//foreach

            node.Value.Handlers.Add(action);
        }//RegisterListener

        void FireEvent(string path, JToken data, StateMessage.Action action) {
            var tokens = Tokenize(path);

            ITreeNode<Event> node = listeners;
            foreach (var token in tokens) {
                var match = node.Children.SingleOrDefault(e => e.Value.Key == token.Value);

                if (match == null) {
                    return;
                } else {
                    foreach (var listener in match.Value.Handlers) {
                        listener.Invoke(new DataChange() { Data = data, Path = path, Action = action });
                    }//foreach

                    node = match;
                }//else
            }//foreach            
        }//FireEvent

        public void Update() {
            lock (queue) {
                var now = DateTime.Now.ConvertToUnixTime();
                while (queue.Count > 0) {
                    if (now >= queue.First().at) {
                        Process(queue.Dequeue());
                    } else {
                        break;
                    }//else
                }//while
            }//lock
        }//Update

        void Process(StateMessage message) {
            switch (message.action) {
            case StateMessage.Action.CONCAT:
                Concat(message.key, message.value);
                break;
            case StateMessage.Action.DEEPMERGE:
                DeepMerge(message.key, message.value);
                break;
            case StateMessage.Action.PUSH:
                Push(message.key, message.value);
                break;
            case StateMessage.Action.SET:
                Set(message.key, message.value);
                break;
            case StateMessage.Action.SPLICE:
                Splice(message.key);
                break;
            case StateMessage.Action.UNSET:
                Unset(message.key);
                break;
            case StateMessage.Action.UNSHIFT:
                Unshift(message.key, message.value);
                break;
            }
        }//Process

        List<Token> Tokenize(string path) {
            var list = new List<Token>();

            string[] tokens;
            do {
                tokens = path.Split(delims, 2, StringSplitOptions.RemoveEmptyEntries);
                var token = new Token();
                token.Value = tokens[0];
                if (tokens.Length > 1) {
                    token.Remainder = tokens[1];
                    path = tokens[1];
                }//if
                list.Add(token);
            } while (!string.IsNullOrEmpty(list.Last().Remainder));

            return list;
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
                if (int.TryParse(token.Value, out index)) {
                    jtoken = jtoken[index];
                } else {
                    var nextJtoken = jtoken[token.Value];

                    if (nextJtoken == null) {
                        nextJtoken = new JObject();
                        jtoken[token.Value] = nextJtoken;
                    }//if

                    jtoken = nextJtoken;
                }//else
            }//for

            var data = JToken.FromObject(value);
            jtoken[tokens[tokens.Count - 1].Value] = data;

            FireEvent(path, data, StateMessage.Action.SET);
        }//Set

        public void Unset(string path) {
            var tokens = Tokenize(path);

            JToken jtoken = state;

            for (int i = 0; i < tokens.Count - 1; i++) {
                var token = tokens[i];
                int index;
                if (int.TryParse(token.Value, out index)) {
                    jtoken = jtoken[index];
                } else {
                    jtoken = jtoken[token.Value];
                }//else
            }//for

            var data = ((JObject)jtoken).SelectToken(tokens[tokens.Count - 1].Value);
            ((JObject)jtoken).Remove(tokens[tokens.Count - 1].Value);

            FireEvent(path, data, StateMessage.Action.UNSET);
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

            FireEvent(path, jArray, StateMessage.Action.PUSH);
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

            FireEvent(path, jArray, StateMessage.Action.UNSHIFT);
        }

        public void Splice(string path) {
            var tokens = Tokenize(path);

            JToken jtoken = state;

            for (int i = 0; i < tokens.Count - 2; i++) {
                var token = tokens[i];
                int idx;
                if (int.TryParse(token.Value, out idx)) {
                    jtoken = jtoken[idx];
                } else {
                    jtoken = jtoken[token.Value];
                }//else
            }//for

            int index = int.Parse(tokens[tokens.Count - 2].Value);
            int count = int.Parse(tokens[tokens.Count - 1].Value);

            for (int i = 0; i < count; i++) {
                ((JArray)jtoken).RemoveAt(index);
            }//for

            FireEvent(path, null, StateMessage.Action.SPLICE);
        }//Splice

        public void Concat(string path, object obj) {
            var jtoken = JToken.FromObject(obj);
            ((JArray)state.SelectToken(path)).Add(jtoken);

            FireEvent(path, jtoken, StateMessage.Action.CONCAT);
        }//Concat

        public void DeepMerge(string path, object obj) {
            var jtoken = JToken.FromObject(obj);

            if (jtoken.Type == JTokenType.Array) {
                DeepMergeArray(path, jtoken as JArray);
            } else if (jtoken.HasValues) {
                foreach (var child in jtoken) {
                    if (child.Type == JTokenType.Object) {

                    } else if (child.Type == JTokenType.Property) {
                        DeepMergeProperty(path, child as JProperty);
                    }
                }
            } else {
                Set(path, obj);
            }
        }//DeepMerge

        void DeepMergeObject(string path, JObject obj) {

        }

        void DeepMergeProperty(string path, JProperty obj) {
            DeepMerge(path + "." + obj.Name, obj.Value);
        }

        void DeepMergeArray(string path, JArray obj) {
            for (int i = 0; i < obj.Count; i++) {
                DeepMerge(path + "[" + i.ToString() + "]", obj[i]);
            }
        }
    }
}
