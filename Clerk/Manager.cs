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

        public object Get(string path) {
            return Get<object>(path);
        }

        public T Get<T>(string path) {
            var obj = state[path];
            if (obj == null) {
                return default(T);
            } else {
                return state[path].ToObject<T>();
            }//else
        }

        public void Set(string path, object value) {
            state[path] = JToken.FromObject(value);
        }

        public void Unset(string path) {
            state.Remove(path);
        }

        public void Push() {

        }

        public void Unshift() {

        }

        public void Splice() {

        }

        public void Concat() {

        }

        public void DeepMerge() {

        }
    }
}
