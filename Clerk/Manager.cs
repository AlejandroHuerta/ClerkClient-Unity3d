using HighEnergy.Collections;
using Newtonsoft.Json.Linq;
using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Clerk {
    public class Manager {
        readonly char[] DELIMS = new char[] { '.', '[', ']', '-' };

        JsonMergeSettings mergeSettings = new JsonMergeSettings();

        SimplePriorityQueue<StateMessage> queue = new SimplePriorityQueue<StateMessage>();

        JObject state;

        class Event {
            public string Key { get; set; }
            public HashSet<Action> Handlers { get; private set; }

            public Event() {
                Handlers = new HashSet<Action>();
            }//Event
        }//Event

        Tree<Event> listeners = new Tree<Event>(new Event() { Key = "" });

        class Token {
            public string Value { get; set; }
            public string Remainder { get; set; }
            public bool Indexer { get; set; }

            public Token() {
                Remainder = "";
            }
        }

        public Manager(JObject state) {
            this.state = state;

            mergeSettings.MergeArrayHandling = MergeArrayHandling.Merge;
        }

        public void Enqueue(StateMessage message) {

            if (message.at == null) {
                message.at = DateTime.Now.ConvertToUnixTime();
            }

            lock (queue) {
                queue.Enqueue(message, (long)message.at);
            }//lock
        }//Enqueue

        public void RegisterListener(string path, Action action) {
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

        public void UnregisterListener(string path, Action action) {
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

            node.Value.Handlers.Remove(action);
        }

        ITreeNode<Event> GetNode(string path) {
            var tokens = Tokenize(path);

            ITreeNode<Event> node = listeners;
            foreach (var token in tokens) {
                var match = node.Children.SingleOrDefault(e => e.Value.Key == token.Value);

                if (match == null) {
                    return node;
                } else {
                    node = match;
                }//else
            }//foreach

            return node;
        }//GetNode

        void FireEvent(string path) {
            var node = GetNode(path);

            do {
                foreach (var listener in node.Value.Handlers) {
                    listener.Invoke();
                }//foreach

                node = node.Parent;
            } while (node != null);
        }//FireEvent

        void FireDeepMergeEvents(string path) {
            FireEvent(path);

            var node = GetNode(path);

            foreach(var child in node.Children) {
                FireEvents(child);
            }//foreach
        }//FireDeepMergeEvents

        void FireEvents(ITreeNode<Event> node) {
            foreach (var listener in node.Value.Handlers) {
                listener.Invoke();
            }//foreach

            foreach (var child in node.Children) {
                FireEvents(child);
            }//foreach
        }//FireEvents

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
                tokens = path.Split(DELIMS, 2, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0) {
                    break;
                }//if
                var token = new Token() { Indexer = Regex.IsMatch(path, @"^[\d]+\]"), Value = tokens[0] };
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

            if (tokens.Count == 0) {
                state = (JObject)value;
            } else {
                JToken jtoken = state;

                foreach (var token in tokens) {
                    int index;
                    if (token.Indexer && int.TryParse(token.Value, out index)) {
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

                jtoken.Replace(JToken.FromObject(value));
            }

            FireEvent(path);
        }//Set

        public void Unset(string path) {
            var target = state.SelectToken(path);
            target.Parent.Remove();

            FireEvent(path);
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

            FireEvent(path);
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

            FireEvent(path);
        }

        public void Splice(string path) {
            var tokens = Tokenize(path);

            JToken jtoken = state;

            for (int i = 0; i < tokens.Count - 2; i++) {
                var token = tokens[i];
                int idx;
                if (token.Indexer && int.TryParse(token.Value, out idx)) {
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

            FireEvent(path);
        }//Splice

        public void Concat(string path, object obj) {
            ((JArray)state.SelectToken(path)).Add(obj);

            FireEvent(path);
        }//Concat

        public void DeepMerge(string path, object obj) {
            var tokens = Tokenize(path);

            JToken jtoken = state;

            foreach(var token in tokens) {
                int index;
                if (token.Indexer && int.TryParse(token.Value, out index)) {
                    jtoken = jtoken[index];
                } else {
                    jtoken = jtoken[token.Value];
                }//else
            }//foreach

            if (jtoken is JObject) {
                ((JObject)jtoken).Merge(obj, mergeSettings);
            } else {
                jtoken.Replace(JToken.FromObject(obj));
            }//else

            FireDeepMergeEvents(path);
        }//DeepMerge
    }
}
