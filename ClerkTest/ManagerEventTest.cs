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

            stateManager.Update();

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

            stateManager.Update();

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

            stateManager.Update();

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

            stateManager.Set("players[0].health", 400);

            stateManager.Update();

            Assert.IsTrue(triggered);
        }
    }
}
