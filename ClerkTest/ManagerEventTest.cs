using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Clerk;

namespace ClerkTest {
    [TestClass]
    public class ManagerEventTest {
        [TestMethod]
        public void ReceiveEventOnValueSet() {
            var state = JObject.Parse(@"
            {
                worldTime: 560
            }");

            var stateManager = new Manager(state);

            var triggered = false;
            stateManager.RegisterListener("worldTime", () => triggered = true);

            stateManager.Set("worldTime", 400);

            Assert.IsTrue(triggered);
        }

        [TestMethod]
        public void ReceiveEventOnDeeperValueSet() {
            var state = JObject.Parse(@"
            {
                world: {
                    time: 500
                }
            }");

            var stateManager = new Manager(state);

            var triggered = false;
            stateManager.RegisterListener("world.time", () => triggered = true);

            stateManager.Set("world.time", 400);

            Assert.IsTrue(triggered);
        }

        [TestMethod]
        public void ReceiveEventOnArrayValueSet() {
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

            var triggered = false;
            stateManager.RegisterListener("players", () => triggered = true);

            stateManager.Set("players[0].health", 400);

            Assert.IsTrue(triggered);
        }

        [TestMethod]
        public void ReceiveEventOnDeeperArrayValueSet() {
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

            var triggered = false;
            stateManager.RegisterListener("players[0].health", () => triggered = true);

            stateManager.DeepMerge("players[0].health", 400);

            Assert.IsTrue(triggered);
        }

        [TestMethod]
        public void ReceiveEventOnDeepMerge() {
            var state = JObject.Parse(@"
            {
                world: {
                    time: 500,
                    country: {
                        time: 300,
                        pop: 2000
                    }
                }
            }");

            var newState = JObject.Parse(@"
            {
                world: {
                    country: {
                        pop: 5000
                    }
                }
            }");

            var stateManager = new Manager(state);

            var triggered = false;
            stateManager.RegisterListener("world.country.pop", () => triggered = true);

            stateManager.DeepMerge("", newState);

            Assert.IsTrue(triggered);
        }

        [TestMethod]
        public void UnregisterListener() {
            var state = JObject.Parse(@"
            {
                worldTime: 560
            }");

            var stateManager = new Manager(state);

            var triggered = false;
            Action action = () => triggered = true;
            stateManager.RegisterListener("worldTime", action);
            stateManager.UnregisterListener("worldTime", action);

            stateManager.Set("worldTime", 400);

            Assert.IsFalse(triggered);
        }

        [TestMethod]
        public void UnregisterDeepListener() {
            var state = JObject.Parse(@"
            {
                world: {
                    time: 500
                }
            }");

            var stateManager = new Manager(state);

            var triggered = false;
            Action action = () => triggered = true;
            stateManager.RegisterListener("world.time", action);
            stateManager.UnregisterListener("world.time", action);

            stateManager.Set("world.time", 400);

            Assert.IsFalse(triggered);
        }

        [TestMethod]
        public void UnregisterListenerFromNonPath() {
            var state = JObject.Parse(@"
            {
                world: {
                    time: 500
                }
            }");

            var stateManager = new Manager(state);
            
            Action action = () => { };
            stateManager.UnregisterListener("world.time", action);
        }
    }
}
