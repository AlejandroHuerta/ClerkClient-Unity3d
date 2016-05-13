using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Clerk;
using Newtonsoft.Json.Linq;

namespace ClerkTest {
    [TestClass]
    public class ManagerTest {

        [TestMethod]
        public void GetAValue() {
            var state = JObject.Parse(@"
            {
                worldTime: 560
            }");

            var stateManager = new Manager(state);
            Assert.AreEqual(560, stateManager.Get<int>("worldTime"));
        }

        [TestMethod]
        public void GetAnArrayValue() {
            var state = JObject.Parse(@"
            {
                players:[
                    {
                        name: 'Rick',
                        health:100
                    }
                ]
            }");
            var rick = JToken.Parse(@"
            {
                name: 'Rick',
                health:100
            }");

            var stateManager = new Manager(state);

            Assert.AreEqual(rick.ToString(), stateManager.Get<JToken>("players[0]").ToString());
        }

        [TestMethod]
        public void GetADeeperValue() {
            var state = JObject.Parse(@"
            {
                players:[
                    {
                        name: 'Rick',
                        health:100
                    }
                ]
            }");

            var stateManager = new Manager(state);
            Assert.AreEqual(100, stateManager.Get<int>("players[0].health"));
        }

        [TestMethod]
        public void ReturnsDefaultIfNotExist() {
            var state = JObject.Parse(@"
            {
            }");

            var stateManager = new Manager(state);
            Assert.IsNull(stateManager.Get("myTime"));
        }

        [TestMethod]
        public void SetAValue() {
            var state = JObject.Parse(@"
            {
                worldTime: 560
            }");

            var stateManager = new Manager(state);
            stateManager.Set("worldTime", 600);
            Assert.AreEqual(600, stateManager.Get<int>("worldTime"));
        }

        [TestMethod]
        public void SetADeeperValue() {
            var state = JObject.Parse(@"
            {
                players:[
                    {
                        name: 'Rick',
                        health:100
                    }
                ]
            }");

            var stateManager = new Manager(state);
            stateManager.Set("players[0].health", 90);
            Assert.AreEqual(90, stateManager.Get<int>("players[0].health"));
        }

        [TestMethod]
        public void AddAValue() {
            var state = JObject.Parse(@"
            {
            }");

            var stateManager = new Manager(state);
            stateManager.Set("gameTime", 600);
            Assert.AreEqual(600, stateManager.Get<int>("gameTime"));
        }

        [TestMethod]
        public void AddADeeperValue() {
            var state = JObject.Parse(@"
            {
            }");

            var stateManager = new Manager(state);
            stateManager.Set("foo.bar.baz", 600);
            Assert.AreEqual(600, stateManager.Get<int>("foo.bar.baz"));
        }

        [TestMethod]
        public void UnsetAValue() {
            var state = JObject.Parse(@"
            {
                worldTime: 560
            }");

            var stateManager = new Manager(state);
            stateManager.Unset("worldTime");
            Assert.IsNull(stateManager.Get("worldTime"));
        }

        [TestMethod]
        public void UnsetADeeperValue() {
            var state = JObject.Parse(@"
            {
                players:[
                    {
                        name: 'Rick',
                        health:100
                    }
                ]
            }");

            var stateManager = new Manager(state);
            stateManager.Unset("players[0].health");
            Assert.IsNull(stateManager.Get("players[0].health"));
        }

        [TestMethod]
        public void PushAnArray() {
            var state = JObject.Parse(@"
            {
                worldTime: 560,
                players:[
                    {
                        name: 'Rick',
                        health:100
                    },
                    {
                        name: 'Morty',
                        health:100
                    }
                ]
            }");
            var family = JToken.Parse(@"
            [
                {
                    name: 'Jerry',
                    health:2
                },
                {
                    name: 'Beth',
                    health:100
                },
                {
                    name: 'Summer',
                    health:99
                }
            ]");

            var stateManager = new Manager(state);
            stateManager.Push("players", family);
            Assert.AreEqual("Jerry", stateManager.Get<string>("players[2].name"));
            Assert.AreEqual("Beth", stateManager.Get<string>("players[3].name"));
            Assert.AreEqual("Summer", stateManager.Get<string>("players[4].name"));
        }

        [TestMethod]
        public void PushAValue() {
            var state = JObject.Parse(@"
            {
                players:[
                    {
                        name: 'Rick',
                        health:100
                    },
                    {
                        name: 'Morty',
                        health:100
                    }
                ]
            }");

            var stateManager = new Manager(state);
            stateManager.Push("players", 600);
            Assert.AreEqual(600, stateManager.Get<int>("players[2]"));
        }

        [TestMethod]
        public void PushAnObject() {
            var state = JObject.Parse(@"
            {
                players:[
                    {
                        name: 'Rick',
                        health:100
                    },
                    {
                        name: 'Morty',
                        health:100
                    }
                ]
            }");
            var obj = JToken.Parse("{name: 'Jerry',health:100}");

            var stateManager = new Manager(state);
            stateManager.Push("players", obj);
            Assert.AreEqual(obj.ToString(), stateManager.Get<JToken>("players[2]").ToString());
        }
    }
}
