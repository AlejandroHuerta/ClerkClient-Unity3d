﻿using System;
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
            stateManager.RegisterListener("worldTime", (change) => triggered = true);

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
            stateManager.RegisterListener("world.time", (change) => triggered = true);

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
            stateManager.RegisterListener("players", (change) => triggered = true);

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
            stateManager.RegisterListener("players[0].health", (change) => triggered = true);

            stateManager.Set("players[0].health", 400);

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
            stateManager.RegisterListener("world.country.pop", (change) => triggered = true);

            stateManager.Set("world.country.pop", 5000);

            Assert.IsTrue(triggered);
        }
    }
}