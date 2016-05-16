using HighEnergy.Collections;
using Newtonsoft.Json.Linq;
using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clerk {
    public class Manager {

        SimplePriorityQueue<StateMessage> queue = new SimplePriorityQueue<StateMessage>();

        JObject state;

        class Event {
            public string Key { get; set; }
            public List<Action> Handlers { get; }

            public Event() {
                Handlers = new List<Action>();
            }//Event
        }//Event

        Tree<Event> listeners = new Tree<Event>(new Event() { Key = "" });
        
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

        public void RegisterListener(string path, Action action) {
            var tokens = Tokenize(path);

            ITreeNode<Event> node = listeners;
            foreach(var token in tokens) {
                var match = node.Children.SingleOrDefault(e => e.Value.Key == token);

                if (match == null) {
                    var child = new TreeNode<Event>(new Event() { Key = token });
                    node.Children.Add(child);
                    node = child;
                } else {
                    node = match;
                }//else
            }//foreach

            node.Value.Handlers.Add(action);
        }//RegisterListener

        void FireEvent(string path) {
            var tokens = Tokenize(path);

            ITreeNode<Event> node = listeners;
            foreach (var token in tokens) {
                var match = node.Children.SingleOrDefault(e => e.Value.Key == token);

                if (match == null) {
                    return;
                } else {
                    foreach (var listener in match.Value.Handlers) {
                        listener.Invoke();
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
            switch(message.action) {
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

            FireEvent(path);
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

            FireEvent(path);
        }//Splice

        public void Concat(string path, object obj) {
            ((JArray)state.SelectToken(path)).Add(obj);

            FireEvent(path);
        }//Concat

        public void DeepMerge(string path, object obj) {
            
        }
    }
}
