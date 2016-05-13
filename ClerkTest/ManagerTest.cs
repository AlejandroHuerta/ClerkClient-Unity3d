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
            var state = JObject.Parse("{worldTime: 560,players:[{name: 'Rick',health:100},{name: 'Morty',health:100}]}");

            var stateManager = new Manager(state);
            Assert.AreEqual<int>(560, stateManager.Get<int>("worldTime"));
        }

        [TestMethod]
        public void ReturnsDefaultIfNotExist() {
            var state = JObject.Parse("{worldTime: 560,players:[{name: 'Rick',health:100},{name: 'Morty',health:100}]}");

            var stateManager = new Manager(state);
            Assert.IsNull(stateManager.Get("myTime"));
        }

        [TestMethod]
        public void SetAValue() {
            var state = JObject.Parse("{worldTime: 560,players:[{name: 'Rick',health:100},{name: 'Morty',health:100}]}");

            var stateManager = new Manager(state);
            stateManager.Set("worldTime", 600);
            Assert.AreEqual<int>(600, stateManager.Get<int>("worldTime"));
        }

        [TestMethod]
        public void SetADeeperValue() {
            var state = JObject.Parse("{worldTime: 560,players:[{name: 'Rick',health:100},{name: 'Morty',health:100}]}");

            var stateManager = new Manager(state);
            stateManager.Set("players[0].health", 90);
            Assert.AreEqual<int>(90, stateManager.Get<int>("players[0].health"));
        }

        [TestMethod]
        public void AddAValue() {
            var state = JObject.Parse("{worldTime: 560,players:[{name: 'Rick',health:100},{name: 'Morty',health:100}]}");

            var stateManager = new Manager(state);
            stateManager.Set("gameTime", 600);
            Assert.AreEqual<int>(600, stateManager.Get<int>("gameTime"));
        }

        [TestMethod]
        public void UnsetAValue() {
            var state = JObject.Parse("{worldTime: 560,players:[{name: 'Rick',health:100},{name: 'Morty',health:100}]}");
        
            var stateManager = new Manager(state);
            stateManager.Unset("worldTime");
            Assert.IsNull(stateManager.Get("worldTime"));
        }

        [TestMethod]
        public void UnsetADeeperValue() {
            var state = JObject.Parse("{worldTime: 560,players:[{name: 'Rick',health:100},{name: 'Morty',health:100}]}");

            var stateManager = new Manager(state);
            stateManager.Unset("players[0].health");
            Assert.IsNull(stateManager.Get("players[0].health"));
        }
    }
}
